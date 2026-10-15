using Escola.Domain.Enums;

namespace Escola.Domain.Entities;

/// <summary>Assinatura do cliente com a plataforma (o plano que o clube paga pra usar o sistema) — criada pelo cadastro do site. Uma por
/// cliente. Clientes provisionados à mão (antes do cadastro pelo site) não têm assinatura e não sofrem bloqueio nenhum.
/// <para>A cobrança é uma assinatura mensal na <b>conta raiz</b> do Asaas (não na subconta da escola). A <see cref="Situacao"/> é
/// derivada das faturas de lá + datas e guardada só pra consulta rápida a cada requisição.</para></summary>
public class Assinatura
{
    public Guid Id { get; set; }
    public SituacaoAssinatura Situacao { get; set; }

    /// <summary>Último dia do teste grátis; nulo = assinou sem teste (paga antes de entrar).</summary>
    public DateOnly? TesteAte { get; set; }

    /// <summary>Vencimento da 1ª fatura (fim do teste, ou o dia do cadastro sem teste). Usado quando o gateway não responde.</summary>
    public DateOnly PrimeiroVencimento { get; set; }

    public decimal ValorMensal { get; set; }
    public int AtletasInformados { get; set; }

    /// <summary>Atletas ativos na última atualização do valor (fase 2: depois da 1ª fatura paga, o valor segue os atletas ativos). Nulo =
    /// ainda vale o número informado no cadastro.</summary>
    public int? AtletasCobrados { get; set; }

    // Cupom usado no cadastro: copiado aqui (desativar ou mudar o cupom depois não mexe em quem já assinou).
    public string? CupomCodigo { get; set; }
    public int? CupomPercentual { get; set; }
    /// <summary>Por quantas mensalidades pagas o desconto vale. Nulo (com cupom) = sempre.</summary>
    public int? CupomMeses { get; set; }

    /// <summary>Já houve alguma fatura paga (em qualquer assinatura do clube). Uma assinatura reativada é nova no gateway e não traz as
    /// faturas antigas — sem isto, quem pagou o mês e desistiu do cancelamento ficaria como "aguardando o 1º pagamento".</summary>
    public bool JaPagou { get; set; }

    // Dados de quem assinou (pra criar o cliente no gateway e pra contato).
    public string CpfCnpj { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string Celular { get; set; } = string.Empty;
    public string EmailCobranca { get; set; } = string.Empty;

    /// <summary>Sandbox ou Producao (do Asaas) — os ids abaixo só valem nesse ambiente.</summary>
    public string? Ambiente { get; set; }
    public string? IdClienteGateway { get; set; }
    public string? IdAssinaturaGateway { get; set; }

    /// <summary>Página de pagamento da fatura em aberto mais antiga (do gateway). Nula quando não há nada a pagar.</summary>
    public string? LinkPagamento { get; set; }
    public DateOnly? VencimentoEmAberto { get; set; }

    /// <summary>Quando o e-mail de boas-vindas (convite do Admin) saiu. Sem teste, só sai depois do 1º pagamento.</summary>
    public DateTime? ConviteEnviadoEm { get; set; }

    public DateTime CriadaEm { get; set; }
    public DateTime? SituacaoAtualizadaEm { get; set; }
    /// <summary>Quando entrou em <see cref="SituacaoAssinatura.Suspensa"/> pela última vez (base da limpeza de testes nunca pagos).</summary>
    public DateTime? SuspensaEm { get; set; }
    public DateTime? CanceladaEm { get; set; }
    /// <summary>Com o cancelamento pedido, o acesso continua até este dia (fim do período pago ou do teste); depois vira Cancelada.</summary>
    public DateOnly? AcessoAte { get; set; }

    // Avisos por e-mail do produto, marcados pra não repetir. As notificações do Asaas ficam desligadas (são pagas): lembrete de
    // vencimento, atraso e recibo também saem daqui, um de cada por fatura (guardado o id da última fatura avisada).
    public DateTime? LembreteTesteEnviadoEm { get; set; }
    public string? LembreteFaturaId { get; set; }
    public string? AvisoAtrasoFaturaId { get; set; }
    public string? ReciboFaturaId { get; set; }
    public DateTime? AvisoSuspensaoEnviadoEm { get; set; }
    /// <summary>Aviso de que os dados vão ser apagados (cancelada ou teste nunca pago). A exclusão só acontece 7 dias depois dele.</summary>
    public DateTime? AvisoExclusaoEnviadoEm { get; set; }

    // Prova do aceite dos Termos de Uso no cadastro.
    public string TermosVersao { get; set; } = string.Empty;
    public DateTime TermosAceitosEm { get; set; }
    public string? TermosIp { get; set; }
    public string? TermosNavegador { get; set; }
}
