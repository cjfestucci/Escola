using Escola.Domain.Entities;

namespace Escola.Api.Dtos;

public static class ContaPagarMapper
{
    public static ContaPagarDto ToDto(this ContaPagar c) => new(
        c.Id,
        c.FornecedorId,
        c.Fornecedor?.Nome ?? string.Empty,
        c.Descricao,
        c.Valor,
        c.Vencimento,
        c.Paga,
        c.PagoEm,
        c.Cancelada,
        c.CanceladaEm);
}
