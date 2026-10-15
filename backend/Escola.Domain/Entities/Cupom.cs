namespace Escola.Domain.Entities;

/// <summary>Cupom de desconto da assinatura pelo site. Do produto, não de um cliente (<see cref="IEntidadePlataforma"/>): quem cria é o
/// Suporte. Nunca é apagado — desativa.</summary>
public class Cupom : IEntidadePlataforma
{
    public Guid Id { get; set; }
    /// <summary>Sempre em maiúsculas, sem espaços (ex.: LANCAMENTO20). Único.</summary>
    public string Codigo { get; set; } = string.Empty;
    /// <summary>0 a 99. Zero = cupom só de meses grátis (100% geraria fatura de R$ 0, que o Asaas recusa).</summary>
    public int PercentualDesconto { get; set; }
    /// <summary>Por quantas mensalidades pagas o desconto vale. Nulo = enquanto durar a assinatura.</summary>
    public int? Meses { get; set; }
    /// <summary>Meses grátis: o teste do clube passa a durar esses meses (em vez de <c>Assinatura:DiasTeste</c> dias) e a 1ª fatura só vence no
    /// fim deles. Nulo = teste normal.</summary>
    public int? MesesGratis { get; set; }
    /// <summary>Último dia em que o cupom pode ser usado num cadastro. Nulo = sem prazo.</summary>
    public DateOnly? ValidoAte { get; set; }
    /// <summary>Quantos cadastros podem usar. Nulo = sem limite.</summary>
    public int? UsosMaximos { get; set; }
    public bool Ativo { get; set; } = true;
    public string? Observacao { get; set; }
    public DateTime CriadoEm { get; set; }
}
