using Escola.Domain.Enums;

namespace Escola.Api.Dtos;

public record ChamadaItemDto(Guid AlunoId, string AlunoNome, string? FotoUrl, StatusPresenca? Status);

/// <param name="Registrada">Já existe chamada salva para a turma nesse dia (senão os itens vêm sem status).</param>
public record ChamadaDto(
    Guid TurmaId,
    DateOnly Data,
    bool Registrada,
    IReadOnlyList<ChamadaItemDto> Itens,
    int Presentes,
    int Faltas,
    int Justificadas);

/// <param name="Percentual">Presenças ÷ (presenças + faltas), em %. Nulo se ainda não há chamada registrada na janela.</param>
/// <param name="FaltasSeguidas">Faltas seguidas nas chamadas mais recentes (justificadas são ignoradas).</param>
public record FrequenciaAlunoDto(
    Guid AlunoId,
    string AlunoNome,
    int Presencas,
    int Faltas,
    int Justificadas,
    decimal? Percentual,
    int FaltasSeguidas);

public record PresencaRecenteDto(DateOnly Data, StatusPresenca Status);

/// <param name="De">Início do período efetivamente usado (inclusive).</param>
/// <param name="Ate">Fim do período efetivamente usado (inclusive; nunca depois de hoje).</param>
/// <param name="Recentes">Chamadas do período, da mais recente pra mais antiga (até 60).</param>
public record FrequenciaDoAlunoDto(FrequenciaAlunoDto Resumo, int JanelaDias, IReadOnlyList<PresencaRecenteDto> Recentes, DateOnly De, DateOnly Ate);

public record FaltosoDto(Guid AlunoId, string AlunoNome, Guid TurmaId, string TurmaNome, int FaltasSeguidas);
