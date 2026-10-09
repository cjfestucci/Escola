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
    public static SituacaoAssinatura Calcular(
        DateOnly hoje, DateOnly? testeAte, DateOnly primeiroVencimento, int toleranciaDias, bool cancelada,
        IReadOnlyCollection<FaturaAssinatura> faturas)
    {
        if (cancelada) return SituacaoAssinatura.Cancelada;

        var lista = faturas.Count > 0 ? faturas : [new FaturaAssinatura("", primeiroVencimento, 0m, false, null, null)];
        var algumaPaga = lista.Any(f => f.Paga);
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

    /// <summary>A fatura que o cliente deve pagar agora: a em aberto mais antiga.</summary>
    public static FaturaAssinatura? EmAberto(IEnumerable<FaturaAssinatura> faturas) =>
        faturas.Where(f => !f.Paga).OrderBy(f => f.Vencimento).FirstOrDefault();
}
