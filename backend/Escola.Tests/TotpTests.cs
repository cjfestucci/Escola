using Escola.Infrastructure.Auth;

namespace Escola.Tests;

/// <summary>Vetores do Apêndice B do RFC 6238 (segredo ASCII "12345678901234567890", SHA-1) — o gerador do app
/// autenticador do usuário tem que produzir exatamente estes códigos.</summary>
public class TotpTests
{
    private const string Segredo = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";

    [Theory]
    [InlineData(59L, "287082")]
    [InlineData(1111111109L, "081804")]
    [InlineData(1111111111L, "050471")]
    [InlineData(1234567890L, "005924")]
    [InlineData(2000000000L, "279037")]
    public void BateComOsVetoresDoRfc6238(long segundos, string esperado)
    {
        Assert.Equal(esperado, Totp.Codigo(Segredo, segundos / 30));
    }

    private static DateTime Instante(long segundos) => DateTimeOffset.FromUnixTimeSeconds(segundos).UtcDateTime;

    [Fact]
    public void AceitaCodigoDoPassoAtualEDosVizinhos()
    {
        var agora = 1234567890L;
        var passo = agora / 30;
        foreach (var delta in new[] { -1, 0, 1 })
            Assert.True(Totp.TentarValidar(Segredo, Totp.Codigo(Segredo, passo + delta), Instante(agora), null, out _));
    }

    [Fact]
    public void RecusaCodigoForaDaJanela()
    {
        var agora = 1234567890L;
        var passo = agora / 30;
        Assert.False(Totp.TentarValidar(Segredo, Totp.Codigo(Segredo, passo + 2), Instante(agora), null, out _));
        Assert.False(Totp.TentarValidar(Segredo, Totp.Codigo(Segredo, passo - 2), Instante(agora), null, out _));
    }

    [Fact]
    public void CodigoJaUsadoNaoValeDeNovo()
    {
        var agora = 1234567890L;
        var codigo = Totp.Codigo(Segredo, agora / 30);

        Assert.True(Totp.TentarValidar(Segredo, codigo, Instante(agora), null, out var passo));
        Assert.False(Totp.TentarValidar(Segredo, codigo, Instante(agora), passo, out _));
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("abcdef")]
    [InlineData(null)]
    public void RecusaFormatoInvalido(string? codigo)
    {
        Assert.False(Totp.TentarValidar(Segredo, codigo, Instante(1234567890), null, out _));
    }

    [Fact]
    public void AceitaCodigoComEspaco()
    {
        var agora = 1234567890L;
        var c = Totp.Codigo(Segredo, agora / 30);
        Assert.True(Totp.TentarValidar(Segredo, $"{c[..3]} {c[3..]}", Instante(agora), null, out _));
    }

    [Fact]
    public void SegredoGeradoEhValidoESegredosFracosNao()
    {
        Assert.True(Totp.SegredoValido(Totp.GerarSegredo()));
        Assert.False(Totp.SegredoValido(null));
        Assert.False(Totp.SegredoValido("ABC"));
        Assert.False(Totp.SegredoValido("1111111111111111")); // '1' não existe em base32
    }
}
