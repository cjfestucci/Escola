using Escola.Api.Auth;
using Escola.Api.Dtos;
using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Escola.Infrastructure.Data;
using Escola.Infrastructure.Tempo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Escola.Api.Controllers;

/// <summary>Jogos e convocação do ponto de vista da família de um atleta (Portal da Família). Fica fora do
/// <c>JogosController</c> porque aquele é restrito à Equipe — o Responsável precisa de uma visão própria, mais enxuta
/// e que só expõe o que diz respeito ao filho. Equipe também pode consultar (ver como a família vê).</summary>
[ApiController]
[Route("api/alunos/{alunoId:guid}")]
[Authorize(Roles = $"{GruposDePapeis.Equipe},{GruposDePapeis.Responsavel}")]
public class JogosFamiliaController(EscolaDbContext db, IRelogioEscola relogio) : ControllerBase
{
    private const int MaxProximos = 5;
    private const int MaxRecentes = 5;

    private sealed record AlunoDoPortal(Guid Id, Guid TurmaId);

    /// <summary>Próximos jogos e últimos resultados do atleta, com a convocação dele.</summary>
    [HttpGet("jogos")]
    public async Task<ActionResult<JogosDoAlunoDto>> Listar(Guid alunoId)
    {
        var (aluno, erro) = await AutorizarAsync(alunoId);
        if (erro is not null) return erro;

        var hoje = await relogio.HojeAsync();
        var jogos = await CarregarJogosAsync(aluno!, campeonatoId: null);

        // Próximos: ainda por vir, agendados ou cancelados (a família precisa saber que um jogo foi cancelado).
        var proximos = jogos
            .Where(x => x.Jogo.Data >= hoje && x.Jogo.Status != StatusJogo.Realizado)
            .OrderBy(x => x.Jogo.Data).ThenBy(x => x.Jogo.Hora)
            .Take(MaxProximos)
            .Select(ParaDto)
            .ToList();

        var recentes = jogos
            .Where(x => x.Jogo.Status == StatusJogo.Realizado)
            .OrderByDescending(x => x.Jogo.Data).ThenByDescending(x => x.Jogo.Hora)
            .Take(MaxRecentes)
            .Select(ParaDto)
            .ToList();

        return Ok(new JogosDoAlunoDto(proximos, recentes));
    }

    /// <summary>Campeonatos em que o time do atleta tem jogos (ou em que ele foi convocado), do mais recente pro mais antigo.</summary>
    [HttpGet("campeonatos")]
    public async Task<ActionResult<List<CampeonatoFamiliaDto>>> ListarCampeonatos(Guid alunoId)
    {
        var (aluno, erro) = await AutorizarAsync(alunoId);
        if (erro is not null) return erro;

        var jogos = await CarregarJogosAsync(aluno!, campeonatoId: null);
        var campeonatos = jogos
            .Where(x => x.Jogo.Campeonato is not null)
            .GroupBy(x => x.Jogo.Campeonato!)
            .Select(g => new CampeonatoFamiliaDto(
                g.Key.Id, g.Key.Nome, g.Key.DataInicio, g.Key.DataFim, g.Key.Ativo,
                g.Count(x => x.Jogo.Status != StatusJogo.Cancelado)))
            .OrderByDescending(c => c.Ativo).ThenByDescending(c => c.DataInicio)
            .ToList();

        return Ok(campeonatos);
    }

    /// <summary>Calendário completo do time do atleta num campeonato (passados e futuros, em ordem de data) com o resumo do time.
    /// O resumo conta só os jogos <b>da turma</b> do atleta — um jogo em que ele atuou "por cima" aparece na lista mas não entra
    /// na campanha do time dele.</summary>
    [HttpGet("campeonatos/{campeonatoId:guid}/jogos")]
    public async Task<ActionResult<JogosCampeonatoDoAlunoDto>> JogosDoCampeonato(Guid alunoId, Guid campeonatoId)
    {
        var (aluno, erro) = await AutorizarAsync(alunoId);
        if (erro is not null) return erro;

        var jogos = await CarregarJogosAsync(aluno!, campeonatoId);
        var campeonato = jogos.Select(x => x.Jogo.Campeonato).FirstOrDefault(c => c is not null)
            ?? await db.Campeonatos.FirstOrDefaultAsync(c => c.Id == campeonatoId);
        if (campeonato is null) return NotFound("Campeonato não encontrado.");

        var doTime = jogos.Where(x => x.Jogo.TurmaId == aluno!.TurmaId && x.Jogo.Status == StatusJogo.Realizado
            && x.Jogo.GolsPro is not null && x.Jogo.GolsContra is not null).Select(x => x.Jogo).ToList();
        var vitorias = doTime.Count(j => j.GolsPro > j.GolsContra);
        var empates = doTime.Count(j => j.GolsPro == j.GolsContra);
        var derrotas = doTime.Count(j => j.GolsPro < j.GolsContra);
        var resumo = new ResumoCampanhaDto(doTime.Count, vitorias, empates, derrotas,
            doTime.Sum(j => j.GolsPro!.Value), doTime.Sum(j => j.GolsContra!.Value), vitorias * 3 + empates);

        var lista = jogos
            .OrderBy(x => x.Jogo.Data).ThenBy(x => x.Jogo.Hora)
            .Select(ParaDto)
            .ToList();

        return Ok(new JogosCampeonatoDoAlunoDto(campeonato.Id, campeonato.Nome, campeonato.DataInicio, campeonato.DataFim, resumo, lista));
    }

    // ----- helpers -----

    /// <summary>Equipe vê qualquer aluno; um Responsável só o(s) próprio(s) filho(s) (claim <c>responsavelId</c>).</summary>
    private async Task<(AlunoDoPortal? Aluno, ActionResult? Erro)> AutorizarAsync(Guid alunoId)
    {
        var aluno = await db.Alunos.Where(a => a.Id == alunoId).Select(a => new AlunoDoPortal(a.Id, a.TurmaId)).FirstOrDefaultAsync();
        if (aluno is null) return (null, NotFound("Aluno não encontrado."));

        if (User.IsInRole("Responsavel"))
        {
            var responsavelId = User.FindFirst("responsavelId")?.Value;
            var ehFilho = await db.AlunoResponsaveis.AnyAsync(ar => ar.AlunoId == alunoId && ar.ResponsavelId.ToString() == responsavelId);
            if (!ehFilho) return (null, Forbid());
        }

        return (aluno, null);
    }

    private sealed record LinhaJogo(Jogo Jogo, JogoAtleta? Participacao, bool TemConvocacao);

    /// <summary>Jogos da turma atual do atleta + qualquer jogo em que ele foi convocado (ex.: jogando "por cima" de outra categoria),
    /// opcionalmente só de um campeonato.</summary>
    private async Task<List<LinhaJogo>> CarregarJogosAsync(AlunoDoPortal aluno, Guid? campeonatoId)
    {
        var query = db.Jogos
            .Include(j => j.Campeonato)
            .Include(j => j.Turma)
            .Where(j => j.TurmaId == aluno.TurmaId || j.Convocados.Any(c => c.AlunoId == aluno.Id));
        if (campeonatoId is { } cid) query = query.Where(j => j.CampeonatoId == cid);

        var linhas = await query
            .Select(j => new
            {
                Jogo = j,
                Participacao = j.Convocados.FirstOrDefault(c => c.AlunoId == aluno.Id),
                TemConvocacao = j.Convocados.Any()
            })
            .ToListAsync();
        return linhas.Select(l => new LinhaJogo(l.Jogo, l.Participacao, l.TemConvocacao)).ToList();
    }

    private static JogoFamiliaDto ParaDto(LinhaJogo x) => new(
        x.Jogo.Id, x.Jogo.Data, x.Jogo.Hora, x.Jogo.Local, x.Jogo.Mando, x.Jogo.Adversario,
        x.Jogo.Campeonato?.Nome, x.Jogo.Turma.Nome, x.Jogo.Status, x.Jogo.GolsPro, x.Jogo.GolsContra,
        x.TemConvocacao, x.Participacao is not null,
        x.Participacao?.Titular ?? false, x.Participacao?.Gols ?? 0, x.Participacao?.CartoesAmarelos ?? 0, x.Participacao?.CartaoVermelho ?? false);
}
