using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Microsoft.AspNetCore.Hosting;
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
    public async Task ComSubcontasDesligadasAEscolaNaoVeNemAbreOPagamentoAutomatico()
    {
        // Produção hoje: o Asaas serve só pra assinatura dos clubes; subconta custa R$ 12,90 e ainda não tem onboarding.
        using var fabrica = api.WithWebHostBuilder(b => b.UseSetting("Asaas:SubcontasHabilitadas", "false"));
        var admin = fabrica.CreateClient();
        admin.DefaultRequestHeaders.Authorization = (await api.ClienteLogadoAsync(PapelUsuario.Admin)).DefaultRequestHeaders.Authorization;
        var criacoesAntes = api.Asaas.Chamadas.Count(c => c.Metodo == "POST" && c.Caminho == "/accounts");

        var conta = await admin.GetFromJsonAsync<JsonElement>("/api/pagamentos/conta", Json);
        Assert.False(conta.GetProperty("disponivel").GetBoolean());
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/pagamentos/conta", Conta())).StatusCode);
        Assert.Equal(criacoesAntes, api.Asaas.Chamadas.Count(c => c.Metodo == "POST" && c.Caminho == "/accounts"));
    }

    [Fact]
    public async Task ContaEmAnaliseMostraOsDocumentosPendentesComOLinkDeEnvio()
    {
        await GarantirContaAsync();
        var admin = await api.ClienteLogadoAsync(PapelUsuario.Admin);
        api.Asaas.SituacaoConta = "AWAITING_APPROVAL";
        try
        {
            var conta = await (await admin.PostAsync("/api/pagamentos/conta/atualizar", null)).Content.ReadFromJsonAsync<JsonElement>(Json);
            var documentos = conta.GetProperty("documentos").EnumerateArray().ToList();
            // O aprovado não aparece; o "não enviado" leva o link do Asaas (e não vai pela API); o "em análise" não tem o que enviar;
            // o que não tem link do Asaas é enviado pelo sistema.
            Assert.Equal(3, documentos.Count);
            Assert.Equal("Não enviado", documentos[0].GetProperty("situacao").GetString());
            Assert.Equal("https://asaas.teste/onboarding/doc_1", documentos[0].GetProperty("urlEnvio").GetString());
            Assert.False(documentos[0].GetProperty("envioPelaApi").GetBoolean());
            Assert.Equal("Em análise", documentos[1].GetProperty("situacao").GetString());
            Assert.Equal(JsonValueKind.Null, documentos[1].GetProperty("urlEnvio").ValueKind);
            Assert.False(documentos[1].GetProperty("envioPelaApi").GetBoolean());
            Assert.Equal("Não enviado", documentos[2].GetProperty("situacao").GetString());
            Assert.Equal("doc_4", documentos[2].GetProperty("id").GetString());
            Assert.True(documentos[2].GetProperty("envioPelaApi").GetBoolean());
        }
        finally
        {
            api.Asaas.SituacaoConta = "APPROVED";
            (await admin.PostAsync("/api/pagamentos/conta/atualizar", null)).EnsureSuccessStatusCode();
        }

        // Aprovada: nada de documentos (nem consulta).
        var aprovada = await admin.GetFromJsonAsync<JsonElement>("/api/pagamentos/conta", Json);
        Assert.Equal(JsonValueKind.Null, aprovada.GetProperty("documentos").ValueKind);
    }

    private static MultipartFormDataContent Arquivo(byte[] conteudo, string nome)
    {
        var parte = new ByteArrayContent(conteudo);
        parte.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        return new MultipartFormDataContent { { parte, "arquivo", nome } };
    }

    private static readonly byte[] Pdf = System.Text.Encoding.ASCII.GetBytes("%PDF-1.4\n1 0 obj\n<<>>\nendobj\ntrailer\n<<>>\n%%EOF");

    [Fact]
    public async Task DocumentoSemLinkDoAsaasVaiPelaApiComOTipoDoProprioAsaasEFicaNoHistorico()
    {
        await GarantirContaAsync();
        var admin = await api.ClienteLogadoAsync(PapelUsuario.Admin);
        api.Asaas.SituacaoConta = "AWAITING_APPROVAL";
        try
        {
            (await admin.PostAsync("/api/pagamentos/conta/atualizar", null)).EnsureSuccessStatusCode();

            // Permissão: quem não é Admin/Suporte não envia documento da empresa.
            var coordenador = await api.ClienteLogadoAsync(PapelUsuario.Coordenador);
            Assert.Equal(HttpStatusCode.Forbidden, (await coordenador.PostAsync("/api/pagamentos/conta/documentos/doc_4", Arquivo(Pdf, "mei.pdf"))).StatusCode);

            // Texto disfarçado de PDF (o nome diz .pdf, o conteúdo não) é recusado pelo conteúdo.
            var falso = await admin.PostAsync("/api/pagamentos/conta/documentos/doc_4", Arquivo("<html>oi</html>"u8.ToArray(), "mei.pdf"));
            Assert.Equal(HttpStatusCode.BadRequest, falso.StatusCode);

            // Documento que tem a página do Asaas (doc_1) nunca vai pela API.
            var comLink = await admin.PostAsync("/api/pagamentos/conta/documentos/doc_1", Arquivo(Pdf, "rg.pdf"));
            Assert.Equal(HttpStatusCode.BadRequest, comLink.StatusCode);
            var inexistente = await admin.PostAsync("/api/pagamentos/conta/documentos/doc_999", Arquivo(Pdf, "x.pdf"));
            Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
            Assert.DoesNotContain(api.Asaas.Chamadas, c => c.Metodo == "POST" && c.Caminho is "/myAccount/documents/doc_1" or "/myAccount/documents/doc_999");

            var ok = await admin.PostAsync("/api/pagamentos/conta/documentos/doc_4", Arquivo(Pdf, "mei.pdf"));
            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

            // Foi à subconta (nunca à raiz), com o tipo que o Asaas informou e o arquivo.
            var envio = api.Asaas.Chamadas.Last(c => c.Metodo == "POST" && c.Caminho == "/myAccount/documents/doc_4");
            Assert.Equal(AsaasFalso.ApiKeySubconta, envio.ApiKey);
            var multipart = envio.Corpo!["multipart"]!.GetValue<string>();
            Assert.Contains("MEI_CERTIFICATE", multipart);
            Assert.Contains("documentFile", multipart);
            Assert.Contains("%PDF-1.4", multipart);

            using var db = api.Contexto(ApiFactory.ClienteId);
            Assert.Contains(await db.LogsAuditoria.Where(l => l.EntidadeTipo == nameof(ContaPagamento)).Select(l => l.Detalhe).ToListAsync(),
                d => d != null && d.Contains("Documento enviado ao parceiro de pagamentos: Certificado MEI"));
        }
        finally
        {
            api.Asaas.SituacaoConta = "APPROVED";
            (await admin.PostAsync("/api/pagamentos/conta/atualizar", null)).EnsureSuccessStatusCode();
        }
    }

    [Fact]
    public async Task DocumentosSoSaemDepoisDaEsperaDoAsaasAposCriarASubconta()
    {
        await GarantirContaAsync();
        api.Asaas.SituacaoConta = "AWAITING_APPROVAL";
        try
        {
            // Espera de uma hora: a conta (aberta há segundos) ainda está "em preparo" e o Asaas nem é consultado.
            using var fabrica = api.WithWebHostBuilder(b => b.UseSetting("Asaas:EsperaDocumentosSegundos", "3600"));
            var admin = fabrica.CreateClient();
            admin.DefaultRequestHeaders.Authorization = (await api.ClienteLogadoAsync(PapelUsuario.Admin)).DefaultRequestHeaders.Authorization;
            var consultasAntes = api.Asaas.Chamadas.Count(c => c.Metodo == "GET" && c.Caminho == "/myAccount/documents");

            var conta = await (await admin.PostAsync("/api/pagamentos/conta/atualizar", null)).Content.ReadFromJsonAsync<JsonElement>(Json);
            Assert.True(conta.GetProperty("documentosEmPreparo").GetBoolean());
            Assert.Empty(conta.GetProperty("documentos").EnumerateArray());
            Assert.Equal(consultasAntes, api.Asaas.Chamadas.Count(c => c.Metodo == "GET" && c.Caminho == "/myAccount/documents"));
        }
        finally
        {
            api.Asaas.SituacaoConta = "APPROVED";
            var admin = await api.ClienteLogadoAsync(PapelUsuario.Admin);
            (await admin.PostAsync("/api/pagamentos/conta/atualizar", null)).EnsureSuccessStatusCode();
        }
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

    /// <summary>Visto no sandbox real: logo depois de criar a chave Pix da subconta, o Asaas cria a cobrança mas recusa o QR Code
    /// ("você não possui uma chave Pix") por alguns segundos. A cobrança sem QR é excluída e o pagador criado fica guardado.</summary>
    [Fact]
    public async Task SeOQrCodeFalhaACobrancaOrfaEhExcluidaEOPagadorNaoDuplica()
    {
        await GarantirContaAsync();
        var financeiro = await api.ClienteLogadoAsync(PapelUsuario.Financeiro);
        var (cobrancaId, responsavelId) = await CriarCobrancaAsync(cpfResponsavel: "52998224725");

        api.Asaas.FalharQrCode = true;
        try { await financeiro.GetAsync($"/api/financeiro/cobrancas/{cobrancaId}/pix"); }
        finally { api.Asaas.FalharQrCode = false; }

        List<(string Metodo, string Caminho, string? ApiKey, System.Text.Json.Nodes.JsonNode? Corpo)> chamadas;
        lock (api.Asaas.Chamadas) chamadas = [.. api.Asaas.Chamadas];
        var idCriado = chamadas.Last(c => c.Metodo == "GET" && c.Caminho.EndsWith("/pixQrCode")).Caminho.Split('/')[2];
        Assert.Contains(chamadas, c => c.Metodo == "DELETE" && c.Caminho == $"/payments/{idCriado}");

        string? clienteGuardado;
        using (var db = api.Contexto(ApiFactory.ClienteId))
            clienteGuardado = (await db.Responsaveis.SingleAsync(r => r.Id == responsavelId)).IdClienteAsaas;
        Assert.NotNull(clienteGuardado);

        // Na próxima tentativa (chave já ativa) o Pix sai pelo Asaas, reaproveitando o mesmo cliente.
        int clientesAntes;
        lock (api.Asaas.Chamadas) clientesAntes = api.Asaas.Chamadas.Count(c => c.Metodo == "POST" && c.Caminho == "/customers");
        var pix = await financeiro.GetFromJsonAsync<JsonElement>($"/api/financeiro/cobrancas/{cobrancaId}/pix", Json);
        Assert.True(pix.GetProperty("automatico").GetBoolean());
        lock (api.Asaas.Chamadas) Assert.Equal(clientesAntes, api.Asaas.Chamadas.Count(c => c.Metodo == "POST" && c.Caminho == "/customers"));
    }

    [Fact]
    public async Task PixNumaCobrancaCanceladaViraPendenciaAteAlguemConferir()
    {
        await GarantirContaAsync();
        var financeiro = await api.ClienteLogadoAsync(PapelUsuario.Financeiro);
        var (cobrancaId, _) = await CriarCobrancaAsync(cpfResponsavel: "52998224725");
        (await financeiro.GetAsync($"/api/financeiro/cobrancas/{cobrancaId}/pix")).EnsureSuccessStatusCode();
        (await financeiro.PostAsync($"/api/financeiro/cobrancas/{cobrancaId}/cancelar", null)).EnsureSuccessStatusCode();

        string pagamentoId;
        using (var db = api.Contexto(ApiFactory.ClienteId))
            pagamentoId = (await db.CobrancasPix.SingleAsync(p => p.CobrancaId == cobrancaId)).TxId;

        // A família pagou o código que já tinha em mãos, depois do cancelamento.
        lock (api.Asaas.Pagamentos) api.Asaas.Pagamentos[pagamentoId] = ("RECEIVED", 250m, "2030-01-05");
        var aviso = new HttpRequestMessage(HttpMethod.Post, "/api/pagamentos/asaas/webhook")
        { Content = JsonContent.Create(new { @event = "PAYMENT_RECEIVED", payment = new { id = pagamentoId } }) };
        aviso.Headers.Add("asaas-access-token", ApiFactory.AsaasWebhookToken);
        Assert.Equal(HttpStatusCode.OK, (await api.CreateClient().SendAsync(aviso)).StatusCode);

        var educador = await api.ClienteLogadoAsync(PapelUsuario.Educador);
        Assert.Equal(HttpStatusCode.Forbidden, (await educador.GetAsync("/api/financeiro/pix-pendencias")).StatusCode);

        var pendencias = await financeiro.GetFromJsonAsync<JsonElement>("/api/financeiro/pix-pendencias", Json);
        var pendencia = pendencias.EnumerateArray().Single(p => p.GetProperty("cobrancaId").GetGuid() == cobrancaId);
        Assert.Equal("CobrancaCancelada", pendencia.GetProperty("motivo").GetString());
        Assert.Equal(250m, pendencia.GetProperty("valorRecebido").GetDecimal());
        Assert.Equal("Atleta Asaas", pendencia.GetProperty("alunoNome").GetString());

        // A baixa não acontece sozinha numa cobrança cancelada.
        using (var db = api.Contexto(ApiFactory.ClienteId))
            Assert.False((await db.Cobrancas.SingleAsync(c => c.Id == cobrancaId)).Paga);

        var id = pendencia.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await financeiro.PostAsync($"/api/financeiro/pix-pendencias/{id}/conferir", null)).StatusCode);

        pendencias = await financeiro.GetFromJsonAsync<JsonElement>("/api/financeiro/pix-pendencias", Json);
        Assert.DoesNotContain(pendencias.EnumerateArray(), p => p.GetProperty("cobrancaId").GetGuid() == cobrancaId);
        using (var db = api.Contexto(ApiFactory.ClienteId))
        {
            var log = await db.LogsAuditoria.Where(l => l.EntidadeId == cobrancaId).Select(l => l.Detalhe).ToListAsync();
            Assert.Contains(log, d => d != null && d.StartsWith("ATENÇÃO") && d.Contains("cancelada"));
            Assert.Contains(log, d => d != null && d.Contains("conferido"));
        }
        Assert.Equal(HttpStatusCode.NotFound, (await financeiro.PostAsync($"/api/financeiro/pix-pendencias/{Guid.NewGuid()}/conferir", null)).StatusCode);
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
