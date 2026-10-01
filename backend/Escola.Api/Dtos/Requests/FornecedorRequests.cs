namespace Escola.Api.Dtos.Requests;

public record CriarOuEditarFornecedorRequest(string Nome, string? Documento, string? Telefone, string? Email);
