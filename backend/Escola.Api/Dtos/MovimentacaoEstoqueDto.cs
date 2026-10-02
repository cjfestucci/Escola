using Escola.Domain.Enums;

namespace Escola.Api.Dtos;

public record MovimentacaoEstoqueDto(
    Guid Id,
    Guid ProdutoId,
    string ProdutoNome,
    string UnidadeMedida,
    TipoMovimentacaoEstoque Tipo,
    decimal Quantidade,
    DateOnly Data,
    Guid? FornecedorId,
    string? FornecedorNome,
    string? Observacao,
    string RegistradoPorNome,
    DateTime RegistradoEm);
