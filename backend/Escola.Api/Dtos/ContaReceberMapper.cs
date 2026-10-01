using Escola.Domain.Entities;

namespace Escola.Api.Dtos;

public static class ContaReceberMapper
{
    public static ContaReceberDto ToDto(this ContaReceber c) => new(
        c.Id,
        c.Descricao,
        c.Origem,
        c.Valor,
        c.Vencimento,
        c.Recebida,
        c.RecebidoEm,
        c.Cancelada,
        c.CanceladaEm);
}
