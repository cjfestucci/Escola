using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Escola.Tests;

/// <summary>Conta Asaas <b>própria do clube</b> (ele gera a chave de API no Asaas e cola no sistema): validação da chave, consentimento,
/// webhook cadastrado na conta dele, Pix gerado com a chave dele e desconexão. Contra o <see cref="AsaasFalso"/>, sem rede.</summary>
public class ContaAsaasPropriaHttpTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static object Pedido(string chave = AsaasFalso.ApiKeyClube, bool termos = true, bool chavePix = true) =>
        new { apiKey = chave, aceitouTermosAsaas = termos, autorizouChavePix = chavePix };

    [Fact]
    public async Task ConectaContaPropriaGuardandoAChaveCriptografadaOConsentimentoEOWebhookEDepoisUsaAChaveDoClubeNoPix()
    {
        var coordenador = await api.ClienteLogadoAsync(PapelUsuario.Coordenador);
        Assert.Equal(HttpStatusCode.Forbidden, (await coordenador.PostAsJsonAsync("/api/pagamentos/conta/propria", Pedido())).StatusCode);

        var admin = await api.ClienteLogadoAsync(PapelUsuario.Admin);

        // Sem o aceite dos Termos do Asaas ou sem a autorização da chave Pix: nem chega a falar com o Asaas.
        var chamadasAntes = api.Asaas.Chamadas.Count;
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/pagamentos/conta/propria", Pedido(termos: false))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/pagamentos/conta/propria", Pedido(chavePix: false))).StatusCode);
        // Chave de produção num ambiente de teste, e texto que nem parece chave do Asaas.
        var producao = await admin.PostAsJsonAsync("/api/pagamentos/conta/propria", Pedido(chave: "$aact_prod_chave_de_producao_qualquer"));
        Assert.Equal(HttpStatusCode.BadRequest, producao.StatusCode);
        Assert.Contains("produção", await producao.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/pagamentos/conta/propria", Pedido(chave: "qualquer coisa colada errado"))).StatusCode);
        Assert.Equal(chamadasAntes, api.Asaas.Chamadas.Count);

        // Chave com cara de certa, mas que o Asaas recusa.
        var recusada = await admin.PostAsJsonAsync("/api/pagamentos/conta/propria", Pedido(chave: "$aact_hmlg_chave_que_o_asaas_nao_conhece"));
        Assert.Equal(HttpStatusCode.BadRequest, recusada.StatusCode);
        Assert.DoesNotContain("chave_que_o_asaas", await recusada.Content.ReadAsStringAsync());
        using (var db = api.Contexto(ApiFactory.ClienteId)) Assert.False(await db.ContasPagamento.AnyAsync());

        var ok = await admin.PostAsJsonAsync("/api/pagamentos/conta/propria", Pedido());
        Assert.True(ok.IsSuccessStatusCode, await ok.Content.ReadAsStringAsync());
        var corpo = await ok.Content.ReadAsStringAsync();
        Assert.DoesNotContain(AsaasFalso.ApiKeyClube, corpo);
        var conta = JsonSerializer.Deserialize<JsonElement>(corpo, Json);
        Assert.True(conta.GetProperty("conectada").GetBoolean());
        Assert.True(conta.GetProperty("propria").GetBoolean());
        Assert.True(conta.GetProperty("chavePixCriada").GetBoolean());
        Assert.True(conta.GetProperty("webhookConfigurado").GetBoolean());
        Assert.Equal("Clube Próprio FC", conta.GetProperty("titularNome").GetString());

        // Chave Pix e webhook foram criados na conta do clube, com a chave dele.
        Assert.Contains(api.Asaas.Chamadas, c => c.Metodo == "POST" && c.Caminho == "/pix/addressKeys" && c.ApiKey == AsaasFalso.ApiKeyClube);
        Assert.Contains("https://escola.teste/api/pagamentos/asaas/webhook", api.Asaas.Webhooks);
        // Conta própria não abre subconta nem consulta documentos (o KYC é do clube com o Asaas).
        Assert.DoesNotContain(api.Asaas.Chamadas, c => c.Caminho == "/accounts");
        Assert.DoesNotContain(api.Asaas.Chamadas, c => c.Caminho == "/myAccount/documents");

        using (var db = api.Contexto(ApiFactory.ClienteId))
        {
            var salva = await db.ContasPagamento.SingleAsync();
            Assert.True(salva.Propria);
            Assert.StartsWith("v1:", salva.ApiKeyCriptografada);
            Assert.DoesNotContain(AsaasFalso.ApiKeyClube, salva.ApiKeyCriptografada);
            Assert.NotNull(salva.ConsentimentoEm);
            Assert.Contains(await db.LogsAuditoria.Where(l => l.EntidadeTipo == nameof(ContaPagamento)).Select(l => l.Detalhe).ToListAsync(),
                d => d != null && d.Contains("Conta Asaas própria conectada") && d.Contains("Termos do Asaas aceitos"));
        }

        // Já conectada: não conecta de novo.
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/pagamentos/conta/propria", Pedido())).StatusCode);

        // O Pix da mensalidade sai da conta do clube, com a chave dele (nunca a da conta raiz).
        var financeiro = await api.ClienteLogadoAsync(PapelUsuario.Financeiro);
        Guid cobrancaId;
        using (var db = api.Contexto(ApiFactory.ClienteId))
        {
            var turma = BancoDeTeste.NovaTurma(db, $"Turma {Guid.NewGuid():N}");
            var aluno = new Aluno { Id = Guid.NewGuid(), Nome = "Atleta Conta Própria", DataNascimento = new DateOnly(2014, 5, 1), TurmaId = turma.Id, MatriculaConfirmadaEm = DateTime.UtcNow };
            var responsavel = new Responsavel { Id = Guid.NewGuid(), Nome = "Mãe", Email = $"mae.{Guid.NewGuid():N}@exemplo.test", Cpf = "52998224725" };
            var cobranca = new Cobranca { Id = Guid.NewGuid(), AlunoId = aluno.Id, Descricao = "Mensalidade - Janeiro/2030", Valor = 120m, Vencimento = new DateOnly(2030, 1, 10) };
            db.Alunos.Add(aluno);
            db.Responsaveis.Add(responsavel);
            db.AlunoResponsaveis.Add(new AlunoResponsavel { AlunoId = aluno.Id, ResponsavelId = responsavel.Id, ResponsavelFinanceiro = true });
            db.Cobrancas.Add(cobranca);
            await db.SaveChangesAsync();
            cobrancaId = cobranca.Id;
        }
        var pix = await financeiro.GetFromJsonAsync<JsonElement>($"/api/financeiro/cobrancas/{cobrancaId}/pix", Json);
        Assert.True(pix.GetProperty("automatico").GetBoolean());
        var chamadasDePagamento = api.Asaas.Chamadas.Where(c => c.Caminho.StartsWith("/payments") || c.Caminho == "/customers").ToList();
        Assert.NotEmpty(chamadasDePagamento);
        Assert.All(chamadasDePagamento, c => Assert.Equal(AsaasFalso.ApiKeyClube, c.ApiKey));

        // Desconectar: só a Gestão/Suporte... aqui o Admin; some a conta e a chave guardada.
        Assert.Equal(HttpStatusCode.Forbidden, (await coordenador.PostAsync("/api/pagamentos/conta/desconectar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsync("/api/pagamentos/conta/desconectar", null)).StatusCode);
        using (var db = api.Contexto(ApiFactory.ClienteId))
        {
            Assert.False(await db.ContasPagamento.AnyAsync());
            Assert.Contains(await db.LogsAuditoria.Where(l => l.EntidadeTipo == nameof(ContaPagamento)).Select(l => l.Detalhe).ToListAsync(),
                d => d != null && d.Contains("Conta Asaas própria desconectada"));
        }
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsync("/api/pagamentos/conta/desconectar", null)).StatusCode);
    }
}
