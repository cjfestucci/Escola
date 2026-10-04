using System.Text.RegularExpressions;

namespace Escola.Infrastructure.Tema;

/// <summary>Validação da cor principal do tema. O app usa essa cor como fundo de botões e da barra lateral
/// com texto branco por cima, então uma cor clara demais deixaria o texto ilegível — por isso há um contraste mínimo.</summary>
public static partial class CorPrincipal
{
    /// <summary>Contraste mínimo contra branco (WCAG AA para texto normal).</summary>
    public const double ContrasteMinimo = 4.5;

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
    private static partial Regex FormatoHex();

    public static bool FormatoValido(string? cor) => cor is not null && FormatoHex().IsMatch(cor);

    /// <summary>Normaliza pra "#RRGGBB" maiúsculo; vazio/nulo vira nulo (= cor padrão).</summary>
    public static string? Normalizar(string? cor)
    {
        var texto = cor?.Trim();
        return string.IsNullOrEmpty(texto) ? null : texto.ToUpperInvariant();
    }

    public static double ContrasteComBranco(string cor)
    {
        double Canal(int inicio)
        {
            var c = Convert.ToInt32(cor.Substring(inicio, 2), 16) / 255.0;
            return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }

        var luminancia = 0.2126 * Canal(1) + 0.7152 * Canal(3) + 0.0722 * Canal(5);
        return 1.05 / (luminancia + 0.05);
    }
}
