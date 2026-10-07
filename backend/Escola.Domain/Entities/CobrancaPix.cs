using Escola.Domain.Enums;

namespace Escola.Domain.Entities;

/// <summary>Cobrança Pix <b>dinâmica</b> criada no banco (API Pix) para uma <see cref="Cobranca"/>. Diferente do "copia e cola"
/// estático (gerado localmente, sem dono), esta tem um identificador (<see cref="TxId"/>) que o banco devolve quando o
/// pagamento acontece — é o que permite dar baixa sozinho. Uma cobrança pode ter várias ao longo do tempo (o valor muda com
/// multa/juros), mas só uma costuma estar ativa.</summary>
public class CobrancaPix
{
    public Guid Id { get; set; }

    public Guid CobrancaId { get; set; }
    public Cobranca Cobranca { get; set; } = null!;

    /// <summary>Identificador da cobrança no provedor: o txid no BB (26–35 letras/números, regra do Banco Central) ou o id do pagamento
    /// no Asaas (ex.: "pay_…").</summary>
    public string TxId { get; set; } = string.Empty;

    /// <summary>Valor com que a cobrança foi criada no banco (fixo: o Pix só aceita exatamente esse valor).</summary>
    public decimal Valor { get; set; }

    public string PixCopiaECola { get; set; } = string.Empty;

    /// <summary>Quem criou esta cobrança (e quem a confere). As anteriores ao Asaas são todas do BB.</summary>
    public ProvedorPagamento Provedor { get; set; } = ProvedorPagamento.BancoDoBrasil;

    public StatusCobrancaPix Status { get; set; }

    public DateTime CriadoEm { get; set; }
    public DateTime ExpiraEm { get; set; }
    public DateTime? UltimaVerificacaoEm { get; set; }

    public DateTime? ConcluidoEm { get; set; }
    public string? EndToEndId { get; set; }
    public decimal? ValorRecebido { get; set; }
}
