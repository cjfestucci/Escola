using System.Security.Cryptography;
using System.Text;

namespace Escola.Infrastructure.Auth;

/// <summary>Códigos de recuperação da verificação em duas etapas: pra quem perdeu o celular entrar mesmo assim. Cada um vale uma vez e só o
/// hash fica guardado. Formato "XXXX-XXXX" (letras e números sem os que confundem: 0/O, 1/I/L).</summary>
public static class CodigosRecuperacao
{
    public const int Quantidade = 8;
    private const string Alfabeto = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    public static IReadOnlyList<string> Gerar()
    {
        var codigos = new List<string>(Quantidade);
        for (var i = 0; i < Quantidade; i++)
        {
            var c = new char[8];
            for (var j = 0; j < c.Length; j++) c[j] = Alfabeto[RandomNumberGenerator.GetInt32(Alfabeto.Length)];
            codigos.Add($"{new string(c, 0, 4)}-{new string(c, 4, 4)}");
        }
        return codigos;
    }

    public static string Guardar(IEnumerable<string> codigos) => string.Join(',', codigos.Select(Hash));

    public static int Restantes(string? guardados) =>
        string.IsNullOrEmpty(guardados) ? 0 : guardados.Split(',', StringSplitOptions.RemoveEmptyEntries).Length;

    /// <summary>Se o código digitado é um dos guardados, devolve a lista sem ele (o código "gasta").</summary>
    public static bool TentarUsar(string? guardados, string? digitado, out string? restantes)
    {
        restantes = guardados;
        if (string.IsNullOrEmpty(guardados) || string.IsNullOrWhiteSpace(digitado)) return false;

        var normalizado = Normalizar(digitado);
        if (normalizado.Length != 8) return false;
        var alvo = Hash($"{normalizado[..4]}-{normalizado[4..]}");

        var lista = guardados.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        var indice = lista.FindIndex(h => CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(h), Encoding.ASCII.GetBytes(alvo)));
        if (indice < 0) return false;
        lista.RemoveAt(indice);
        restantes = lista.Count == 0 ? string.Empty : string.Join(',', lista);
        return true;
    }

    /// <summary>Parece um código de recuperação (8 letras/números, com ou sem traço), e não um código de 6 dígitos do app.</summary>
    public static bool PareceCodigoDeRecuperacao(string? digitado) => digitado is not null && Normalizar(digitado).Length == 8;

    private static string Normalizar(string texto) =>
        new(texto.ToUpperInvariant().Where(char.IsAsciiLetterOrDigit).ToArray());

    private static string Hash(string codigo) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(codigo))).ToLowerInvariant();
}
