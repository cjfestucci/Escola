using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Escola.Api.Servicos;
using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Escola.Infrastructure.Auth;
using Escola.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Escola.Tests;

/// <summary>Assinatura pelo site: cadastro → cliente novo com Admin por convite → senha → login; e o ciclo da cobrança (atraso além da
/// tolerância suspende, pagamento pelo webhook libera) — contra o <see cref="AsaasFalso"/>.</summary>
public partial class AssinaturaHttpTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private const string Cnpj = "11222333000181";

    private static object Cadastro(string email, string cpfCnpj = Cnpj, bool aceite = true) => new
    {
        nomeClube = "Clube Teste Assinatura", cpfCnpj, cidade = "Campinas", atletas = 40,
        nomeAdmin = "Maria Dona do Clube", email, celular = "(19) 99999-8888", aceiteTermos = aceite
    };

    /// <summary>Cadastra, cria a senha pelo link do e-mail e devolve o cliente + o Admin já logado.</summary>
    private async Task<(Guid ClienteId, HttpClient Admin, string Email)> AssinarEEntrarAsync()
    {
        var email = $"dono.{Guid.NewGuid():N}@clube.test";
        var http = api.CreateClient();
        (await http.PostAsJsonAsync("/api/assinaturas", Cadastro(email))).EnsureSuccessStatusCode();

        var corpo = api.Email.Enviados.Last(e => e.Para == email).Corpo;
        var token = TokenDoLink().Match(corpo).Groups[1].Value;
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
    public async Task OPlanoEhPublico()
    {
        var plano = await api.CreateClient().GetFromJsonAsync<JsonElement>("/api/assinaturas/plano", Json);
        Assert.Equal(60m, plano.GetProperty("precoFixo").GetDecimal());
        Assert.Equal(20, plano.GetProperty("atletasIncluidos").GetInt32());
        Assert.Equal(4m, plano.GetProperty("precoPorAtleta").GetDecimal());
        Assert.Equal(7, plano.GetProperty("diasTeste").GetInt32());
    }

    [Fact]
    public async Task CadastroInvalidoEhRecusado()
    {
        var http = api.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await http.PostAsJsonAsync("/api/assinaturas", Cadastro("a@clube.test", cpfCnpj: "11111111111"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await http.PostAsJsonAsync("/api/assinaturas", Cadastro("a@clube.test", aceite: false))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await http.PostAsJsonAsync("/api/assinaturas", Cadastro("sem-arroba"))).StatusCode);
    }

    [Fact]
    public async Task CadastroCriaOClubeComAdminPorConviteEAssinaturaNoGatewayEOAdminEntra()
    {
        var email = $"dono.{Guid.NewGuid():N}@clube.test";
        var resposta = await api.CreateClient().PostAsJsonAsync("/api/assinaturas", Cadastro(email));
        resposta.EnsureSuccessStatusCode();
        var cadastro = await resposta.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal("EmTeste", cadastro.GetProperty("situacao").GetString());
        Assert.Equal(140m, cadastro.GetProperty("valorMensal").GetDecimal()); // 60 + 4 × (40 − 20)
        Assert.Equal(JsonValueKind.Null, cadastro.GetProperty("linkPagamento").ValueKind); // com teste, nada a pagar agora
        var testeAte = DateOnly.Parse(cadastro.GetProperty("testeAte").GetString()!);

        Guid clienteId;
        using (var db = api.Contexto(ApiFactory.ClienteId))
        {
            var assinatura = await db.Assinaturas.IgnoreQueryFilters().SingleAsync(a => a.EmailCobranca == email);
            clienteId = db.Entry(assinatura).Property<Guid>(EscolaDbContext.ColunaCliente).CurrentValue;
            Assert.Equal(Cnpj, assinatura.CpfCnpj);
            Assert.NotNull(assinatura.IdAssinaturaGateway);
            Assert.NotNull(assinatura.ConviteEnviadoEm);
            Assert.Equal(AssinaturaService.VersaoTermosUso, assinatura.TermosVersao);
        }

        using (var db = api.Contexto(clienteId))
        {
            var cliente = await db.Clientes.SingleAsync(c => c.Id == clienteId);
            Assert.Equal(SegmentoCliente.Clube, cliente.Segmento);
            var admin = await db.Usuarios.SingleAsync(u => u.Papel == PapelUsuario.Admin);
            Assert.Equal(SenhaHasher.ConvitePendente, admin.SenhaHash);
            Assert.True(await db.Unidades.AnyAsync(u => u.Nome == "Unidade Principal"));
            Assert.True(await db.Usuarios.AnyAsync(u => u.Papel == PapelUsuario.Suporte && u.Ativo));
        }

        // Assinatura mensal na conta RAIZ, com o clube escolhendo a forma de pagamento, 1º vencimento no fim do teste.
        var criacao = api.Asaas.Chamadas.Last(c => c.Metodo == "POST" && c.Caminho == "/subscriptions");
        Assert.Equal("$aact_hmlg_conta_raiz_de_teste", criacao.ApiKey);
        Assert.Equal("UNDEFINED", criacao.Corpo!["billingType"]!.GetValue<string>());
        Assert.Equal(testeAte.ToString("yyyy-MM-dd"), criacao.Corpo!["nextDueDate"]!.GetValue<string>());

        // Sem senha ainda: não entra. Depois do link do e-mail, entra e vê a assinatura.
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.EntrarAsync(api.CreateClient(), email, "qualquer-coisa-1")).StatusCode);
        Assert.Contains("Bem-vindo", api.Email.Enviados.Last(e => e.Para == email).Assunto);
    }

    [Fact]
    public async Task AtrasoAlemDaToleranciaSuspendeEOPagamentoPeloWebhookLibera()
    {
        var (clienteId, admin, _) = await AssinarEEntrarAsync();

        var minha = await admin.GetFromJsonAsync<JsonElement>("/api/assinaturas/minha", Json);
        Assert.Equal("EmTeste", minha.GetProperty("situacao").GetString());
        Assert.Equal(7, minha.GetProperty("diasRestantesTeste").GetInt32());
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/turmas")).StatusCode);

        // Uma conta da equipe do clube, criada enquanto tudo está em dia.
        api.CriarUsuario(PapelUsuario.Coordenador, $"coord.{Guid.NewGuid():N}@clube.test", clienteId: clienteId);
        string idAssinatura;
        using (var db = api.Contexto(clienteId))
        {
            var a = await db.Assinaturas.SingleAsync();
            idAssinatura = a.IdAssinaturaGateway!;
            // O tempo passa: o teste acabou há 10 dias e a fatura não foi paga.
            a.TesteAte = DateOnly.FromDateTime(DateTime.Today).AddDays(-10);
            a.PrimeiroVencimento = a.TesteAte.Value;
            await db.SaveChangesAsync();
        }
        api.Asaas.MudarVencimento(idAssinatura, 0, DateOnly.FromDateTime(DateTime.Today).AddDays(-10));

        minha = await (await admin.PostAsync("/api/assinaturas/minha/atualizar", null)).Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal("Suspensa", minha.GetProperty("situacao").GetString());
        Assert.True(minha.GetProperty("bloqueada").GetBoolean());
        Assert.StartsWith("https://asaas.teste/i/", minha.GetProperty("linkPagamento").GetString());

        // Suspensa: o Admin só alcança a assinatura (402 no resto); a equipe nem entra.
        Assert.Equal(HttpStatusCode.PaymentRequired, (await admin.GetAsync("/api/turmas")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/assinaturas/minha")).StatusCode);
        string coordenador;
        using (var db = api.Contexto(clienteId)) coordenador = (await db.Usuarios.SingleAsync(u => u.Papel == PapelUsuario.Coordenador)).Email;
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.EntrarAsync(api.CreateClient(), coordenador, "Senha-de-Teste-1")).StatusCode);

        // O clube paga; o webhook (com o token certo) só diz qual assinatura conferir.
        var pagamentoId = api.Asaas.Assinaturas[idAssinatura][0].PagamentoId;
        api.Asaas.Pagar(pagamentoId);
        var aviso = new { @event = "PAYMENT_RECEIVED", payment = new { id = pagamentoId, subscription = idAssinatura } };
        var anonimo = api.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.PostAsJsonAsync("/api/assinaturas/asaas/webhook", aviso)).StatusCode);
        anonimo.DefaultRequestHeaders.Add("asaas-access-token", ApiFactory.AssinaturaWebhookToken);
        Assert.Equal(HttpStatusCode.OK, (await anonimo.PostAsJsonAsync("/api/assinaturas/asaas/webhook", aviso)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anonimo.PostAsJsonAsync("/api/assinaturas/asaas/webhook",
            new { @event = "PAYMENT_RECEIVED", payment = new { id = "pay_x", subscription = "sub_desconhecida" } })).StatusCode);

        minha = await admin.GetFromJsonAsync<JsonElement>("/api/assinaturas/minha", Json);
        Assert.Equal("Ativa", minha.GetProperty("situacao").GetString());
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/turmas")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await api.EntrarAsync(api.CreateClient(), coordenador, "Senha-de-Teste-1")).StatusCode);

        // Cada mudança de situação fica no histórico, assinada pelo sistema.
        using (var db = api.Contexto(clienteId))
        {
            var id = (await db.Assinaturas.SingleAsync()).Id;
            var detalhes = await db.LogsAuditoria.Where(l => l.EntidadeId == id).Select(l => l.Detalhe).ToListAsync();
            Assert.Contains(detalhes, d => d!.Contains("para \"Suspensa\""));
            Assert.Contains(detalhes, d => d!.Contains("para \"Ativa\""));
        }
    }

    [Fact]
    public async Task ClienteSemAssinaturaNaoEhAfetado()
    {
        var admin = await api.ClienteLogadoAsync(PapelUsuario.Admin);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/api/assinaturas/minha")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/turmas")).StatusCode);
        var coordenador = await api.ClienteLogadoAsync(PapelUsuario.Coordenador);
        Assert.Equal(HttpStatusCode.Forbidden, (await coordenador.GetAsync("/api/assinaturas/minha")).StatusCode);
    }

    private static DateOnly Hoje => DateOnly.FromDateTime(DateTime.Today);

    private async Task<JsonElement> AtualizarAsync(HttpClient admin) =>
        await (await admin.PostAsync("/api/assinaturas/minha/atualizar", null)).Content.ReadFromJsonAsync<JsonElement>(Json);

    [Fact]
    public async Task DepoisDaPrimeiraPagaOValorSegueOsAtletasAtivosECancelarMantemOAcessoAteOFimDoPeriodo()
    {
        var (clienteId, admin, _) = await AssinarEEntrarAsync();
        string idAssinatura;
        using (var db = api.Contexto(clienteId))
        {
            idAssinatura = (await db.Assinaturas.SingleAsync()).IdAssinaturaGateway!;
            var turma = BancoDeTeste.NovaTurma(db);
            for (var i = 0; i < 5; i++)
                db.Alunos.Add(new Aluno { Id = Guid.NewGuid(), Nome = $"Atleta {i}", DataNascimento = new DateOnly(2014, 1, 1), TurmaId = turma.Id, MatriculaConfirmadaEm = DateTime.UtcNow });
            db.Alunos.Add(new Aluno { Id = Guid.NewGuid(), Nome = "Atleta inativo", DataNascimento = new DateOnly(2014, 1, 1), TurmaId = turma.Id, Ativo = false });
            await db.SaveChangesAsync();
        }

        // Ainda no teste: vale o número informado no cadastro (40 → R$ 140), mesmo com 5 atletas cadastrados.
        var minha = await AtualizarAsync(admin);
        Assert.Equal(140m, minha.GetProperty("valorMensal").GetDecimal());

        // Pagou a 1ª; a próxima fatura vence em 20 dias: o valor passa a seguir os 5 atletas ATIVOS (dentro dos 20 incluídos → R$ 60), também na fatura em aberto.
        api.Asaas.Pagar(api.Asaas.Assinaturas[idAssinatura][0].PagamentoId);
        var proxima = api.Asaas.GerarFatura(idAssinatura, Hoje.AddDays(20), 140m);
        minha = await AtualizarAsync(admin);
        Assert.Equal("Ativa", minha.GetProperty("situacao").GetString());
        Assert.Equal(60m, minha.GetProperty("valorMensal").GetDecimal());
        Assert.Equal(5, minha.GetProperty("atletasCobrados").GetInt32());
        Assert.Equal(60m, api.Asaas.Pagamentos[proxima].Valor);

        // Coordenador não cancela; o Admin cancela, e o acesso segue até o vencimento da fatura em aberto.
        var coordenadorEmail = $"coord.{Guid.NewGuid():N}@clube.test";
        api.CriarUsuario(PapelUsuario.Coordenador, coordenadorEmail, clienteId: clienteId);
        var coordenador = api.CreateClient();
        coordenador.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            (await (await api.EntrarAsync(coordenador, coordenadorEmail, "Senha-de-Teste-1")).Content.ReadFromJsonAsync<ApiFactory.LoginResposta>())!.Token);
        Assert.Equal(HttpStatusCode.Forbidden, (await coordenador.PostAsync("/api/assinaturas/minha/cancelar", null)).StatusCode);

        var cancelamento = await admin.PostAsync("/api/assinaturas/minha/cancelar", null);
        cancelamento.EnsureSuccessStatusCode();
        minha = await cancelamento.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal(Hoje.AddDays(20).ToString("yyyy-MM-dd"), minha.GetProperty("acessoAte").GetString());
        Assert.Equal("Ativa", minha.GetProperty("situacao").GetString());
        Assert.Contains(idAssinatura, api.Asaas.AssinaturasCanceladas);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync("/api/assinaturas/minha/cancelar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/turmas")).StatusCode);

        // Passou o fim do acesso: Cancelada, e o Admin só alcança a assinatura.
        using (var db = api.Contexto(clienteId))
        {
            (await db.Assinaturas.SingleAsync()).AcessoAte = Hoje.AddDays(-1);
            await db.SaveChangesAsync();
        }
        minha = await AtualizarAsync(admin);
        Assert.Equal("Cancelada", minha.GetProperty("situacao").GetString());
        Assert.Equal(HttpStatusCode.PaymentRequired, (await admin.GetAsync("/api/turmas")).StatusCode);

        // Reativar depois do fim do acesso: assinatura NOVA no Asaas, vencendo hoje. Já pagou antes, então volta a usar (com a fatura do dia
        // em aberto, como qualquer mensalidade); a cancelada não é reaproveitada.
        var reativacao = await admin.PostAsync("/api/assinaturas/minha/reativar", null);
        reativacao.EnsureSuccessStatusCode();
        minha = await reativacao.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal("Ativa", minha.GetProperty("situacao").GetString());
        Assert.Equal(JsonValueKind.Null, minha.GetProperty("acessoAte").ValueKind);
        var nova = api.Asaas.Chamadas.Last(c => c.Metodo == "POST" && c.Caminho == "/subscriptions");
        Assert.Equal(Hoje.ToString("yyyy-MM-dd"), nova.Corpo!["nextDueDate"]!.GetValue<string>());
        using (var db = api.Contexto(clienteId))
            Assert.NotEqual(idAssinatura, (await db.Assinaturas.SingleAsync()).IdAssinaturaGateway);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/turmas")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync("/api/assinaturas/minha/reativar", null)).StatusCode);

        using (var db = api.Contexto(clienteId))
        {
            var id = (await db.Assinaturas.SingleAsync()).Id;
            var detalhes = await db.LogsAuditoria.Where(l => l.EntidadeId == id).Select(l => l.Detalhe).ToListAsync();
            Assert.Contains(detalhes, d => d!.StartsWith("Valor do plano alterado") && d.Contains("5 atletas ativos"));
            Assert.Contains(detalhes, d => d!.StartsWith("Assinatura cancelada"));
            Assert.Contains(detalhes, d => d!.StartsWith("Assinatura reativada"));
        }
    }

    [Fact]
    public async Task LembreteDoFimDoTesteEAvisoDeSuspensaoSaemUmaVezSo()
    {
        var (clienteId, admin, email) = await AssinarEEntrarAsync();
        string idAssinatura;
        using (var db = api.Contexto(clienteId))
        {
            var a = await db.Assinaturas.SingleAsync();
            idAssinatura = a.IdAssinaturaGateway!;
            a.TesteAte = a.PrimeiroVencimento = Hoje.AddDays(1);
            await db.SaveChangesAsync();
        }
        api.Asaas.MudarVencimento(idAssinatura, 0, Hoje.AddDays(1));

        await AtualizarAsync(admin);
        await AtualizarAsync(admin);
        Assert.Single(api.Email.Enviados, e => e.Para == email && e.Assunto.StartsWith("Seu teste grátis termina"));

        using (var db = api.Contexto(clienteId))
        {
            var a = await db.Assinaturas.SingleAsync();
            a.TesteAte = a.PrimeiroVencimento = Hoje.AddDays(-10);
            await db.SaveChangesAsync();
        }
        api.Asaas.MudarVencimento(idAssinatura, 0, Hoje.AddDays(-10));

        Assert.Equal("Suspensa", (await AtualizarAsync(admin)).GetProperty("situacao").GetString());
        await AtualizarAsync(admin);
        Assert.Single(api.Email.Enviados, e => e.Para == email && e.Assunto.StartsWith("Acesso suspenso"));
    }

    [Fact]
    public async Task AsNotificacoesDoAsaasFicamDesligadasEOSistemaMandaReciboLembreteEAtraso()
    {
        var (clienteId, admin, email) = await AssinarEEntrarAsync();
        string idAssinatura;
        using (var db = api.Contexto(clienteId)) idAssinatura = (await db.Assinaturas.SingleAsync()).IdAssinaturaGateway!;

        // O cliente do clube no Asaas nasce sem notificações (são pagas).
        var clienteAsaas = api.Asaas.Chamadas.Last(c => c.Metodo == "POST" && c.Caminho == "/customers" && c.Corpo!["email"]?.GetValue<string>() == email);
        Assert.True(clienteAsaas.Corpo!["notificationDisabled"]!.GetValue<bool>());

        int Avisos(string inicioAssunto) => api.Email.Enviados.Count(e => e.Para == email && e.Assunto.StartsWith(inicioAssunto));

        // Pagou a 1ª e a próxima já vence em 2 dias: recibo numa rodada, lembrete na seguinte, e nada se repete.
        api.Asaas.Pagar(api.Asaas.Assinaturas[idAssinatura][0].PagamentoId);
        var proxima = api.Asaas.GerarFatura(idAssinatura, Hoje.AddDays(2), 140m);
        for (var i = 0; i < 3; i++) await AtualizarAsync(admin);
        Assert.Equal(1, Avisos("Pagamento confirmado"));
        Assert.Equal(1, Avisos("Sua mensalidade vence em"));
        Assert.Contains($"https://asaas.teste/i/{proxima}", api.Email.Enviados.Last(e => e.Para == email && e.Assunto.StartsWith("Sua mensalidade")).Corpo);

        // Venceu há 2 dias (dentro da tolerância): um aviso de atraso, com a data em que o acesso é suspenso.
        api.Asaas.MudarVencimento(idAssinatura, 1, Hoje.AddDays(-2));
        Assert.Equal("EmAtraso", (await AtualizarAsync(admin)).GetProperty("situacao").GetString());
        await AtualizarAsync(admin);
        Assert.Equal(1, Avisos("Mensalidade em atraso"));
        Assert.Contains(Hoje.AddDays(5).ToString("dd/MM/yyyy"), api.Email.Enviados.Last(e => e.Para == email && e.Assunto.StartsWith("Mensalidade em atraso")).Corpo);
    }

    [GeneratedRegex(@"token=([A-Za-z0-9_\-]+)")]
    private static partial Regex TokenDoLink();
}
