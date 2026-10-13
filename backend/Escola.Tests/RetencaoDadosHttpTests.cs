using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Escola.Api.Servicos;
using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Escola.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Escola.Tests;

/// <summary>Retenção de dados: registros de acesso do Marco Civil e a exclusão de um clube cancelado (aviso 7 dias antes, depois apaga tudo
/// dele — e nada de outro cliente).</summary>
public partial class RetencaoDadosHttpTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static DateOnly Hoje => DateOnly.FromDateTime(DateTime.Today);

    private async Task<(Guid ClienteId, HttpClient Admin, string Email)> AssinarEEntrarAsync()
    {
        var email = $"dono.{Guid.NewGuid():N}@clube.test";
        var http = api.CreateClient();
        (await http.PostAsJsonAsync("/api/assinaturas", new
        {
            nomeClube = "Clube Que Vai Embora", cpfCnpj = "11222333000181", cidade = "Campinas", atletas = 10,
            nomeAdmin = "Dona do Clube", email, celular = "(19) 99999-8888", aceiteTermos = true
        })).EnsureSuccessStatusCode();
        var token = TokenDoLink().Match(api.Email.Enviados.Last(e => e.Para == email).Corpo).Groups[1].Value;
        (await http.PostAsJsonAsync("/api/auth/redefinir-senha", new { token, novaSenha = "Senha-do-Dono-1" })).EnsureSuccessStatusCode();
        var login = await api.EntrarAsync(http, email, "Senha-do-Dono-1");
        login.EnsureSuccessStatusCode();
        var admin = api.CreateClient();
        admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            (await login.Content.ReadFromJsonAsync<ApiFactory.LoginResposta>())!.Token);

        using var db = api.Contexto(ApiFactory.ClienteId);
        var clienteId = await db.Assinaturas.IgnoreQueryFilters().Where(a => a.EmailCobranca == email)
            .Select(a => EF.Property<Guid>(a, EscolaDbContext.ColunaCliente)).SingleAsync();
        return (clienteId, admin, email);
    }

    [Fact]
    public async Task LoginESenhaPeloLinkFicamNosRegistrosDeAcesso()
    {
        var (clienteId, admin, email) = await AssinarEEntrarAsync();
        (await admin.GetAsync("/api/turmas")).EnsureSuccessStatusCode(); // mesmo dia do login: não vira outro registro

        using var db = api.Contexto(clienteId);
        var registros = await db.RegistrosAcesso.Where(r => r.Email == email).OrderBy(r => r.OcorridoEm).Select(r => r.Tipo).ToListAsync();
        Assert.Equal([TipoRegistroAcesso.SenhaPeloLink, TipoRegistroAcesso.Login], registros);
    }

    [Fact]
    public async Task ClubeCanceladoRecebeAvisoEDepoisTemTudoApagadoSemTocarEmOutroCliente()
    {
        var (clienteId, admin, email) = await AssinarEEntrarAsync();
        PovoarClube(clienteId);
        int usuariosDoOutroCliente;
        using (var db = api.Contexto(ApiFactory.ClienteId)) usuariosDoOutroCliente = await db.Usuarios.CountAsync();

        // Cancelou; o acesso acabou há 31 dias (passou dos 30 de retenção).
        (await admin.PostAsync("/api/assinaturas/minha/cancelar", null)).EnsureSuccessStatusCode();
        using (var db = api.Contexto(clienteId))
        {
            (await db.Assinaturas.SingleAsync()).AcessoAte = Hoje.AddDays(-31);
            await db.SaveChangesAsync();
        }
        using var escopo = api.Services.CreateScope();
        var assinaturas = escopo.ServiceProvider.GetRequiredService<IAssinaturaService>();
        await assinaturas.AtualizarAsync(clienteId);

        // 1ª passada: só o aviso (e a exclusão fica pra 7 dias depois dele).
        await assinaturas.ProcessarRetencaoAsync(clienteId);
        Assert.Single(api.Email.Enviados, e => e.Para == email && e.Assunto.Contains("serão apagados"));
        await assinaturas.ProcessarRetencaoAsync(clienteId);
        using (var db = api.Contexto(clienteId)) Assert.True(await db.Alunos.AnyAsync());

        // 7 dias depois do aviso: apaga.
        using (var db = api.Contexto(clienteId))
        {
            (await db.Assinaturas.SingleAsync()).AvisoExclusaoEnviadoEm = DateTime.UtcNow.AddDays(-8);
            await db.SaveChangesAsync();
        }
        await assinaturas.ProcessarRetencaoAsync(clienteId);

        using (var db = api.Contexto(clienteId))
        {
            foreach (var tipo in ExclusaoDadosCliente.OrdemDeExclusao(db.Model))
            {
                var restantes = await (Task<int>)Contar.MakeGenericMethod(tipo).Invoke(null, [db, clienteId])!;
                Assert.True(restantes == 0, $"{tipo.Name}: sobraram {restantes} linhas do cliente excluído.");
            }
            var cliente = await db.Clientes.SingleAsync(c => c.Id == clienteId);
            Assert.Equal(ExclusaoDadosCliente.NomeClienteExcluido, cliente.Nome);
            Assert.False(cliente.Ativo);
            Assert.NotNull(cliente.ExcluidoEm); // marca pro provisionamento do Suporte não recriar conta nele
            Assert.True(await db.RegistrosAcesso.AnyAsync(r => r.Email == email)); // Marco Civil: ficam 6 meses
        }
        using (var db = api.Contexto(ApiFactory.ClienteId)) Assert.Equal(usuariosDoOutroCliente, await db.Usuarios.CountAsync());
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.EntrarAsync(api.CreateClient(), email, "Senha-do-Dono-1")).StatusCode);
    }

    [Fact]
    public async Task ReativarAntesDaExclusaoInterrompeOProcesso()
    {
        var (clienteId, admin, email) = await AssinarEEntrarAsync();
        (await admin.PostAsync("/api/assinaturas/minha/cancelar", null)).EnsureSuccessStatusCode();
        using (var db = api.Contexto(clienteId))
        {
            (await db.Assinaturas.SingleAsync()).AcessoAte = Hoje.AddDays(-25);
            await db.SaveChangesAsync();
        }
        using var escopo = api.Services.CreateScope();
        var assinaturas = escopo.ServiceProvider.GetRequiredService<IAssinaturaService>();
        await assinaturas.AtualizarAsync(clienteId);
        await assinaturas.ProcessarRetencaoAsync(clienteId);

        var minha = await admin.GetFromJsonAsync<JsonElement>("/api/assinaturas/minha", Json);
        Assert.Equal(Hoje.AddDays(5).ToString("yyyy-MM-dd"), minha.GetProperty("dataExclusao").GetString());

        (await admin.PostAsync("/api/assinaturas/minha/reativar", null)).EnsureSuccessStatusCode();
        minha = await admin.GetFromJsonAsync<JsonElement>("/api/assinaturas/minha", Json);
        Assert.Equal(JsonValueKind.Null, minha.GetProperty("dataExclusao").ValueKind);
        using (var db = api.Contexto(clienteId)) Assert.Null((await db.Assinaturas.SingleAsync()).AvisoExclusaoEnviadoEm);
    }

    /// <summary>Um pouco de cada módulo, pra exclusão passar pelas chaves estrangeiras de verdade.</summary>
    private void PovoarClube(Guid clienteId)
    {
        using var db = api.Contexto(clienteId);
        var admin = db.Usuarios.First(u => u.Papel == PapelUsuario.Admin);
        var agora = DateTime.UtcNow;
        var turma = BancoDeTeste.NovaTurma(db, "Sub-13");
        var aluno = new Aluno { Id = Guid.NewGuid(), Nome = "Atleta", DataNascimento = new DateOnly(2013, 1, 1), TurmaId = turma.Id, FotoUrl = "/uploads/nao-existe.jpg", MatriculaConfirmadaEm = agora };
        var responsavel = new Responsavel { Id = Guid.NewGuid(), Nome = "Mãe", Email = $"mae.{Guid.NewGuid():N}@exemplo.test" };
        var cobranca = new Cobranca { Id = Guid.NewGuid(), AlunoId = aluno.Id, Descricao = "Mensalidade", Valor = 100m, Vencimento = Hoje };
        var campeonato = new Campeonato { Id = Guid.NewGuid(), Nome = "Copa", DataInicio = Hoje, RegistradoEm = agora };
        var jogo = new Jogo { Id = Guid.NewGuid(), CampeonatoId = campeonato.Id, TurmaId = turma.Id, Adversario = "Rival", Data = Hoje, Hora = new TimeOnly(9, 0), RegistradoEm = agora };
        var fornecedor = new Fornecedor { Id = Guid.NewGuid(), Nome = "Loja" };
        var produto = new Produto { Id = Guid.NewGuid(), Nome = "Bola", UnidadeMedida = "un", RegistradoEm = agora };

        db.AddRange(aluno, responsavel, cobranca, campeonato, jogo, fornecedor, produto);
        db.AlunoResponsaveis.Add(new AlunoResponsavel { AlunoId = aluno.Id, ResponsavelId = responsavel.Id, ResponsavelFinanceiro = true });
        db.Add(new CobrancaPix { Id = Guid.NewGuid(), CobrancaId = cobranca.Id, TxId = $"tx{Guid.NewGuid():N}", Valor = 100m, PixCopiaECola = "000201", CriadoEm = agora, ExpiraEm = agora.AddDays(1) });
        db.Add(new JogoAtleta { Id = Guid.NewGuid(), JogoId = jogo.Id, AlunoId = aluno.Id, Gols = 1 });
        db.Add(new RegistroPresenca { Id = Guid.NewGuid(), TurmaId = turma.Id, AlunoId = aluno.Id, Data = Hoje, UsuarioId = admin.Id, RegistradoEm = agora });
        db.Add(new ContaPagar { Id = Guid.NewGuid(), FornecedorId = fornecedor.Id, Descricao = "Bolas", Valor = 50m, Vencimento = Hoje, RegistradoEm = agora });
        db.Add(new MovimentacaoEstoque { Id = Guid.NewGuid(), ProdutoId = produto.Id, Quantidade = 10, Data = Hoje, FornecedorId = fornecedor.Id, UsuarioId = admin.Id, RegistradoEm = agora });
        db.Add(new FichaSaude { Id = Guid.NewGuid(), AlunoId = aluno.Id, Alergias = "Amendoim", AtualizadoEm = agora });
        db.Add(new DocumentoSaude { Id = Guid.NewGuid(), AlunoId = aluno.Id, NomeArquivo = "exame.pdf", ContentType = "application/pdf", ArquivoArmazenado = "nao-existe.pdf", UsuarioId = admin.Id, EnviadoEm = agora });
        db.Add(new RegistroDiarioClasse { Id = Guid.NewGuid(), TurmaId = turma.Id, CriadoPorUsuarioId = admin.Id, Titulo = "Treino", RegistradoEm = agora });
        db.Add(new TermoAceite { Id = Guid.NewGuid(), AlunoId = aluno.Id, ResponsavelId = responsavel.Id, UsuarioId = admin.Id, Versao = "v", TextoAceito = "termo", AceitoEm = agora });
        db.SaveChanges();
    }

    private static readonly MethodInfo Contar = typeof(RetencaoDadosHttpTests).GetMethod(nameof(ContarAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static Task<int> ContarAsync<T>(EscolaDbContext db, Guid clienteId) where T : class =>
        db.Set<T>().IgnoreQueryFilters().CountAsync(e => EF.Property<Guid>(e, EscolaDbContext.ColunaCliente) == clienteId);

    [GeneratedRegex(@"token=([A-Za-z0-9_\-]+)")]
    private static partial Regex TokenDoLink();
}
