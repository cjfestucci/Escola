namespace Escola.Domain.Entities;

/// <summary>Receita a receber que não é mensalidade de aluno/atleta (essa já tem a "Cobranca" própria,
/// com Pix e vínculo ao Portal dos Pais) — ex.: patrocínio, aluguel de quadra, inscrição de evento.</summary>
public class ContaReceber
{
    public Guid Id { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string? Origem { get; set; }
    public decimal Valor { get; set; }
    public DateOnly Vencimento { get; set; }
    public bool Recebida { get; set; }
    public DateOnly? RecebidoEm { get; set; }
    public bool Cancelada { get; set; }
    public DateOnly? CanceladaEm { get; set; }
    public DateTime RegistradoEm { get; set; }
}
