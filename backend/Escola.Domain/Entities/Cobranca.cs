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

    /// <summary>Primeiro dia do mês a que a mensalidade se refere (só nas geradas em lote) — é o que impede gerar duas vezes o mesmo mês.</summary>
    public DateOnly? Competencia { get; set; }

    /// <summary>Quanto foi efetivamente recebido (valor + multa/juros do dia em que foi marcada como paga). Nulo se não paga.</summary>
    public decimal? ValorPago { get; set; }

    public bool Paga { get; set; }
    public DateOnly? PagoEm { get; set; }

    public bool Cancelada { get; set; }
    public DateOnly? CanceladaEm { get; set; }

    public DateTime RegistradoEm { get; set; }
}
