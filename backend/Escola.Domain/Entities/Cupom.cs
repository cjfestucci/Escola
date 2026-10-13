namespace Escola.Domain.Entities;

/// <summary>Cupom de desconto da assinatura pelo site. Do produto, não de um cliente (<see cref="IEntidadePlataforma"/>): quem cria é o
/// Suporte. Nunca é apagado — desativa.</summary>
public class Cupom : IEntidadePlataforma
{
    public Guid Id { get; set; }
    /// <summary>Sempre em maiúsculas, sem espaços (ex.: LANCAMENTO20). Único.</summary>
    public string Codigo { get; set; } = string.Empty;
    /// <summary>1 a 100.</summary>
    public int PercentualDesconto { get; set; }
    /// <summary>Por quantas mensalidades pagas o desconto vale. Nulo = enquanto durar a assinatura.</summary>
    public int? Meses { get; set; }
    /// <summary>Último dia em que o cupom pode ser usado num cadastro. Nulo = sem prazo.</summary>
    public DateOnly? ValidoAte { get; set; }
    /// <summary>Quantos cadastros podem usar. Nulo = sem limite.</summary>
    public int? UsosMaximos { get; set; }
    public bool Ativo { get; set; } = true;
    public string? Observacao { get; set; }
    public DateTime CriadoEm { get; set; }
}
