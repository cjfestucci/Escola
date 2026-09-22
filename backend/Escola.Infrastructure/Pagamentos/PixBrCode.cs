using System.Globalization;
using System.Text;

namespace Escola.Infrastructure.Pagamentos;

/// <summary>
/// Monta o payload "Pix Copia e Cola" (BR Code) seguindo o padrão EMV do Banco Central —
/// não depende de nenhuma API externa, o código já sai válido pra qualquer app de banco ler.
/// </summary>
public static class PixBrCode
{
    public static string Gerar(string chave, string nomeRecebedor, string cidade, decimal valor, string? txid = null)
    {
        var nome = Sanitizar(nomeRecebedor, 25);
        var cidadeSanitizada = Sanitizar(cidade, 15);
        var txidSanitizado = string.IsNullOrWhiteSpace(txid) ? "***" : Sanitizar(txid, 25);

        var infoContaRecebedor = Campo("00", "br.gov.bcb.pix") + Campo("01", chave);
        var dadosAdicionais = Campo("05", txidSanitizado);

        var payload = new StringBuilder();
        payload.Append(Campo("00", "01"));
        payload.Append(Campo("26", infoContaRecebedor));
        payload.Append(Campo("52", "0000"));
        payload.Append(Campo("53", "986"));
        payload.Append(Campo("54", valor.ToString("F2", CultureInfo.InvariantCulture)));
        payload.Append(Campo("58", "BR"));
        payload.Append(Campo("59", nome));
        payload.Append(Campo("60", cidadeSanitizada));
        payload.Append(Campo("62", dadosAdicionais));
        payload.Append("6304");

        return payload + Crc16(payload.ToString());
    }

    private static string Campo(string id, string valor) => $"{id}{valor.Length:D2}{valor}";

    private static string Sanitizar(string valor, int tamanhoMaximo)
    {
        var semAcento = RemoverAcentos(valor).ToUpperInvariant();
        var limpo = new string(semAcento.Where(c => char.IsLetterOrDigit(c) || c == ' ').ToArray()).Trim();
        return limpo.Length > tamanhoMaximo ? limpo[..tamanhoMaximo] : limpo;
    }

    private static string RemoverAcentos(string texto)
    {
        var normalizado = texto.Normalize(NormalizationForm.FormD);
        return new string(normalizado.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
    }

    private static string Crc16(string payload)
    {
        ushort crc = 0xFFFF;
        const ushort polinomio = 0x1021;

        foreach (var b in Encoding.ASCII.GetBytes(payload))
        {
            crc ^= (ushort)(b << 8);
            for (var i = 0; i < 8; i++)
                crc = (crc & 0x8000) != 0 ? (ushort)((crc << 1) ^ polinomio) : (ushort)(crc << 1);
        }

        return crc.ToString("X4");
    }
}
