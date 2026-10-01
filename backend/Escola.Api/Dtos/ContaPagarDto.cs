namespace Escola.Api.Dtos;

public record ContaPagarDto(
    Guid Id,
    Guid FornecedorId,
    string FornecedorNome,
    string Descricao,
    decimal Valor,
    DateOnly Vencimento,
    bool Paga,
    DateOnly? PagoEm,
    bool Cancelada,
    DateOnly? CanceladaEm);
