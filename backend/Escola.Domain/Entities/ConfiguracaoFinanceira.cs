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

    /// <summary>Dia do mês em que vencem as mensalidades geradas em lote (1–31; em mês mais curto vence no último dia).</summary>
    public int DiaVencimentoMensalidade { get; set; } = 10;

    /// <summary>Multa única sobre o valor, cobrada uma vez quando a cobrança passa do vencimento. Nulo = sem multa.</summary>
    public decimal? MultaAtrasoPercentual { get; set; }

    /// <summary>Juros por mês de atraso, calculados pro rata por dia (÷30), sobre o valor. Nulo = sem juros.</summary>
    public decimal? JurosMensaisPercentual { get; set; }

    // ----- Formas de pagamento oferecidas à família (desde 2026-10-06). Só aparece no portal/e-mail o que estiver ativo. -----

    /// <summary>Pix (código copia e cola + QR). Ligado por padrão: era a única forma antes desta opção existir.</summary>
    public bool PagamentoPixAtivo { get; set; } = true;

    /// <summary>Boleto. Ainda não há integração com banco/processadora: quando ativo, aparece pra família como "em breve".</summary>
    public bool PagamentoBoletoAtivo { get; set; }

    /// <summary>Pagamento na própria escola (dinheiro, cartão na secretaria...). Só informativo: a baixa continua manual.</summary>
    public bool PagamentoPresencialAtivo { get; set; }

    /// <summary>Texto mostrado à família junto do pagamento presencial (onde, horário, formas aceitas).</summary>
    public string? InstrucoesPagamentoPresencial { get; set; }
    public DateTime AtualizadoEm { get; set; }
}
