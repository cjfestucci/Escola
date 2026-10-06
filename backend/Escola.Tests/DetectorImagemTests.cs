using System.Text;
using Escola.Infrastructure.Storage;

namespace Escola.Tests;

/// <summary>Garante que o tipo vem do conteúdo — foi a correção de um XSS armazenado no upload de fotos.</summary>
public class DetectorImagemTests
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0];
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0, 0, 0, 0, 0, 0, 0, 0];
    private static readonly byte[] Webp = Encoding.ASCII.GetBytes("RIFF\0\0\0\0WEBP");

    [Fact]
    public void ReconheceOsFormatosPermitidos()
    {
        Assert.Equal(".png", DetectorImagem.Detectar(Png)?.Extensao);
        Assert.Equal(".jpg", DetectorImagem.Detectar(Jpeg)?.Extensao);
        Assert.Equal("image/webp", DetectorImagem.Detectar(Webp)?.ContentType);
    }

    [Theory]
    [InlineData("<html><script>alert(1)</script></html>")]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\"><script/></svg>")]
    [InlineData("%PDF-1.7 not an image")]
    [InlineData("")]
    public void ConteudoQueNaoEhImagemEhRecusadoQualquerQueSejaOContentTypeDeclarado(string conteudo) =>
        Assert.Null(DetectorImagem.Detectar(Encoding.UTF8.GetBytes(conteudo)));

    [Fact]
    public void RiffQueNaoEhWebpNaoPassa() =>
        Assert.Null(DetectorImagem.Detectar(Encoding.ASCII.GetBytes("RIFF\0\0\0\0WAVE")));

    [Fact]
    public async Task DetectarAsyncDevolveOStreamNaPosicaoOriginal()
    {
        using var stream = new MemoryStream(Png.Concat(new byte[100]).ToArray());
        var tipo = await DetectorImagem.DetectarAsync(stream);
        Assert.Equal("image/png", tipo?.ContentType);
        Assert.Equal(0, stream.Position);
    }
}
