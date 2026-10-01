namespace Escola.Api.Dtos;

public record FornecedorDto(
    Guid Id,
    string Nome,
    string? Documento,
    string? Telefone,
    string? Email,
    bool Ativo);
