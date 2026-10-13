using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Escola.Tests;

/// <summary>Perfil Secretaria: matrícula, recebimento no balcão e estoque. Sem desconto, cancelamento, lote, contas, configurações,
/// equipe, exportação LGPD nem lançamentos de turma.</summary>
public class SecretariaHttpTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static object Matricula(Guid turmaId, decimal desconto = 0) => new
    {
        nome = "Atleta da Secretaria",
        dataNascimento = new DateOnly(2015, 3, 1),
        turmaId,
        fotoUrl = (string?)null,
        responsaveis = new[] { new { id = (Guid?)null, nome = "Mãe", email = $"mae.{Guid.NewGuid():N}@exemplo.test", telefone = (string?)null, responsavelFinanceiro = true } },
        descontoMensalidadePercentual = desconto,
        motivoDesconto = desconto > 0 ? "Bolsa" : null
    };

    [Fact]
    public async Task SecretariaMatriculaRecebeNoBalcaoEMexeNoEstoqueMasNaoDaDescontoNemCancela()
    {
        Guid turmaId;
        using (var db = api.Contexto(ApiFactory.ClienteId))
        {
            turmaId = BancoDeTeste.NovaTurma(db, $"Sub-{Guid.NewGuid():N}"[..10]).Id;
            await db.SaveChangesAsync();
        }
        var secretaria = await api.ClienteLogadoAsync(PapelUsuario.Secretaria);

        // Matrícula sim; com desconto, não.
        Assert.Equal(HttpStatusCode.BadRequest, (await secretaria.PostAsJsonAsync("/api/alunos", Matricula(turmaId, desconto: 10))).StatusCode);
        var criado = await secretaria.PostAsJsonAsync("/api/alunos", Matricula(turmaId));
        Assert.True(criado.IsSuccessStatusCode, await criado.Content.ReadAsStringAsync());
        var alunoId = (await criado.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("id").GetGuid();

        // Recebimento no balcão: lança avulsa e marca como paga; editar o valor, desmarcar e cancelar ficam com o financeiro.
        var cobranca = await secretaria.PostAsJsonAsync("/api/financeiro/cobrancas",
            new { alunoId, descricao = "Uniforme", valor = 80m, vencimento = new DateOnly(2030, 1, 10) });
        Assert.True(cobranca.IsSuccessStatusCode, await cobranca.Content.ReadAsStringAsync());
        var cobrancaId = (await cobranca.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await secretaria.PostAsync($"/api/financeiro/cobrancas/{cobrancaId}/marcar-paga", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await secretaria.PostAsync($"/api/financeiro/cobrancas/{cobrancaId}/desmarcar-paga", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await secretaria.PostAsync($"/api/financeiro/cobrancas/{cobrancaId}/cancelar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await secretaria.PutAsJsonAsync($"/api/financeiro/cobrancas/{cobrancaId}",
            new { descricao = "Uniforme", valor = 1m, vencimento = new DateOnly(2030, 1, 10) })).StatusCode);

        // Estoque: venda de uniforme no balcão.
        var produto = await secretaria.PostAsJsonAsync("/api/estoque/produtos", new { nome = $"Camisa {Guid.NewGuid():N}", codigo = (string?)null, unidadeMedida = "un", estoqueMinimo = 0m });
        Assert.True(produto.IsSuccessStatusCode, await produto.Content.ReadAsStringAsync());
        var produtoId = (await produto.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("id").GetGuid();
        var entrada = await secretaria.PostAsJsonAsync("/api/estoque/movimentacoes",
            new { produtoId, tipo = "Entrada", quantidade = 5m, data = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)), fornecedorId = (Guid?)null, observacao = (string?)null });
        Assert.True(entrada.IsSuccessStatusCode, await entrada.Content.ReadAsStringAsync());

        // Consulta turmas, mas não lança chamada.
        Assert.Equal(HttpStatusCode.OK, (await secretaria.GetAsync("/api/turmas")).StatusCode);
        var chamada = await secretaria.PutAsJsonAsync($"/api/turmas/{turmaId}/presenca",
            new { data = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)), itens = new[] { new { alunoId, status = "Presente" } } });
        Assert.Equal(HttpStatusCode.Forbidden, chamada.StatusCode);
        Assert.Contains("secretaria consulta", await chamada.Content.ReadAsStringAsync());

        using (var db = api.Contexto(ApiFactory.ClienteId))
            Assert.Equal(0m, (await db.Alunos.SingleAsync(a => a.Id == alunoId)).DescontoMensalidadePercentual);
    }

    [Fact]
    public async Task EditarMatriculaSemMexerNoDescontoPassaMudarODescontoNao()
    {
        Guid turmaId, alunoId;
        using (var db = api.Contexto(ApiFactory.ClienteId))
        {
            var turma = BancoDeTeste.NovaTurma(db, $"Sub-{Guid.NewGuid():N}"[..10]);
            var aluno = new Aluno { Id = Guid.NewGuid(), Nome = "Bolsista", DataNascimento = new DateOnly(2014, 2, 2), TurmaId = turma.Id,
                DescontoMensalidadePercentual = 50, MotivoDesconto = "Bolsa", MatriculaConfirmadaEm = DateTime.UtcNow };
            db.Alunos.Add(aluno);
            await db.SaveChangesAsync();
            (turmaId, alunoId) = (turma.Id, aluno.Id);
        }
        var secretaria = await api.ClienteLogadoAsync(PapelUsuario.Secretaria);

        object Edicao(decimal desconto) => new
        {
            nome = "Bolsista Editado", dataNascimento = new DateOnly(2014, 2, 2), turmaId, fotoUrl = (string?)null,
            responsaveis = new[] { new { id = (Guid?)null, nome = "Pai", email = $"pai.{Guid.NewGuid():N}@exemplo.test", telefone = (string?)null, responsavelFinanceiro = true } },
            descontoMensalidadePercentual = desconto, motivoDesconto = "Bolsa"
        };

        Assert.Equal(HttpStatusCode.BadRequest, (await secretaria.PutAsJsonAsync($"/api/alunos/{alunoId}", Edicao(100))).StatusCode);
        var ok = await secretaria.PutAsJsonAsync($"/api/alunos/{alunoId}", Edicao(50));
        Assert.True(ok.IsSuccessStatusCode, await ok.Content.ReadAsStringAsync());
    }
}
