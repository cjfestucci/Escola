using Escola.Domain.Enums;
using Escola.Infrastructure.Frequencia;

namespace Escola.Tests;

public class FrequenciaCalculoTests
{
    private static DateOnly D(int dia) => new(2026, 10, dia);

    [Fact]
    public void SemRegistrosOPercentualEhNuloEnaoZero()
    {
        var r = FrequenciaCalculo.Resumir([]);
        Assert.Null(r.Percentual);
    }

    [Fact]
    public void FaltaJustificadaNaoEntraNaConta()
    {
        var r = FrequenciaCalculo.Resumir([StatusPresenca.Presente, StatusPresenca.Presente, StatusPresenca.Falta, StatusPresenca.Justificada, StatusPresenca.Justificada]);
        Assert.Equal(67m, r.Percentual); // 2 de 3
        Assert.Equal(2, r.Justificadas);
    }

    [Fact]
    public void SoJustificadasTambemNaoTemPercentual()
    {
        Assert.Null(FrequenciaCalculo.Resumir([StatusPresenca.Justificada]).Percentual);
    }

    [Fact]
    public void FaltasSeguidasContamDoDiaMaisRecenteParaTras()
    {
        var registros = new (DateOnly, StatusPresenca)[]
        {
            (D(1), StatusPresenca.Falta),      // antiga: não conta (tem presença no meio)
            (D(2), StatusPresenca.Presente),
            (D(3), StatusPresenca.Falta),
            (D(4), StatusPresenca.Falta),
            (D(5), StatusPresenca.Falta),
        };
        Assert.Equal(3, FrequenciaCalculo.FaltasSeguidas(registros));
    }

    [Fact]
    public void OrdemDeEntradaNaoImporta()
    {
        var registros = new (DateOnly, StatusPresenca)[]
        {
            (D(5), StatusPresenca.Falta), (D(2), StatusPresenca.Presente), (D(4), StatusPresenca.Falta),
        };
        Assert.Equal(2, FrequenciaCalculo.FaltasSeguidas(registros));
    }

    [Fact]
    public void JustificadaNaoSomaNemInterrompeASequencia()
    {
        var registros = new (DateOnly, StatusPresenca)[]
        {
            (D(1), StatusPresenca.Falta), (D(2), StatusPresenca.Justificada), (D(3), StatusPresenca.Falta),
        };
        Assert.Equal(2, FrequenciaCalculo.FaltasSeguidas(registros));
    }

    [Fact]
    public void PresencaRecenteZeraAContagem()
    {
        var registros = new (DateOnly, StatusPresenca)[]
        {
            (D(1), StatusPresenca.Falta), (D(2), StatusPresenca.Falta), (D(3), StatusPresenca.Presente),
        };
        Assert.Equal(0, FrequenciaCalculo.FaltasSeguidas(registros));
    }
}
