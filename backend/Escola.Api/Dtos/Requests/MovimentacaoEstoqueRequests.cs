using Escola.Domain.Enums;

namespace Escola.Api.Dtos.Requests;

public record CriarMovimentacaoEstoqueRequest(
    Guid ProdutoId,
    TipoMovimentacaoEstoque Tipo,
    decimal Quantidade,
    DateOnly Data,
    Guid? FornecedorId,
    string? Observacao);
