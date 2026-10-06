using System.Security.Cryptography;
using System.Text;

namespace Escola.Infrastructure.Auth;

/// <summary>Senha de uso único baseada em tempo (TOTP, RFC 6238: HMAC-SHA1, 6 dígitos, passo de 30 s) — o "código do app autenticador"
/// (Google Authenticator, Microsoft Authenticator, Authy, 1Password…) usado como segundo fator. Sem dependência externa.</summary>
public static class Totp
{
    private const int Digitos = 6;
    private const int PeriodoSegundos = 30;
    private const int BytesDoSegredo = 20; // 160 bits, o recomendado pelo RFC 4226

    /// <summary>Segredo novo, em base32 (é o que o app autenticador aceita digitado ou em QR).</summary>
    public static string GerarSegredo() => Base32.Codificar(RandomNumberGenerator.GetBytes(BytesDoSegredo));

    /// <summary>Base32 válido e com pelo menos 80 bits (16 caracteres) — abaixo disso o segredo é fraco demais pra proteger conta nenhuma.</summary>
    public static bool SegredoValido(string? segredo)
    {
        if (string.IsNullOrWhiteSpace(segredo) || segredo.Length < 16) return false;
        return Base32.TentarDecodificar(segredo, out var bytes) && bytes.Length >= 10;
    }

    public static long PassoAtual(DateTime agoraUtc) => new DateTimeOffset(agoraUtc, TimeSpan.Zero).ToUnixTimeSeconds() / PeriodoSegundos;

    public static string Codigo(string segredoBase32, long passo)
    {
        Base32.TentarDecodificar(segredoBase32, out var chave);
        var contador = BitConverter.GetBytes(passo);
        if (BitConverter.IsLittleEndian) Array.Reverse(contador);

        var hash = HMACSHA1.HashData(chave, contador);
        var deslocamento = hash[^1] & 0x0F;
        var binario = ((hash[deslocamento] & 0x7F) << 24) | (hash[deslocamento + 1] << 16) | (hash[deslocamento + 2] << 8) | hash[deslocamento + 3];
        return (binario % (int)Math.Pow(10, Digitos)).ToString().PadLeft(Digitos, '0');
    }

    /// <summary>Confere o código aceitando o passo atual e um de folga pra cada lado (relógios levemente fora de hora).
    /// <paramref name="ultimoPassoUsado"/> impede <b>reuso</b>: um código já aceito não vale de novo, mesmo dentro da janela
    /// (senão quem espiou o código por cima do ombro poderia entrar logo depois).</summary>
    public static bool TentarValidar(string segredoBase32, string? codigo, DateTime agoraUtc, long? ultimoPassoUsado, out long passoAceito)
    {
        passoAceito = 0;
        var limpo = codigo?.Replace(" ", "").Replace("-", "") ?? string.Empty;
        if (limpo.Length != Digitos || !limpo.All(char.IsAsciiDigit)) return false;

        var atual = PassoAtual(agoraUtc);
        var aceito = false;
        for (var passo = atual - 1; passo <= atual + 1; passo++)
        {
            // Não dá pra sair do laço no primeiro acerto: o tempo gasto não pode revelar em qual passo bateu.
            var confere = CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(Codigo(segredoBase32, passo)), Encoding.ASCII.GetBytes(limpo));
            if (confere && (ultimoPassoUsado is null || passo > ultimoPassoUsado) && !aceito)
            {
                aceito = true;
                passoAceito = passo;
            }
        }

        return aceito;
    }

    /// <summary>URI que o app autenticador entende (<c>otpauth://</c>) — vira QR ou é colada/digitada pelo segredo.</summary>
    public static string UriOtpAuth(string emissor, string conta, string segredoBase32) =>
        $"otpauth://totp/{Uri.EscapeDataString(emissor)}:{Uri.EscapeDataString(conta)}?secret={segredoBase32}&issuer={Uri.EscapeDataString(emissor)}&algorithm=SHA1&digits={Digitos}&period={PeriodoSegundos}";
}

/// <summary>Base32 (RFC 4648, sem preenchimento) — o alfabeto dos segredos de app autenticador.</summary>
internal static class Base32
{
    private const string Alfabeto = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public static string Codificar(byte[] dados)
    {
        var resultado = new StringBuilder((dados.Length * 8 + 4) / 5);
        int buffer = 0, bits = 0;
        foreach (var b in dados)
        {
            buffer = (buffer << 8) | b;
            bits += 8;
            while (bits >= 5)
            {
                resultado.Append(Alfabeto[(buffer >> (bits - 5)) & 0x1F]);
                bits -= 5;
            }
        }

        if (bits > 0) resultado.Append(Alfabeto[(buffer << (5 - bits)) & 0x1F]);
        return resultado.ToString();
    }

    public static bool TentarDecodificar(string texto, out byte[] dados)
    {
        var bytes = new List<byte>();
        int buffer = 0, bits = 0;
        foreach (var c in texto.Trim().TrimEnd('=').ToUpperInvariant())
        {
            var valor = Alfabeto.IndexOf(c);
            if (valor < 0)
            {
                dados = [];
                return false;
            }

            buffer = (buffer << 5) | valor;
            bits += 5;
            if (bits >= 8)
            {
                bytes.Add((byte)((buffer >> (bits - 8)) & 0xFF));
                bits -= 8;
            }
        }

        dados = [.. bytes];
        return true;
    }
}
