namespace Escola.Api.Dtos.Requests;

public record CriarOuEditarContaReceberRequest(string Descricao, string? Origem, decimal Valor, DateOnly Vencimento);
