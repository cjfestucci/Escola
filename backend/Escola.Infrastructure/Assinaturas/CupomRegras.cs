using Escola.Domain.Entities;

namespace Escola.Infrastructure.Assinaturas;

/// <summary>Regras puras do cupom de desconto (testáveis, sem banco).</summary>
public static class CupomRegras
{
    public static string Normalizar(string? codigo) =>
        new((codigo ?? string.Empty).Trim().ToUpperInvariant().Where(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_').ToArray());

    /// <returns>Nulo se o cupom pode ser usado num cadastro hoje; senão, o motivo (em pt-BR, pra mostrar ao visitante).</returns>
    public static string? MotivoInvalido(Cupom? cupom, DateOnly hoje, int usos)
    {
        if (cupom is null || !cupom.Ativo) return "Cupom inválido.";
        if (cupom.ValidoAte is { } ate && hoje > ate) return "Este cupom expirou.";
        if (cupom.UsosMaximos is { } maximo && usos >= maximo) return "Este cupom já foi usado o máximo de vezes.";
        return null;
    }

    /// <summary>O desconto ainda vale? Conta as mensalidades já pagas: com 3 meses, vale na 1ª, 2ª e 3ª fatura.</summary>
    public static bool DescontoVigente(int? percentual, int? meses, int mensalidadesPagas) =>
        percentual is > 0 && (meses is null || mensalidadesPagas < meses);

    public static decimal AplicarDesconto(decimal valor, int? percentual) =>
        percentual is > 0 ? decimal.Round(valor * (100 - Math.Min(100, percentual.Value)) / 100m, 2) : valor;

    public static string Descricao(int percentual, int? meses, int? mesesGratis = null)
    {
        var gratis = mesesGratis is > 0 ? (mesesGratis == 1 ? "1 mês grátis" : $"{mesesGratis} meses grátis") : null;
        if (percentual <= 0) return gratis ?? "sem desconto";
        var desconto = meses is null ? $"{percentual}% de desconto em todas as mensalidades"
            : meses == 1 ? $"{percentual}% de desconto na 1ª mensalidade"
            : $"{percentual}% de desconto nas {meses} primeiras mensalidades";
        return gratis is null ? desconto : $"{gratis} e depois {desconto}";
    }

    /// <summary>Até quando vai o teste de quem assina hoje: os meses grátis do cupom, se houver; senão os dias de teste do plano (nulo = sem
    /// teste, paga antes de usar).</summary>
    public static DateOnly? FimDoTeste(DateOnly hoje, int diasTeste, int? mesesGratis) =>
        mesesGratis is > 0 ? hoje.AddMonths(mesesGratis.Value)
        : diasTeste > 0 ? hoje.AddDays(diasTeste)
        : null;
}
