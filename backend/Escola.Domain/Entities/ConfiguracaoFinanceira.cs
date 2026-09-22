namespace Escola.Domain.Entities;

/// <summary>Configuração financeira da escola — linha única, usada pra montar o código PIX das cobranças.</summary>
public class ConfiguracaoFinanceira
{
    public Guid Id { get; set; }
    public string? PixChave { get; set; }
    public string? PixNomeRecebedor { get; set; }
    public string? PixCidade { get; set; }
    public DateTime AtualizadoEm { get; set; }
}
