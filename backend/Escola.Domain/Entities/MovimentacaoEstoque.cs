using Escola.Domain.Enums;

namespace Escola.Domain.Entities;

/// <summary>Lançamento imutável de entrada ou saída de um produto — não existe edição nem exclusão:
/// um erro se corrige com uma movimentação contrária, mantendo o histórico completo e auditável.</summary>
public class MovimentacaoEstoque
{
    public Guid Id { get; set; }
    public Guid ProdutoId { get; set; }
    public Produto Produto { get; set; } = null!;
    public TipoMovimentacaoEstoque Tipo { get; set; }
    public decimal Quantidade { get; set; }
    public DateOnly Data { get; set; }
    public Guid? FornecedorId { get; set; }
    public Fornecedor? Fornecedor { get; set; }
    public string? Observacao { get; set; }
    public Guid UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public DateTime RegistradoEm { get; set; }
}
