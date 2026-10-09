using Escola.Infrastructure.Tema;

namespace Escola.Tests;

public class CorPrincipalTests
{
    [Theory]
    [InlineData("#0E2A3A")] // padrão do produto (azul-marinho da marca)
    [InlineData("#6C5DD3")] // padrão antigo (roxo)
    [InlineData("#2563EB")]
    [InlineData("#334155")]
    public void CoresDasAmostrasPassamNoContraste(string cor) =>
        Assert.True(CorPrincipal.ContrasteComBranco(cor) >= CorPrincipal.ContrasteMinimo);

    [Theory]
    [InlineData("#FFEB3B")] // amarelo
    [InlineData("#CCCCCC")] // cinza claro
    [InlineData("#FFFFFF")]
    public void CoresClarasDemaisFalham(string cor) =>
        Assert.True(CorPrincipal.ContrasteComBranco(cor) < CorPrincipal.ContrasteMinimo);

    [Theory]
    [InlineData("#6c5dd3", true)]
    [InlineData("6C5DD3", false)]
    [InlineData("#6C5DD", false)]
    [InlineData("#GGGGGG", false)]
    [InlineData("red", false)]
    [InlineData(null, false)]
    public void FormatoExigeHashESeisDigitosHex(string? cor, bool esperado) =>
        Assert.Equal(esperado, CorPrincipal.FormatoValido(cor));

    [Fact]
    public void NormalizarDeixaMaiusculoEVazioViraNulo()
    {
        Assert.Equal("#6C5DD3", CorPrincipal.Normalizar("  #6c5dd3 "));
        Assert.Null(CorPrincipal.Normalizar("   "));
        Assert.Null(CorPrincipal.Normalizar(null));
    }
}
