using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Escola.Infrastructure.Competicoes;

namespace Escola.Api.Dtos;

public record DesempenhoDto(Guid TurmaId, string TurmaNome, int Jogos, int Vitorias, int Empates, int Derrotas, int GolsPro, int GolsContra, int Pontos);

public record ArtilheiroDto(Guid AlunoId, string AlunoNome, int Gols);

/// <summary>Suspenso ou pendurado, com a explicação pronta pra exibir. Derivado dos cartões das súmulas — não é gravado.</summary>
public record AlertaDisciplinarDto(Guid AlunoId, string AlunoNome, TipoAlertaDisciplinar Tipo, string Detalhe, int AmarelosAcumulados);

/// <summary>Desempenho, Artilheiros e Disciplina só vêm preenchidos no detalhe de um campeonato.</summary>
public record CampeonatoDto(
    Guid Id,
    string Nome,
    DateOnly DataInicio,
    DateOnly? DataFim,
    string? Observacao,
    int? AmarelosParaSuspensao,
    bool Ativo,
    int QuantidadeJogos,
    IReadOnlyList<DesempenhoDto>? Desempenho = null,
    IReadOnlyList<ArtilheiroDto>? Artilheiros = null,
    IReadOnlyList<AlertaDisciplinarDto>? Disciplina = null);

public record JogoAtletaDto(Guid AlunoId, string AlunoNome, bool Titular, int Gols, int CartoesAmarelos, bool CartaoVermelho);

/// <summary>Convocados e Alertas só vêm preenchidos no detalhe de um jogo (na listagem vão só as quantidades).
/// Suspensos/Pendurados/Alertas só existem em jogo Agendado de um campeonato com controle disciplinar.</summary>
public record JogoDto(
    Guid Id,
    Guid? CampeonatoId,
    string? CampeonatoNome,
    Guid TurmaId,
    string TurmaNome,
    string Adversario,
    DateOnly Data,
    TimeOnly Hora,
    string? Local,
    LocalJogo Mando,
    StatusJogo Status,
    int? GolsPro,
    int? GolsContra,
    string? Observacao,
    int QuantidadeConvocados,
    IReadOnlyList<JogoAtletaDto>? Convocados = null,
    int Suspensos = 0,
    int Pendurados = 0,
    IReadOnlyList<AlertaDisciplinarDto>? Alertas = null);

public static class CompeticaoMapper
{
    /// <summary>Exige Jogos carregados (Include) pra contar.</summary>
    public static CampeonatoDto ToDto(this Campeonato c) =>
        new(c.Id, c.Nome, c.DataInicio, c.DataFim, c.Observacao, c.AmarelosParaSuspensao, c.Ativo, c.Jogos.Count);

    public static AlertaDisciplinarDto ToDto(this AlertaDisciplinar a) =>
        new(a.AlunoId, a.AlunoNome, a.Tipo, a.Detalhe, a.AmarelosAcumulados);

    /// <summary>Exige Campeonato, Turma e Convocados carregados; com <paramref name="comConvocados"/> também Convocados.Aluno.</summary>
    public static JogoDto ToDto(this Jogo j, bool comConvocados = false) => new(
        j.Id,
        j.CampeonatoId,
        j.Campeonato?.Nome,
        j.TurmaId,
        j.Turma.Nome,
        j.Adversario,
        j.Data,
        j.Hora,
        j.Local,
        j.Mando,
        j.Status,
        j.GolsPro,
        j.GolsContra,
        j.Observacao,
        j.Convocados.Count,
        comConvocados
            ? j.Convocados
                .OrderByDescending(c => c.Titular).ThenBy(c => c.Aluno.Nome)
                .Select(c => new JogoAtletaDto(c.AlunoId, c.Aluno.Nome, c.Titular, c.Gols, c.CartoesAmarelos, c.CartaoVermelho))
                .ToList()
            : null);
}
