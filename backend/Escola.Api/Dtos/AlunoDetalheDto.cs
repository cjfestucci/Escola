namespace Escola.Api.Dtos;

public record AlunoDetalheDto(
    Guid Id,
    string Nome,
    DateOnly DataNascimento,
    string? FotoUrl,
    Guid TurmaId,
    string TurmaNome,
    IReadOnlyList<ResponsavelResumoDto> Responsaveis);
