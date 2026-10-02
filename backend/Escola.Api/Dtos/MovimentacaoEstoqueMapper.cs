using Escola.Domain.Entities;

namespace Escola.Api.Dtos;

public static class MovimentacaoEstoqueMapper
{
    /// <summary>Exige Produto, Fornecedor e Usuario já carregados (Include).</summary>
    public static MovimentacaoEstoqueDto ToDto(this MovimentacaoEstoque m) => new(
        m.Id,
        m.ProdutoId,
        m.Produto.Nome,
        m.Produto.UnidadeMedida,
        m.Tipo,
        m.Quantidade,
        m.Data,
        m.FornecedorId,
        m.Fornecedor?.Nome,
        m.Observacao,
        m.Usuario.Nome,
        m.RegistradoEm);
}
