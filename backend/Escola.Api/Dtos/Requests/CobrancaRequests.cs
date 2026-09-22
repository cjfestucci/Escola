namespace Escola.Api.Dtos.Requests;

public record CriarCobrancaRequest(Guid AlunoId, string Descricao, decimal Valor, DateOnly Vencimento);

public record EditarCobrancaRequest(string Descricao, decimal Valor, DateOnly Vencimento);
