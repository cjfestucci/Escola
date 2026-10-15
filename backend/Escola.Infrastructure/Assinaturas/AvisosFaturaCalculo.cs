using Escola.Domain.Enums;

namespace Escola.Infrastructure.Assinaturas;

public enum TipoAvisoFatura
{
    /// <summary>A mensalidade vence em poucos dias (com o link de pagamento).</summary>
    Lembrete,
    /// <summary>Venceu e não foi paga: ainda funciona, mas suspende quando acabar a tolerância.</summary>
    Atraso,
    /// <summary>Pagamento confirmado (com o link da fatura, que serve de comprovante).</summary>
    Recibo
}

public sealed record AvisoFatura(TipoAvisoFatura Tipo, FaturaAssinatura Fatura);

/// <summary>Quais e-mails de fatura o produto manda ao clube. As notificações do Asaas ficam desligadas (custam R$ 0,99 por cobrança paga)
/// e o sistema avisa pelo próprio e-mail: um aviso de cada tipo por fatura, guardado pelo id da fatura. Regra pura, testável.</summary>
public static class AvisosFaturaCalculo
{
    /// <summary>Quantos dias antes do vencimento sai o lembrete.</summary>
    public const int DiasLembrete = 3;

    /// <summary>Recibo só de pagamento recente — senão, na primeira leitura, todas as faturas antigas gerariam recibo.</summary>
    public const int DiasRecibo = 5;

    /// <summary>O próximo aviso a mandar (um por vez; a tarefa periódica manda o seguinte na rodada seguinte), ou nulo.</summary>
    /// <param name="ultimoLembrete">Id da última fatura que já recebeu lembrete (idem para os outros dois).</param>
    public static AvisoFatura? Proximo(DateOnly hoje, SituacaoAssinatura situacao, IReadOnlyList<FaturaAssinatura> faturas,
        string? ultimoLembrete, string? ultimoAtraso, string? ultimoRecibo)
    {
        var paga = faturas.Where(f => f.Paga && f.PagaEm is { } d && d >= hoje.AddDays(-DiasRecibo))
            .OrderByDescending(f => f.PagaEm).FirstOrDefault();
        if (paga is not null && paga.Id != ultimoRecibo) return new AvisoFatura(TipoAvisoFatura.Recibo, paga);

        var aberta = SituacaoAssinaturaCalculo.EmAberto(faturas);
        if (aberta is null) return null;

        // Suspensa tem o próprio e-mail; aguardando o 1º pagamento (sem teste) ainda nem entrou no sistema.
        if (aberta.Vencimento < hoje)
            return situacao == SituacaoAssinatura.EmAtraso && aberta.Id != ultimoAtraso ? new AvisoFatura(TipoAvisoFatura.Atraso, aberta) : null;

        // No teste, a 1ª fatura já é avisada pelo lembrete do fim do teste.
        var dias = aberta.Vencimento.DayNumber - hoje.DayNumber;
        return situacao == SituacaoAssinatura.Ativa && dias <= DiasLembrete && aberta.Id != ultimoLembrete
            ? new AvisoFatura(TipoAvisoFatura.Lembrete, aberta)
            : null;
    }
}
