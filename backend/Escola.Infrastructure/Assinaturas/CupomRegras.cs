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

    public static string Descricao(int percentual, int? meses) =>
        meses is null ? $"{percentual}% de desconto em todas as mensalidades"
        : meses == 1 ? $"{percentual}% de desconto na 1ª mensalidade"
        : $"{percentual}% de desconto nas {meses} primeiras mensalidades";
}
