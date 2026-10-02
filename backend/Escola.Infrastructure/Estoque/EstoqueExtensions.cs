using Escola.Domain.Enums;
using Escola.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Escola.Infrastructure.Estoque;

public static class EstoqueExtensions
{
    /// <summary>Saldo atual (entradas − saídas) por produto. Produto sem nenhuma movimentação não aparece
    /// no dicionário — quem consulta deve tratar a ausência como saldo zero.</summary>
    public static async Task<Dictionary<Guid, decimal>> SaldosAsync(this EscolaDbContext db, IReadOnlyCollection<Guid>? produtoIds = null)
    {
        var query = db.MovimentacoesEstoque.AsQueryable();
        if (produtoIds is not null) query = query.Where(m => produtoIds.Contains(m.ProdutoId));

        var saldos = await query
            .GroupBy(m => m.ProdutoId)
            .Select(g => new
            {
                ProdutoId = g.Key,
                Saldo = g.Sum(m => m.Tipo == TipoMovimentacaoEstoque.Entrada ? m.Quantidade : -m.Quantidade)
            })
            .ToListAsync();

        return saldos.ToDictionary(s => s.ProdutoId, s => s.Saldo);
    }
}
