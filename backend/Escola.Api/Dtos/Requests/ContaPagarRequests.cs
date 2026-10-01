namespace Escola.Api.Dtos.Requests;

public record CriarContaPagarRequest(Guid FornecedorId, string Descricao, decimal Valor, DateOnly Vencimento);

public record EditarContaPagarRequest(Guid FornecedorId, string Descricao, decimal Valor, DateOnly Vencimento);
