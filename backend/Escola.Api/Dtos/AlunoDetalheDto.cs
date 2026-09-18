namespace Escola.Api.Dtos;

public record AlunoDetalheDto(
    Guid Id,
    string Nome,
    DateOnly DataNascimento,
    string? FotoUrl,
    Guid TurmaId,
    string TurmaNome,
    IReadOnlyList<ResponsavelResumoDto> Responsaveis,
    IReadOnlyList<SenhaGeradaDto> SenhasGeradas)
{
    public AlunoDetalheDto(
        Guid Id, string Nome, DateOnly DataNascimento, string? FotoUrl, Guid TurmaId,
        string TurmaNome, IReadOnlyList<ResponsavelResumoDto> Responsaveis)
        : this(Id, Nome, DataNascimento, FotoUrl, TurmaId, TurmaNome, Responsaveis, [])
    {
    }
}
