using Escola.Domain.Enums;

namespace Escola.Infrastructure.Assinaturas;

/// <summary>Uma fatura da assinatura (vinda do gateway).</summary>
public sealed record FaturaAssinatura(string Id, DateOnly Vencimento, decimal Valor, bool Paga, DateOnly? PagaEm, string? LinkPagamento);

/// <summary>Regra pura da situação da assinatura — sem banco, sem gateway, testável.</summary>
public static class SituacaoAssinaturaCalculo
{
    /// <summary>Situações em que a escola não usa o sistema (só o Admin entra, e só na tela da assinatura).</summary>
    public static bool Bloqueia(SituacaoAssinatura situacao) =>
        situacao is SituacaoAssinatura.AguardandoPagamento or SituacaoAssinatura.Suspensa or SituacaoAssinatura.Cancelada;

    /// <param name="faturas">Faturas conhecidas. Vazia (gateway fora do ar ou não configurado) = considera a 1ª fatura em aberto, vencendo
    /// em <paramref name="primeiroVencimento"/> — assim o teste acaba mesmo sem gateway.</param>
    /// <param name="toleranciaDias">Dias depois do vencimento em que ainda funciona (com aviso). Passou disso, suspende.</param>
    /// <param name="acessoAteCancelamento">Cancelamento pedido: até esse dia segue a regra normal (o período pago vale); depois, Cancelada.
    /// Nulo = não cancelada.</param>
    /// <param name="jaPagouAntes">Houve fatura paga numa assinatura anterior (reativada depois de cancelar).</param>
    public static SituacaoAssinatura Calcular(
        DateOnly hoje, DateOnly? testeAte, DateOnly primeiroVencimento, int toleranciaDias, DateOnly? acessoAteCancelamento,
        IReadOnlyCollection<FaturaAssinatura> faturas, bool jaPagouAntes = false)
    {
        if (acessoAteCancelamento is { } ate && hoje > ate) return SituacaoAssinatura.Cancelada;

        var lista = faturas.Count > 0 ? faturas : [new FaturaAssinatura("", primeiroVencimento, 0m, false, null, null)];
        var algumaPaga = jaPagouAntes || lista.Any(f => f.Paga);
        var semTeste = testeAte is null;

        // Fatura em aberto que já venceu (o dia do vencimento ainda não é atraso).
        var vencida = lista.Where(f => !f.Paga && f.Vencimento < hoje).OrderBy(f => f.Vencimento).FirstOrDefault();
        if (vencida is not null)
        {
            var diasAtraso = hoje.DayNumber - vencida.Vencimento.DayNumber;
            if (diasAtraso > Math.Max(0, toleranciaDias)) return SituacaoAssinatura.Suspensa;
            // Sem teste e nunca pagou: continua sem acesso enquanto não pagar (não há "atraso" de quem nunca começou a usar).
            return !algumaPaga && semTeste ? SituacaoAssinatura.AguardandoPagamento : SituacaoAssinatura.EmAtraso;
        }

        if (!algumaPaga) return semTeste ? SituacaoAssinatura.AguardandoPagamento : SituacaoAssinatura.EmTeste;
        return SituacaoAssinatura.Ativa;
    }

    /// <summary>Até quando vale o acesso de quem cancela agora: o período já pago (até o vencimento da fatura em aberto, ou o fim do teste).
    /// Com fatura vencida em aberto não há período pago a respeitar: termina hoje. Sem fatura em aberto, a última paga cobre um mês a partir
    /// do vencimento dela — o Asaas só gera a próxima perto da data (bug do teste de produção, 15/10/2026: quem pagou a 1ª fatura ainda
    /// no teste e cancelou perdia o mês pago).</summary>
    public static DateOnly AcessoAteAoCancelar(DateOnly hoje, DateOnly? testeAte, DateOnly primeiroVencimento, IReadOnlyCollection<FaturaAssinatura> faturas)
    {
        var aberta = EmAberto(faturas);
        var ultimaPaga = faturas.Where(f => f.Paga).OrderByDescending(f => f.Vencimento).FirstOrDefault();
        var fim = aberta?.Vencimento
            ?? ultimaPaga?.Vencimento.AddMonths(1)
            ?? testeAte ?? primeiroVencimento;
        return fim < hoje ? hoje : fim;
    }

    /// <summary>Dias antes do vencimento em que o valor da fatura em aberto não muda mais (o clube pode estar pagando).</summary>
    public const int DiasValorCongelado = 3;

    /// <summary>O valor da assinatura pode seguir os atletas ativos agora? Só depois da 1ª fatura paga (no teste vale o número informado), sem
    /// fatura vencida em aberto e longe o bastante do próximo vencimento.</summary>
    public static bool PodeAtualizarValor(DateOnly hoje, IReadOnlyCollection<FaturaAssinatura> faturas)
    {
        if (!faturas.Any(f => f.Paga)) return false;
        var aberta = EmAberto(faturas);
        return aberta is null || aberta.Vencimento.DayNumber - hoje.DayNumber > DiasValorCongelado;
    }

    /// <summary>Dias antes da exclusão dos dados em que sai o aviso por e-mail (e a exclusão só acontece 7 dias depois desse aviso).</summary>
    public const int DiasAvisoExclusao = 7;

    /// <summary>Quando os dados do clube podem ser apagados: <paramref name="diasRetencao"/> dias depois do fim do acesso (cancelada) ou da
    /// suspensão de um teste que <b>nunca pagou</b>. Nulo = nunca (clube em dia, em teste, em atraso, ou suspenso que já pagou alguma vez —
    /// esse é cliente inadimplente, não teste abandonado).</summary>
    public static DateOnly? DataExclusao(SituacaoAssinatura situacao, DateOnly? acessoAte, DateTime? suspensaEm, bool jaPagou, int diasRetencao) =>
        situacao switch
        {
            SituacaoAssinatura.Cancelada when acessoAte is { } ate => ate.AddDays(diasRetencao),
            SituacaoAssinatura.Suspensa when !jaPagou && suspensaEm is { } em => DateOnly.FromDateTime(em).AddDays(diasRetencao),
            _ => null
        };

    /// <summary>A fatura que o cliente deve pagar agora: a em aberto mais antiga.</summary>
    public static FaturaAssinatura? EmAberto(IEnumerable<FaturaAssinatura> faturas) =>
        faturas.Where(f => !f.Paga).OrderBy(f => f.Vencimento).FirstOrDefault();
}
