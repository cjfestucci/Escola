using Escola.Domain.Entities;

namespace Escola.Api.Dtos;

public static class ProdutoMapper
{
    public static ProdutoDto ToDto(this Produto p, decimal saldoAtual) => new(
        p.Id,
        p.Nome,
        p.Codigo,
        p.UnidadeMedida,
        p.EstoqueMinimo,
        p.Ativo,
        saldoAtual,
        p.Ativo && p.EstoqueMinimo > 0 && saldoAtual > 0 && saldoAtual <= p.EstoqueMinimo);
}
