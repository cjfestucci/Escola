namespace Escola.Infrastructure.Storage;

/// <summary>Descobre o tipo real de uma imagem pelo <b>conteúdo</b> (assinatura dos primeiros bytes), nunca pelo <c>Content-Type</c> nem pela
/// extensão que o cliente manda — os dois são livres pra quem envia. Sem isso, um <c>.html</c> declarado como imagem seria servido
/// como página do próprio site (XSS armazenado). SVG fica de fora de propósito: é XML e pode carregar script.</summary>
public static class DetectorImagem
{
    public readonly record struct Tipo(string ContentType, string Extensao);

    /// <summary>Lê só o começo do stream e o devolve à posição original (precisa ser <c>CanSeek</c>). Nulo se não for JPEG/PNG/WEBP/GIF.</summary>
    public static async Task<Tipo?> DetectarAsync(Stream stream, CancellationToken ct = default)
    {
        var inicio = stream.Position;
        var cabecalho = new byte[12];
        var lidos = 0;
        while (lidos < cabecalho.Length)
        {
            var n = await stream.ReadAsync(cabecalho.AsMemory(lidos), ct);
            if (n == 0) break;
            lidos += n;
        }

        stream.Position = inicio;
        return Detectar(cabecalho.AsSpan(0, lidos));
    }

    public static Tipo? Detectar(ReadOnlySpan<byte> b)
    {
        if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return new Tipo("image/jpeg", ".jpg");
        if (b.Length >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47 && b[4] == 0x0D && b[5] == 0x0A && b[6] == 0x1A && b[7] == 0x0A)
            return new Tipo("image/png", ".png");
        if (b.Length >= 12 && b[0] == (byte)'R' && b[1] == (byte)'I' && b[2] == (byte)'F' && b[3] == (byte)'F'
            && b[8] == (byte)'W' && b[9] == (byte)'E' && b[10] == (byte)'B' && b[11] == (byte)'P')
            return new Tipo("image/webp", ".webp");
        if (b.Length >= 6 && b[0] == (byte)'G' && b[1] == (byte)'I' && b[2] == (byte)'F' && b[3] == (byte)'8' && (b[4] == (byte)'7' || b[4] == (byte)'9') && b[5] == (byte)'a')
            return new Tipo("image/gif", ".gif");
        return null;
    }
}
