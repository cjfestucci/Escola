namespace Escola.Infrastructure.Financeiro;

/// <summary>Multa e juros por atraso de uma cobrança — sempre <b>calculados na hora</b> (nunca gravados), como o
/// "atrasado". O que fica gravado é só o <c>ValorPago</c> no momento em que a cobrança é marcada como paga.</summary>
public static class EncargosCobranca
{
    /// <summary>Política da escola: multa única + juros ao mês (pro rata por dia, mês de 30 dias). Nulos/zero = não cobra.</summary>
    public sealed record Politica(decimal? MultaPercentual, decimal? JurosMensaisPercentual)
    {
        public static readonly Politica Nenhuma = new(null, null);
    }

    public readonly record struct Resultado(decimal Multa, decimal Juros, int DiasAtraso)
    {
        public decimal Total => Multa + Juros;
        public static readonly Resultado Zero = new(0m, 0m, 0);
    }

    /// <summary>Só há encargo em cobrança em aberto (não paga, não cancelada) com vencimento anterior a <paramref name="hoje"/>.
    /// No dia do vencimento ainda não é atraso.</summary>
    public static Resultado Calcular(decimal valor, DateOnly vencimento, DateOnly hoje, bool paga, bool cancelada, Politica politica)
    {
        if (paga || cancelada || vencimento >= hoje) return Resultado.Zero;

        var dias = hoje.DayNumber - vencimento.DayNumber;
        var multa = Math.Round(valor * (politica.MultaPercentual ?? 0m) / 100m, 2, MidpointRounding.AwayFromZero);
        var juros = Math.Round(valor * (politica.JurosMensaisPercentual ?? 0m) / 100m / 30m * dias, 2, MidpointRounding.AwayFromZero);
        return new Resultado(multa, juros, dias);
    }
}
