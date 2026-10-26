namespace Escola.Infrastructure.Storage;

/// <summary>Descobre o tipo de um arquivo (PDF ou imagem JPEG/PNG/WEBP) pela <b>assinatura do conteúdo</b>, nunca pelo
/// <c>Content-Type</c> ou pela extensão que o cliente declara — um <c>.html</c> disfarçado de PDF é recusado.</summary>
public static class DetectorDocumento
{
    public static async Task<(string ContentType, string Extensao)?> DetectarAsync(Stream stream, CancellationToken ct = default)
    {
        var cabecalho = new byte[12];
        var lidos = await stream.ReadAtLeastAsync(cabecalho, cabecalho.Length, throwOnEndOfStream: false, ct);
        if (lidos < 4) return null;

        if (cabecalho[0] == 0x25 && cabecalho[1] == 0x50 && cabecalho[2] == 0x44 && cabecalho[3] == 0x46)
            return ("application/pdf", ".pdf");
        if (cabecalho[0] == 0xFF && cabecalho[1] == 0xD8 && cabecalho[2] == 0xFF)
            return ("image/jpeg", ".jpg");
        if (cabecalho[0] == 0x89 && cabecalho[1] == 0x50 && cabecalho[2] == 0x4E && cabecalho[3] == 0x47)
            return ("image/png", ".png");
        if (lidos >= 12 && cabecalho[0] == 0x52 && cabecalho[1] == 0x49 && cabecalho[2] == 0x46 && cabecalho[3] == 0x46
            && cabecalho[8] == 0x57 && cabecalho[9] == 0x45 && cabecalho[10] == 0x42 && cabecalho[11] == 0x50)
            return ("image/webp", ".webp");

        return null;
    }
}
