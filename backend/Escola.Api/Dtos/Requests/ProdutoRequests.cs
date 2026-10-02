namespace Escola.Api.Dtos.Requests;

public record CriarOuEditarProdutoRequest(string Nome, string? Codigo, string UnidadeMedida, decimal EstoqueMinimo);
