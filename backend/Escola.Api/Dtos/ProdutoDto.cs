namespace Escola.Api.Dtos;

public record ProdutoDto(
    Guid Id,
    string Nome,
    string? Codigo,
    string UnidadeMedida,
    decimal EstoqueMinimo,
    bool Ativo,
    decimal SaldoAtual,
    bool EstoqueBaixo);
