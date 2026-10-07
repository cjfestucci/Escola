using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Escola.Tests;

/// <summary>Formas de pagamento ativas (Configurações → Financeiro): só o que está ativo é oferecido à família.</summary>
public class FormasPagamentoHttpTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static object Config(bool pix, bool boleto, bool presencial, string? instrucoes = null) => new
    {
        pixChave = "+5511999998888", pixTipoChave = "Telefone", pixNomeRecebedor = "Escola Teste", pixCidade = "Sao Paulo",
        pagamentoPixAtivo = pix, pagamentoBoletoAtivo = boleto, pagamentoPresencialAtivo = presencial,
        instrucoesPagamentoPresencial = instrucoes
    };

    private async Task<Guid> CriarCobrancaAsync()
    {
        using var db = api.Contexto(ApiFactory.ClienteId);
        var turma = BancoDeTeste.NovaTurma(db, $"Turma {Guid.NewGuid():N}");
        var aluno = new Aluno { Id = Guid.NewGuid(), Nome = "Aluno Pagamento", DataNascimento = new DateOnly(2015, 1, 1), TurmaId = turma.Id, MatriculaConfirmadaEm = DateTime.UtcNow };
        var cobranca = new Cobranca { Id = Guid.NewGuid(), AlunoId = aluno.Id, Descricao = "Mensalidade", Valor = 300m, Vencimento = new DateOnly(2030, 1, 10) };
        db.Alunos.Add(aluno);
        db.Cobrancas.Add(cobranca);
        await db.SaveChangesAsync();
        return cobranca.Id;
    }

    [Fact]
    public async Task PixDesligadoNaoGeraCodigoEFormasRefletemAConfiguracao()
    {
        var gestao = await api.ClienteLogadoAsync(PapelUsuario.Coordenador);
        var cobrancaId = await CriarCobrancaAsync();

        (await gestao.PutAsJsonAsync("/api/financeiro/configuracao", Config(pix: true, boleto: false, presencial: false))).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, (await gestao.GetAsync($"/api/financeiro/cobrancas/{cobrancaId}/pix")).StatusCode);

        var resposta = await gestao.PutAsJsonAsync("/api/financeiro/configuracao", Config(pix: false, boleto: true, presencial: true, "Secretaria, 8h às 17h"));
        resposta.EnsureSuccessStatusCode();

        var formas = await gestao.GetFromJsonAsync<JsonElement>("/api/financeiro/formas-pagamento", Json);
        Assert.False(formas.GetProperty("pix").GetBoolean());
        Assert.True(formas.GetProperty("boleto").GetBoolean());
        Assert.True(formas.GetProperty("presencial").GetBoolean());
        Assert.Equal("Secretaria, 8h às 17h", formas.GetProperty("instrucoesPresencial").GetString());

        var pix = await gestao.GetAsync($"/api/financeiro/cobrancas/{cobrancaId}/pix");
        Assert.Equal(HttpStatusCode.BadRequest, pix.StatusCode);
        Assert.Contains("não está habilitado", await pix.Content.ReadAsStringAsync());

        using (var db = api.Contexto(ApiFactory.ClienteId))
            Assert.Contains(await db.LogsAuditoria.Where(l => l.EntidadeTipo == "ConfiguracaoFinanceira").Select(l => l.Detalhe).ToListAsync(),
                d => d != null && d.Contains("Pagamento por Pix alterado de \"Sim\" para \"Não\""));

        // Volta ao padrão pros outros testes da mesma fábrica.
        (await gestao.PutAsJsonAsync("/api/financeiro/configuracao", Config(pix: true, boleto: false, presencial: false))).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task NaoDeixaDesligarTodasAsFormas()
    {
        var gestao = await api.ClienteLogadoAsync(PapelUsuario.Admin);
        var resposta = await gestao.PutAsJsonAsync("/api/financeiro/configuracao", Config(pix: false, boleto: false, presencial: false));
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Contains("ao menos uma forma de pagamento", await resposta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ResponsavelEnxergaAsFormasMasNaoAConfiguracaoCompleta()
    {
        var pai = await api.ClienteLogadoAsync(PapelUsuario.Responsavel);
        Assert.Equal(HttpStatusCode.OK, (await pai.GetAsync("/api/financeiro/formas-pagamento")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await pai.GetAsync("/api/financeiro/configuracao")).StatusCode);
    }
}
