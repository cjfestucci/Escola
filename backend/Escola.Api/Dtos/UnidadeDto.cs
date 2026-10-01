namespace Escola.Api.Dtos;

public record UnidadeDto(
    Guid Id,
    string Nome,
    string? Endereco,
    string? Telefone,
    bool Ativa);
