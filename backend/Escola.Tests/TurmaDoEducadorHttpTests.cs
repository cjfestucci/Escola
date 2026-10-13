using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Escola.Domain.Entities;
using Escola.Domain.Enums;

namespace Escola.Tests;

/// <summary>O Educador só lança (chamada, jogo, convocação, rotina, diário) nas turmas em que é professor; ver continua liberado.
/// Gestão lança em qualquer turma.</summary>
public class TurmaDoEducadorHttpTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    private const string Senha = "Senha-de-Teste-1";

    private async Task<(HttpClient Http, Guid UsuarioId)> EducadorDaTurmaAsync(Guid turmaId)
    {
        var email = $"educador.{Guid.NewGuid():N}@escola.test";
        var usuario = api.CriarUsuario(PapelUsuario.Educador, email);
        using (var db = api.Contexto(ApiFactory.ClienteId))
        {
            db.TurmaEducadores.Add(new TurmaEducador { TurmaId = turmaId, UsuarioId = usuario.Id });
            await db.SaveChangesAsync();
        }
        var resposta = await api.EntrarAsync(api.CreateClient(), email, Senha);
        resposta.EnsureSuccessStatusCode();
        var http = api.CreateClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            (await resposta.Content.ReadFromJsonAsync<ApiFactory.LoginResposta>())!.Token);
        return (http, usuario.Id);
    }

    private async Task<(Guid TurmaId, Guid AlunoId)> TurmaComAtletaAsync()
    {
        using var db = api.Contexto(ApiFactory.ClienteId);
        var turma = BancoDeTeste.NovaTurma(db, $"Sub-{Guid.NewGuid():N}"[..10]);
        var aluno = new Aluno { Id = Guid.NewGuid(), Nome = "Atleta", DataNascimento = new DateOnly(2014, 1, 1), TurmaId = turma.Id, MatriculaConfirmadaEm = DateTime.UtcNow };
        db.Alunos.Add(aluno);
        await db.SaveChangesAsync();
        return (turma.Id, aluno.Id);
    }

    private static object Chamada(Guid alunoId) => new
    {
        data = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)),
        itens = new[] { new { alunoId, status = "Presente" } }
    };

    private static object Jogo(Guid turmaId) => new
    {
        campeonatoId = (Guid?)null, turmaId, adversario = "Adversário FC", data = new DateOnly(2030, 5, 10), hora = "10:00",
        local = (string?)null, mando = "Casa", status = "Agendado", golsPro = (int?)null, golsContra = (int?)null, observacao = (string?)null
    };

    [Fact]
    public async Task EducadorSoLancaNasPropriasTurmasEGestaoEmQualquerUma()
    {
        var (minhaTurma, meuAtleta) = await TurmaComAtletaAsync();
        var (outraTurma, outroAtleta) = await TurmaComAtletaAsync();
        var (educador, educadorId) = await EducadorDaTurmaAsync(minhaTurma);

        // Chamada
        Assert.Equal(HttpStatusCode.OK, (await educador.PutAsJsonAsync($"/api/turmas/{minhaTurma}/presenca", Chamada(meuAtleta))).StatusCode);
        var proibida = await educador.PutAsJsonAsync($"/api/turmas/{outraTurma}/presenca", Chamada(outroAtleta));
        Assert.Equal(HttpStatusCode.Forbidden, proibida.StatusCode);
        Assert.Contains("turmas em que é professor", await proibida.Content.ReadAsStringAsync());
        // Ver a chamada de outra turma continua liberado.
        Assert.Equal(HttpStatusCode.OK, (await educador.GetAsync($"/api/turmas/{outraTurma}/presenca?data={DateTime.UtcNow.AddDays(-2):yyyy-MM-dd}")).StatusCode);

        // Jogo e convocação
        Assert.Equal(HttpStatusCode.Forbidden, (await educador.PostAsJsonAsync("/api/jogos", Jogo(outraTurma))).StatusCode);
        var criado = await educador.PostAsJsonAsync("/api/jogos", Jogo(minhaTurma));
        Assert.True(criado.IsSuccessStatusCode, await criado.Content.ReadAsStringAsync());

        var coordenador = await api.ClienteLogadoAsync(PapelUsuario.Coordenador);
        var jogoDeOutra = await coordenador.PostAsJsonAsync("/api/jogos", Jogo(outraTurma));
        Assert.True(jogoDeOutra.IsSuccessStatusCode);
        var jogoId = (await jogoDeOutra.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Forbidden, (await educador.PutAsJsonAsync($"/api/jogos/{jogoId}/convocacao", new { convocados = Array.Empty<object>() })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await educador.PostAsync($"/api/jogos/{jogoId}/cancelar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await educador.GetAsync($"/api/jogos/{jogoId}")).StatusCode);

        // Rotina (pela turma do atleta) e diário
        Assert.Equal(HttpStatusCode.Forbidden, (await educador.PostAsJsonAsync($"/api/alunos/{outroAtleta}/rotina/momento",
            new { usuarioId = educadorId, observacao = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await educador.PostAsJsonAsync($"/api/alunos/{meuAtleta}/rotina/momento",
            new { usuarioId = educadorId, observacao = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await educador.PostAsJsonAsync($"/api/turmas/{outraTurma}/diario",
            new { usuarioId = educadorId, titulo = "Treino" })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await educador.PostAsJsonAsync($"/api/turmas/{minhaTurma}/diario",
            new { usuarioId = educadorId, titulo = "Treino" })).StatusCode);

        // Gestão lança em qualquer turma.
        Assert.Equal(HttpStatusCode.OK, (await coordenador.PutAsJsonAsync($"/api/turmas/{outraTurma}/presenca", Chamada(outroAtleta))).StatusCode);

        // As telas descobrem as turmas do professor por aqui (antes respondia 500 e as telas caíam em "todas as turmas").
        var minhas = await educador.GetFromJsonAsync<List<System.Text.Json.JsonElement>>($"/api/usuarios/{educadorId}/turmas");
        Assert.Equal(minhaTurma, Assert.Single(minhas!).GetProperty("id").GetGuid());
    }
}
