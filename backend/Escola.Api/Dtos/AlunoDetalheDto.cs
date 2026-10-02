using Escola.Domain.Enums;

namespace Escola.Api.Dtos;

public record AlunoDetalheDto(
    Guid Id,
    string Nome,
    DateOnly DataNascimento,
    string? FotoUrl,
    Guid TurmaId,
    string TurmaNome,
    bool Ativo,
    IReadOnlyList<ResponsavelResumoDto> Responsaveis,
    IReadOnlyList<SenhaGeradaDto> SenhasGeradas,
    PosicaoAtleta? Posicao = null)
{
    public AlunoDetalheDto(
        Guid Id, string Nome, DateOnly DataNascimento, string? FotoUrl, Guid TurmaId,
        string TurmaNome, bool Ativo, IReadOnlyList<ResponsavelResumoDto> Responsaveis, PosicaoAtleta? Posicao = null)
        : this(Id, Nome, DataNascimento, FotoUrl, TurmaId, TurmaNome, Ativo, Responsaveis, [], Posicao)
    {
    }
}
