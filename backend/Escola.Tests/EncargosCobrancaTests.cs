using Escola.Infrastructure.Financeiro;

namespace Escola.Tests;

public class EncargosCobrancaTests
{
    private static readonly DateOnly Hoje = new(2026, 10, 20);
    private static readonly EncargosCobranca.Politica Politica = new(2m, 3m); // multa 2%, juros 3% ao mês

    [Fact]
    public void NoDiaDoVencimentoAindaNaoEhAtraso()
    {
        var r = EncargosCobranca.Calcular(100m, Hoje, Hoje, paga: false, cancelada: false, Politica);
        Assert.Equal(EncargosCobranca.Resultado.Zero, r);
    }

    [Fact]
    public void AntesDoVencimentoNaoCobraNada()
    {
        var r = EncargosCobranca.Calcular(100m, Hoje.AddDays(5), Hoje, false, false, Politica);
        Assert.Equal(0m, r.Total);
    }

    [Fact]
    public void UmDiaDeAtrasoCobraMultaInteiraEUmTrintaAvosDosJuros()
    {
        var r = EncargosCobranca.Calcular(300m, Hoje.AddDays(-1), Hoje, false, false, Politica);
        Assert.Equal(6.00m, r.Multa);  // 2% de 300
        Assert.Equal(0.30m, r.Juros);  // 300 * 3% / 30 * 1
        Assert.Equal(1, r.DiasAtraso);
        Assert.Equal(6.30m, r.Total);
    }

    [Fact]
    public void JurosCrescemPorDiaEMultaNaoSeRepete()
    {
        var r = EncargosCobranca.Calcular(300m, Hoje.AddDays(-30), Hoje, false, false, Politica);
        Assert.Equal(6.00m, r.Multa);
        Assert.Equal(9.00m, r.Juros); // um mês cheio = 3%
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void CobrancaPagaOuCanceladaNaoTemEncargo(bool paga, bool cancelada)
    {
        var r = EncargosCobranca.Calcular(100m, Hoje.AddDays(-40), Hoje, paga, cancelada, Politica);
        Assert.Equal(EncargosCobranca.Resultado.Zero, r);
    }

    [Fact]
    public void SemPoliticaOsDiasDeAtrasoAindaSaoCalculadosMasOValorEhZero()
    {
        var r = EncargosCobranca.Calcular(100m, Hoje.AddDays(-10), Hoje, false, false, EncargosCobranca.Politica.Nenhuma);
        Assert.Equal(0m, r.Total);
        Assert.Equal(10, r.DiasAtraso);
    }

    [Fact]
    public void ArredondaParaDuasCasasParaCima()
    {
        // 10,05 * 2% = 0,201 → 0,20 ; juros 10,05 * 3% / 30 * 1 = 0,01005 → 0,01
        var r = EncargosCobranca.Calcular(10.05m, Hoje.AddDays(-1), Hoje, false, false, Politica);
        Assert.Equal(0.20m, r.Multa);
        Assert.Equal(0.01m, r.Juros);
    }
}
