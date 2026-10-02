namespace Escola.Domain.Entities;

/// <summary>Item controlado em estoque. O saldo não é um campo — é sempre calculado somando as
/// movimentações (entradas − saídas), pra nunca ficar dessincronizado do histórico.</summary>
public class Produto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Codigo { get; set; }
    public string UnidadeMedida { get; set; } = "un";
    public decimal EstoqueMinimo { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime RegistradoEm { get; set; }

    public ICollection<MovimentacaoEstoque> Movimentacoes { get; set; } = new List<MovimentacaoEstoque>();
}
