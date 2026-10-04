using Escola.Domain.Enums;

namespace Escola.Domain.Entities;

/// <summary>Configuração financeira da escola — linha única, usada pra montar o código PIX das cobranças.</summary>
public class ConfiguracaoFinanceira
{
    public Guid Id { get; set; }
    public string? PixChave { get; set; }

    /// <summary>Nulo em instalações que cadastraram a chave antes do tipo existir (o frontend infere pelo formato).</summary>
    public TipoChavePix? PixTipoChave { get; set; }
    public string? PixNomeRecebedor { get; set; }
    public string? PixCidade { get; set; }

    /// <summary>Quantos dias de atraso numa cobrança em aberto até o aluno ser considerado bloqueado.
    /// Nulo = sem bloqueio por inadimplência.</summary>
    public int? DiasParaBloqueio { get; set; }
    public DateTime AtualizadoEm { get; set; }
}
