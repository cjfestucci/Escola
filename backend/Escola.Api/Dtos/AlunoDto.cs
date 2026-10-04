using Escola.Domain.Enums;

namespace Escola.Api.Dtos;

public record AlunoDto(
    Guid Id,
    string Nome,
    DateOnly DataNascimento,
    string? FotoUrl,
    Guid TurmaId,
    string TurmaNome,
    bool Ativo,
    PosicaoAtleta? Posicao = null,
    bool Bloqueado = false);
