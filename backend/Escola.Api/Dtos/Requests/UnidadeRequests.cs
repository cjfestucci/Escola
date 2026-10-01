namespace Escola.Api.Dtos.Requests;

public record CriarOuEditarUnidadeRequest(string Nome, string? Endereco, string? Telefone, bool Ativa);
