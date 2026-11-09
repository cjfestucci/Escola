using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Escola.Domain.Entities;
using Escola.Domain.Enums;

namespace Escola.Tests;

/// <summary>"Divulgar convocação": o técnico avisa por e-mail os responsáveis dos atletas da turma. Cada responsável recebe só a situação
/// dos próprios filhos.</summary>
public class DivulgacaoConvocacaoHttpTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private sealed record Cenario(Guid TurmaId, Guid JogoId, string EmailPai, string EmailMae, string EmailSoDoNaoConvocado, Guid AtletaA, Guid AtletaB, Guid AtletaC);

    /// <summary>Turma com três atletas: A e B convocados, C não convocado. O responsável do A só tem o A; a mãe tem B e C;
    /// um terceiro responsável só tem o C.</summary>
    private async Task<(HttpClient Coordenador, Cenario Dados)> MontarAsync(bool convocar = true)
    {
        var sufixo = Guid.NewGuid().ToString("N")[..8];
        var emailPai = $"pai.{sufixo}@pais.test";
        var emailMae = $"mae.{sufixo}@pais.test";
        var emailOutro = $"outro.{sufixo}@pais.test";

        Guid turmaId, a, b, c;
        using (var db = api.Contexto(ApiFactory.ClienteId))
        {
            var turma = BancoDeTeste.NovaTurma(db, $"Sub-{sufixo}");
            Aluno Novo(string nome) => new() { Id = Guid.NewGuid(), Nome = nome, DataNascimento = new DateOnly(2014, 1, 1), TurmaId = turma.Id, MatriculaConfirmadaEm = DateTime.UtcNow };
            var alunoA = Novo("Atleta Alfa");
            var alunoB = Novo("Atleta Beta");
            var alunoC = Novo("Atleta Gama");
            db.Alunos.AddRange(alunoA, alunoB, alunoC);

            Responsavel Resp(string nome, string email) => new() { Id = Guid.NewGuid(), Nome = nome, Email = email };
            var pai = Resp("Pai Alfa", emailPai);
            var mae = Resp("Mãe Beta e Gama", emailMae);
            var outro = Resp("Outro Responsável", emailOutro);
            db.Responsaveis.AddRange(pai, mae, outro);
            db.AlunoResponsaveis.AddRange(
                new AlunoResponsavel { AlunoId = alunoA.Id, ResponsavelId = pai.Id },
                new AlunoResponsavel { AlunoId = alunoB.Id, ResponsavelId = mae.Id },
                new AlunoResponsavel { AlunoId = alunoC.Id, ResponsavelId = mae.Id },
                new AlunoResponsavel { AlunoId = alunoC.Id, ResponsavelId = outro.Id });
            await db.SaveChangesAsync();
            (turmaId, a, b, c) = (turma.Id, alunoA.Id, alunoB.Id, alunoC.Id);
        }

        var coordenador = await api.ClienteLogadoAsync(PapelUsuario.Coordenador);
        var criado = await coordenador.PostAsJsonAsync("/api/jogos", new
        {
            campeonatoId = (Guid?)null, turmaId, adversario = "Time Adversário", data = new DateOnly(2030, 5, 10), hora = "10:00",
            local = "Campo Central", mando = "Casa", status = "Agendado", golsPro = (int?)null, golsContra = (int?)null, observacao = (string?)null
        });
        criado.EnsureSuccessStatusCode();
        var jogoId = (await criado.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("id").GetGuid();

        if (convocar)
        {
            var salvo = await coordenador.PutAsJsonAsync($"/api/jogos/{jogoId}/convocacao", new
            {
                convocados = new[]
                {
                    new { alunoId = a, gols = 0, cartoesAmarelos = 0, cartaoVermelho = false },
                    new { alunoId = b, gols = 0, cartoesAmarelos = 0, cartaoVermelho = false }
                }
            });
            salvo.EnsureSuccessStatusCode();
        }

        return (coordenador, new Cenario(turmaId, jogoId, emailPai, emailMae, emailOutro, a, b, c));
    }

    private string Corpo(string para)
    {
        lock (api.Email.Enviados) return api.Email.Enviados.Single(e => e.Para == para).Corpo;
    }

    [Fact]
    public async Task CadaResponsavelRecebeSoOsProprioFilhosEOJogoFicaMarcadoComoDivulgado()
    {
        var (coordenador, d) = await MontarAsync();

        var resposta = await coordenador.PostAsync($"/api/jogos/{d.JogoId}/convocacao/divulgar", null);
        Assert.True(resposta.IsSuccessStatusCode, await resposta.Content.ReadAsStringAsync());
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal(3, corpo.GetProperty("responsaveis").GetInt32()); // pai, mãe e o terceiro responsável
        Assert.Equal(0, corpo.GetProperty("falhas").GetInt32());
        Assert.NotEqual(JsonValueKind.Null, corpo.GetProperty("jogo").GetProperty("convocacaoDivulgadaEm").ValueKind);
        Assert.False(corpo.GetProperty("jogo").GetProperty("convocacaoAlteradaDepoisDaDivulgacao").GetBoolean());

        // O pai do A só vê o A (convocado): nem o nome nem a situação dos outros atletas.
        var doPai = Corpo(d.EmailPai);
        Assert.Contains("Atleta Alfa", doPai);
        Assert.Contains("convocado", doPai);
        Assert.DoesNotContain(System.Net.WebUtility.HtmlEncode("não convocado"), doPai);
        Assert.DoesNotContain("Atleta Beta", doPai);
        Assert.DoesNotContain("Atleta Gama", doPai);
        Assert.Contains(System.Net.WebUtility.HtmlEncode("Time Adversário"), doPai); // o HTML codifica os acentos

        // A mãe tem dois filhos: um e-mail só, com os dois (B convocado, C não convocado).
        var daMae = Corpo(d.EmailMae);
        Assert.Contains("Atleta Beta", daMae);
        Assert.Contains("Atleta Gama", daMae);
        Assert.Contains(System.Net.WebUtility.HtmlEncode("não convocado para este jogo"), daMae);
        Assert.DoesNotContain("Atleta Alfa", daMae);

        // Quem só tem o filho não convocado também é avisado.
        var doOutro = Corpo(d.EmailSoDoNaoConvocado);
        Assert.Contains("Atleta Gama", doOutro);
        Assert.DoesNotContain("Atleta Alfa", doOutro);

        // Fica registrado no histórico do jogo.
        var logs = await coordenador.GetFromJsonAsync<JsonElement>($"/api/logs?entidadeTipo=Jogo&entidadeId={d.JogoId}", Json);
        Assert.Contains(logs.EnumerateArray(), l => l.GetProperty("detalhe").GetString()!.Contains("Convocação divulgada por e-mail: 3 responsáveis avisados"));
    }

    [Fact]
    public async Task NaoDivulgaSemConvocadosNemDuasVezesSeguidasEAlterarDepoisMarcaParaDivulgarDeNovo()
    {
        var (coordenador, d) = await MontarAsync(convocar: false);

        var semConvocados = await coordenador.PostAsync($"/api/jogos/{d.JogoId}/convocacao/divulgar", null);
        Assert.Equal(HttpStatusCode.BadRequest, semConvocados.StatusCode);
        Assert.Contains("Convoque ao menos um atleta", await semConvocados.Content.ReadAsStringAsync());

        var salvo = await coordenador.PutAsJsonAsync($"/api/jogos/{d.JogoId}/convocacao", new
        {
            convocados = new[] { new { alunoId = d.AtletaA, gols = 0, cartoesAmarelos = 0, cartaoVermelho = false } }
        });
        salvo.EnsureSuccessStatusCode();

        Assert.True((await coordenador.PostAsync($"/api/jogos/{d.JogoId}/convocacao/divulgar", null)).IsSuccessStatusCode);

        // Duas vezes seguidas: bloqueia (evita e-mail em dobro por duplo clique).
        var segunda = await coordenador.PostAsync($"/api/jogos/{d.JogoId}/convocacao/divulgar", null);
        Assert.Equal(HttpStatusCode.BadRequest, segunda.StatusCode);
        Assert.Contains("acabou de ser divulgada", await segunda.Content.ReadAsStringAsync());

        // Mudou a lista depois do aviso: o jogo passa a indicar que vale divulgar de novo.
        await Task.Delay(20);
        var alterado = await coordenador.PutAsJsonAsync($"/api/jogos/{d.JogoId}/convocacao", new
        {
            convocados = new[]
            {
                new { alunoId = d.AtletaA, gols = 0, cartoesAmarelos = 0, cartaoVermelho = false },
                new { alunoId = d.AtletaB, gols = 0, cartoesAmarelos = 0, cartaoVermelho = false }
            }
        });
        alterado.EnsureSuccessStatusCode();
        var jogo = await coordenador.GetFromJsonAsync<JsonElement>($"/api/jogos/{d.JogoId}", Json);
        Assert.True(jogo.GetProperty("convocacaoAlteradaDepoisDaDivulgacao").GetBoolean());
    }

    [Fact]
    public async Task EducadorDeOutraTurmaNaoDivulgaEJogoCanceladoNaoPode()
    {
        var (coordenador, d) = await MontarAsync();

        var educador = await api.ClienteLogadoAsync(PapelUsuario.Educador); // sem vínculo com a turma do jogo
        Assert.Equal(HttpStatusCode.Forbidden, (await educador.PostAsync($"/api/jogos/{d.JogoId}/convocacao/divulgar", null)).StatusCode);

        // Os responsáveis não acessam esta ação, nem o Suporte.
        var responsavel = await api.ClienteLogadoAsync(PapelUsuario.Responsavel);
        Assert.Equal(HttpStatusCode.Forbidden, (await responsavel.PostAsync($"/api/jogos/{d.JogoId}/convocacao/divulgar", null)).StatusCode);

        Assert.True((await coordenador.PostAsync($"/api/jogos/{d.JogoId}/cancelar", null)).IsSuccessStatusCode);
        var cancelado = await coordenador.PostAsync($"/api/jogos/{d.JogoId}/convocacao/divulgar", null);
        Assert.Equal(HttpStatusCode.BadRequest, cancelado.StatusCode);
        Assert.Contains("jogo agendado", await cancelado.Content.ReadAsStringAsync());
    }
}
