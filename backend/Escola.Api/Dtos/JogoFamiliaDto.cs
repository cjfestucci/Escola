using Escola.Domain.Enums;

namespace Escola.Api.Dtos;

/// <summary>Um jogo visto pela família de <b>um</b> atleta: dados do jogo + a situação desse atleta (convocação e,
/// depois do jogo, a participação dele). Nunca traz nome, gols ou cartões de outros atletas.</summary>
/// <param name="ConvocacaoDivulgada">A comissão já montou a convocação (ao menos um convocado). Antes disso, "não convocado" seria enganoso.</param>
public record JogoFamiliaDto(
    Guid Id,
    DateOnly Data,
    TimeOnly Hora,
    string? Local,
    LocalJogo Mando,
    string Adversario,
    string? CampeonatoNome,
    string TurmaNome,
    StatusJogo Status,
    int? GolsPro,
    int? GolsContra,
    bool ConvocacaoDivulgada,
    bool Convocado,
    bool Titular,
    int Gols,
    int CartoesAmarelos,
    bool CartaoVermelho);

public record JogosDoAlunoDto(IReadOnlyList<JogoFamiliaDto> Proximos, IReadOnlyList<JogoFamiliaDto> Recentes);

/// <param name="QuantidadeJogos">Jogos do time do atleta nesse campeonato (sem contar os cancelados).</param>
public record CampeonatoFamiliaDto(Guid Id, string Nome, DateOnly DataInicio, DateOnly? DataFim, bool Ativo, int QuantidadeJogos);

/// <summary>Campanha do <b>time do atleta</b> no campeonato (só jogos realizados da turma dele). Vitória = 3 pontos, empate = 1.</summary>
public record ResumoCampanhaDto(int Jogos, int Vitorias, int Empates, int Derrotas, int GolsPro, int GolsContra, int Pontos);

public record JogosCampeonatoDoAlunoDto(
    Guid CampeonatoId,
    string CampeonatoNome,
    DateOnly DataInicio,
    DateOnly? DataFim,
    ResumoCampanhaDto Resumo,
    IReadOnlyList<JogoFamiliaDto> Jogos);
