using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Escola.Tests;

/// <summary>Gateway Asaas (subconta por escola): conectar a conta, gerar o Pix pela subconta e dar baixa pelo webhook — tudo contra o
/// <see cref="AsaasFalso"/>, sem rede.</summary>
public class PagamentoAsaasHttpTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly SemaphoreSlim Trava = new(1, 1);

    private static object Conta(string cpfCnpj = "52998224725") => new
    {
        nome = "Escola de Futebol Teste", email = "Financeiro@Escola.Teste", cpfCnpj, tipoEmpresa = (string?)null,
        dataNascimento = "1985-03-10", celular = "(11) 99999-8888", cep = "01310-100", endereco = "Av. Paulista", numero = "1000",
        complemento = (string?)null, bairro = "Bela Vista", faturamentoMensal = 20000m
    };

    /// <summary>A conta é uma por escola e a fábrica é compartilhada pela classe: conecta uma vez só, seja qual teste rodar primeiro.</summary>
    private async Task GarantirContaAsync()
    {
        await Trava.WaitAsync();
        try
        {
            using (var db = api.Contexto(ApiFactory.ClienteId))
                if (await db.ContasPagamento.AnyAsync()) return;
            var admin = await api.ClienteLogadoAsync(PapelUsuario.Admin);
            (await admin.PostAsJsonAsync("/api/pagamentos/conta", Conta())).EnsureSuccessStatusCode();
        }
        finally { Trava.Release(); }
    }

    private async Task<(Guid CobrancaId, Guid ResponsavelId)> CriarCobrancaAsync(string? cpfResponsavel)
    {
        using var db = api.Contexto(ApiFactory.ClienteId);
        var turma = BancoDeTeste.NovaTurma(db, $"Turma {Guid.NewGuid():N}");
        var aluno = new Aluno { Id = Guid.NewGuid(), Nome = "Atleta Asaas", DataNascimento = new DateOnly(2014, 5, 1), TurmaId = turma.Id, MatriculaConfirmadaEm = DateTime.UtcNow };
        var responsavel = new Responsavel { Id = Guid.NewGuid(), Nome = "Mãe do Atleta", Email = $"mae.{Guid.NewGuid():N}@exemplo.test", Cpf = cpfResponsavel };
        var cobranca = new Cobranca { Id = Guid.NewGuid(), AlunoId = aluno.Id, Descricao = "Mensalidade - Janeiro/2030", Valor = 250m, Vencimento = new DateOnly(2030, 1, 10) };
        db.Alunos.Add(aluno);
        db.Responsaveis.Add(responsavel);
        db.AlunoResponsaveis.Add(new AlunoResponsavel { AlunoId = aluno.Id, ResponsavelId = responsavel.Id, ResponsavelFinanceiro = true });
        db.Cobrancas.Add(cobranca);
        await db.SaveChangesAsync();
        return (cobranca.Id, responsavel.Id);
    }

    [Fact]
    public async Task ConectarContaCriaASubcontaGuardaAChaveCriptografadaEAuditaSemDeixarConectarDuasVezes()
    {
        var coordenador = await api.ClienteLogadoAsync(PapelUsuario.Coordenador);
        Assert.Equal(HttpStatusCode.Forbidden, (await coordenador.PostAsJsonAsync("/api/pagamentos/conta", Conta())).StatusCode);

        var admin = await api.ClienteLogadoAsync(PapelUsuario.Admin);
        var invalido = await admin.PostAsJsonAsync("/api/pagamentos/conta", Conta(cpfCnpj: "11111111111"));
        Assert.Equal(HttpStatusCode.BadRequest, invalido.StatusCode);

        await GarantirContaAsync();

        var conta = await coordenador.GetFromJsonAsync<JsonElement>("/api/pagamentos/conta", Json);
        Assert.True(conta.GetProperty("conectada").GetBoolean());
        Assert.True(conta.GetProperty("chavePixCriada").GetBoolean());
        Assert.Equal("APPROVED", conta.GetProperty("situacaoGateway").GetString());
        Assert.True(conta.GetProperty("webhookConfigurado").GetBoolean());

        // O webhook vai na criação da subconta, apontando pra plataforma com o token.
        var criacao = api.Asaas.Chamadas.Single(c => c.Metodo == "POST" && c.Caminho == "/accounts");
        var webhook = criacao.Corpo!["webhooks"]![0]!;
        Assert.Equal("https://escola.teste/api/pagamentos/asaas/webhook", webhook["url"]!.GetValue<string>());
        Assert.Equal(ApiFactory.AsaasWebhookToken, webhook["authToken"]!.GetValue<string>());

        using (var db = api.Contexto(ApiFactory.ClienteId))
        {
            var salva = await db.ContasPagamento.SingleAsync();
            Assert.DoesNotContain(AsaasFalso.ApiKeySubconta, salva.ApiKeyCriptografada);
            Assert.StartsWith("v1:", salva.ApiKeyCriptografada);
            Assert.Contains(await db.LogsAuditoria.Where(l => l.EntidadeTipo == nameof(ContaPagamento)).Select(l => l.Detalhe).ToListAsync(),
                d => d != null && d.Contains("Conta de pagamento conectada (Asaas, Sandbox)"));
        }

        var deNovo = await admin.PostAsJsonAsync("/api/pagamentos/conta", Conta());
        Assert.Equal(HttpStatusCode.BadRequest, deNovo.StatusCode);
    }

    [Fact]
    public async Task PixDaMensalidadeSaiDaSubcontaEOWebhookDaABaixaPeloValorQueOAsaasConfirmar()
    {
        await GarantirContaAsync();
        var financeiro = await api.ClienteLogadoAsync(PapelUsuario.Financeiro);
        var (cobrancaId, responsavelId) = await CriarCobrancaAsync(cpfResponsavel: "52998224725");

        var pix = await financeiro.GetFromJsonAsync<JsonElement>($"/api/financeiro/cobrancas/{cobrancaId}/pix", Json);
        Assert.True(pix.GetProperty("automatico").GetBoolean());
        Assert.Contains("ASAAS", pix.GetProperty("codigoCopiaECola").GetString());

        string pagamentoId;
        using (var db = api.Contexto(ApiFactory.ClienteId))
        {
            var cobrancaPix = await db.CobrancasPix.SingleAsync(p => p.CobrancaId == cobrancaId);
            Assert.Equal(ProvedorPagamento.Asaas, cobrancaPix.Provedor);
            pagamentoId = cobrancaPix.TxId;
            // O cliente criado no Asaas fica guardado: a próxima cobrança dessa família não cria outro.
            Assert.NotNull((await db.Responsaveis.SingleAsync(r => r.Id == responsavelId)).IdClienteAsaas);
        }
        // As chamadas da cobrança usam a chave da subconta, nunca a da conta raiz.
        Assert.All(api.Asaas.Chamadas.Where(c => c.Caminho.StartsWith("/payments")), c => Assert.Equal(AsaasFalso.ApiKeySubconta, c.ApiKey));

        // Token errado: recusado, nada muda.
        var anonimo = api.CreateClient();
        var semToken = new HttpRequestMessage(HttpMethod.Post, "/api/pagamentos/asaas/webhook")
        { Content = JsonContent.Create(new { @event = "PAYMENT_RECEIVED", payment = new { id = pagamentoId, value = 1m } }) };
        semToken.Headers.Add("asaas-access-token", "token-errado");
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.SendAsync(semToken)).StatusCode);

        // O corpo do webhook diz R$ 1,00, mas a baixa usa o que o Asaas confirma na consulta.
        lock (api.Asaas.Pagamentos) api.Asaas.Pagamentos[pagamentoId] = ("RECEIVED", 250m, "2030-01-05");
        var aviso = new HttpRequestMessage(HttpMethod.Post, "/api/pagamentos/asaas/webhook")
        { Content = JsonContent.Create(new { @event = "PAYMENT_RECEIVED", payment = new { id = pagamentoId, value = 1m } }) };
        aviso.Headers.Add("asaas-access-token", ApiFactory.AsaasWebhookToken);
        Assert.Equal(HttpStatusCode.OK, (await anonimo.SendAsync(aviso)).StatusCode);

        using (var db = api.Contexto(ApiFactory.ClienteId))
        {
            var cobranca = await db.Cobrancas.SingleAsync(c => c.Id == cobrancaId);
            Assert.True(cobranca.Paga);
            Assert.Equal(250m, cobranca.ValorPago);
            var log = await db.LogsAuditoria.Where(l => l.EntidadeId == cobrancaId && l.UsuarioId == null).Select(l => l.Detalhe).ToListAsync();
            Assert.Contains(log, d => d != null && d.Contains("Asaas"));
        }
    }

    [Fact]
    public async Task WebhookDePagamentoDesconhecidoResponde200SemMexerEmNada()
    {
        var anonimo = api.CreateClient();
        var aviso = new HttpRequestMessage(HttpMethod.Post, "/api/pagamentos/asaas/webhook")
        { Content = JsonContent.Create(new { @event = "PAYMENT_RECEIVED", payment = new { id = "pay_que_nao_existe" } }) };
        aviso.Headers.Add("asaas-access-token", ApiFactory.AsaasWebhookToken);
        Assert.Equal(HttpStatusCode.OK, (await anonimo.SendAsync(aviso)).StatusCode);
    }

    [Fact]
    public async Task SemCpfDoResponsavelCaiNoPixEstatico()
    {
        await GarantirContaAsync();
        var gestao = await api.ClienteLogadoAsync(PapelUsuario.Coordenador);
        (await gestao.PutAsJsonAsync("/api/financeiro/configuracao", new
        {
            pixChave = "+5511999998888", pixTipoChave = "Telefone", pixNomeRecebedor = "Escola Teste", pixCidade = "Sao Paulo",
            pagamentoPixAtivo = true, pagamentoBoletoAtivo = false, pagamentoPresencialAtivo = false
        })).EnsureSuccessStatusCode();
        var (cobrancaId, _) = await CriarCobrancaAsync(cpfResponsavel: null);

        var pix = await gestao.GetFromJsonAsync<JsonElement>($"/api/financeiro/cobrancas/{cobrancaId}/pix", Json);
        Assert.False(pix.GetProperty("automatico").GetBoolean());
        Assert.DoesNotContain("ASAAS", pix.GetProperty("codigoCopiaECola").GetString());
        using var db = api.Contexto(ApiFactory.ClienteId);
        Assert.False(await db.CobrancasPix.AnyAsync(p => p.CobrancaId == cobrancaId));
    }
}
