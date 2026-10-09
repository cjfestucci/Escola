using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Escola.Api.Servicos;
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
        Assert.Equal(99m, plano.GetProperty("precoFixo").GetDecimal());
        Assert.Equal(3m, plano.GetProperty("precoPorAtleta").GetDecimal());
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
        Assert.Equal(219m, cadastro.GetProperty("valorMensal").GetDecimal()); // 99 + 3 × 40
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

    [GeneratedRegex(@"token=([A-Za-z0-9_\-]+)")]
    private static partial Regex TokenDoLink();
}
