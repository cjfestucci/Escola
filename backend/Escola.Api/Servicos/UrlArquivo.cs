using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace Escola.Api.Servicos;

/// <summary>
/// Links assinados dos arquivos em <c>/uploads</c> (fotos dos atletas, da rotina e do diário, e a logo da escola). O banco guarda o
/// caminho puro (<c>/uploads/&lt;guid&gt;.jpg</c>); toda resposta da API que entrega um desses caminhos passa por <see cref="Assinar"/>,
/// que acrescenta <c>?exp=…&amp;sig=…</c>, e o servidor só entrega o arquivo com assinatura válida e dentro do prazo (middleware no
/// <c>Program.cs</c>). Assim só tem o link quem recebeu o dado por uma rota com permissão, e um link vazado (encaminhado, num print)
/// para de funcionar em até 2 dias. A validade é "virada do dia + 2 dias" (UTC): o link é o mesmo o dia todo, então o navegador
/// continua aproveitando o cache da imagem.
/// <para>Quem recebe um link assinado e o devolve num formulário (editar atleta, rotina, diário) passa por <see cref="Normalizar"/>
/// antes de gravar — no banco nunca vai assinatura.</para>
/// </summary>
public static class UrlArquivo
{
    public const string Prefixo = "/uploads/";

    private static byte[]? _chave;

    /// <summary>Chamado uma vez na subida, com um segredo do deploy (a chave do JWT): a chave das assinaturas é derivada dele.</summary>
    public static void Configurar(string segredo) =>
        _chave = HMACSHA256.HashData(Encoding.UTF8.GetBytes(segredo), "gestortatico:assinatura-de-arquivos"u8);

    public static string? Assinar(string? url) => Assinar(url, DateTime.UtcNow);

    public static string? Assinar(string? url, DateTime agoraUtc)
    {
        var caminho = Normalizar(url);
        if (caminho is null || !caminho.StartsWith(Prefixo, StringComparison.Ordinal)) return url;
        var exp = new DateTimeOffset(agoraUtc.Date.AddDays(2), TimeSpan.Zero).ToUnixTimeSeconds();
        return $"{caminho}?exp={exp}&sig={Assinatura(caminho, exp)}";
    }

    /// <summary>Tira a assinatura de um link de <c>/uploads</c> (o que vai pro banco). Outros valores voltam como estão.</summary>
    public static string? Normalizar(string? url)
    {
        if (string.IsNullOrEmpty(url) || !url.StartsWith(Prefixo, StringComparison.Ordinal)) return url;
        var interrogacao = url.IndexOf('?');
        return interrogacao < 0 ? url : url[..interrogacao];
    }

    public static bool Valida(string caminho, string? exp, string? sig, DateTime agoraUtc)
    {
        if (string.IsNullOrEmpty(sig) || !long.TryParse(exp, out var expira)) return false;
        if (DateTimeOffset.FromUnixTimeSeconds(expira).UtcDateTime < agoraUtc) return false;
        var esperada = Encoding.ASCII.GetBytes(Assinatura(caminho, expira));
        return CryptographicOperations.FixedTimeEquals(esperada, Encoding.ASCII.GetBytes(sig));
    }

    private static string Assinatura(string caminho, long exp)
    {
        var chave = _chave ?? throw new InvalidOperationException("UrlArquivo.Configurar não foi chamado na subida da API.");
        return Base64Url.EncodeToString(HMACSHA256.HashData(chave, Encoding.UTF8.GetBytes($"{caminho}|{exp}")));
    }
}
