using System.Globalization;
using Escola.Infrastructure.Pagamentos;

namespace Escola.Tests;

public class PixBrCodeTests
{
    /// <summary>CRC16/CCITT-FALSE (poly 0x1021, init 0xFFFF) escrito de forma independente da implementação.</summary>
    private static string Crc(string texto)
    {
        ushort crc = 0xFFFF;
        foreach (var b in System.Text.Encoding.ASCII.GetBytes(texto))
        {
            crc ^= (ushort)(b << 8);
            for (var i = 0; i < 8; i++) crc = (crc & 0x8000) != 0 ? (ushort)((crc << 1) ^ 0x1021) : (ushort)(crc << 1);
        }
        return crc.ToString("X4", CultureInfo.InvariantCulture);
    }

    [Fact]
    public void CrcDoFinalConfereComUmaImplementacaoIndependente()
    {
        var codigo = PixBrCode.Gerar("52998224725", "Escola Craque do Amanhã", "São Paulo", 150.5m, "COB123");
        Assert.Equal(Crc(codigo[..^4]), codigo[^4..]);
    }

    [Fact]
    public void ContemOsCamposObrigatoriosDoPix()
    {
        var codigo = PixBrCode.Gerar("52998224725", "Escola Craque do Amanhã", "São Paulo", 1234.5m);
        Assert.StartsWith("000201", codigo);
        Assert.Contains("br.gov.bcb.pix0111" + "52998224725", codigo);
        Assert.Contains("5303986", codigo);                 // moeda BRL
        Assert.Contains("54071234.50", codigo);             // valor com ponto, 2 casas
        Assert.Contains("5802BR", codigo);
        Assert.Contains("ESCOLA CRAQUE DO AMANHA", codigo); // sem acento, maiúsculo
        Assert.Contains("SAO PAULO", codigo);
        Assert.Contains("6304", codigo);
    }

    [Fact]
    public void NomeELimitadoA25ECidadeA15Caracteres()
    {
        var codigo = PixBrCode.Gerar("52998224725", new string('A', 60), new string('B', 60), 10m);
        Assert.Contains("5925" + new string('A', 25), codigo);
        Assert.Contains("6015" + new string('B', 15), codigo);
    }
}
