namespace Escola.Domain.Entities;

/// <summary>Despesa a pagar a um fornecedor — mesmo padrão de "Cobranca" (Contas a Receber/Mensalidades),
/// só que do lado de fora: a escola devendo a alguém, em vez de alguém devendo à escola.</summary>
public class ContaPagar
{
    public Guid Id { get; set; }
    public Guid FornecedorId { get; set; }
    public Fornecedor Fornecedor { get; set; } = null!;
    public string Descricao { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public DateOnly Vencimento { get; set; }
    public bool Paga { get; set; }
    public DateOnly? PagoEm { get; set; }
    public bool Cancelada { get; set; }
    public DateOnly? CanceladaEm { get; set; }
    public DateTime RegistradoEm { get; set; }
}
