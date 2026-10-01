namespace Escola.Domain.Entities;

/// <summary>Cobrança (ex.: mensalidade) de um aluno — controle manual, sem gateway de pagamento.
/// "Atrasada" não é um estado guardado: é Pendente com Vencimento no passado, calculado na hora de exibir.</summary>
public class Cobranca
{
    public Guid Id { get; set; }

    public Guid AlunoId { get; set; }
    public Aluno Aluno { get; set; } = null!;

    public string Descricao { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public DateOnly Vencimento { get; set; }

    public bool Paga { get; set; }
    public DateOnly? PagoEm { get; set; }

    public bool Cancelada { get; set; }
    public DateOnly? CanceladaEm { get; set; }

    public DateTime RegistradoEm { get; set; }
}
