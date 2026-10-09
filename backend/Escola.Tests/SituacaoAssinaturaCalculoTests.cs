using Escola.Domain.Enums;
using Escola.Infrastructure.Assinaturas;

namespace Escola.Tests;

public class SituacaoAssinaturaCalculoTests
{
    private static readonly DateOnly Hoje = new(2026, 10, 20);
    private const int Tolerancia = 7;

    private static FaturaAssinatura Fatura(DateOnly vencimento, bool paga = false) =>
        new($"pay_{vencimento:yyyyMMdd}", vencimento, 219m, paga, paga ? vencimento : null, paga ? null : "https://pagar");

    private static SituacaoAssinatura Calcular(DateOnly? testeAte, DateOnly primeiroVencimento, params FaturaAssinatura[] faturas) =>
        SituacaoAssinaturaCalculo.Calcular(Hoje, testeAte, primeiroVencimento, Tolerancia, cancelada: false, faturas);

    [Fact]
    public void DentroDoTesteEhEmTeste() =>
        Assert.Equal(SituacaoAssinatura.EmTeste, Calcular(Hoje.AddDays(3), Hoje.AddDays(3), Fatura(Hoje.AddDays(3))));

    [Fact]
    public void NoDiaDoVencimentoAindaNaoEhAtraso() =>
        Assert.Equal(SituacaoAssinatura.EmTeste, Calcular(Hoje, Hoje, Fatura(Hoje)));

    [Fact]
    public void TesteAcabouSemPagarFicaEmAtrasoAteATolerancia()
    {
        Assert.Equal(SituacaoAssinatura.EmAtraso, Calcular(Hoje.AddDays(-1), Hoje.AddDays(-1), Fatura(Hoje.AddDays(-1))));
        Assert.Equal(SituacaoAssinatura.EmAtraso, Calcular(Hoje.AddDays(-7), Hoje.AddDays(-7), Fatura(Hoje.AddDays(-7))));
    }

    [Fact]
    public void PassouDaToleranciaSuspende() =>
        Assert.Equal(SituacaoAssinatura.Suspensa, Calcular(Hoje.AddDays(-8), Hoje.AddDays(-8), Fatura(Hoje.AddDays(-8))));

    [Fact]
    public void PagouEhAtiva() =>
        Assert.Equal(SituacaoAssinatura.Ativa, Calcular(Hoje.AddDays(-20), Hoje.AddDays(-20), Fatura(Hoje.AddDays(-20), paga: true), Fatura(Hoje.AddDays(10))));

    [Fact]
    public void ClienteAntigoComMensalidadeVencidaFicaEmAtrasoDepoisSuspende()
    {
        var primeira = Fatura(Hoje.AddDays(-40), paga: true);
        Assert.Equal(SituacaoAssinatura.EmAtraso, Calcular(null, Hoje.AddDays(-40), primeira, Fatura(Hoje.AddDays(-5))));
        Assert.Equal(SituacaoAssinatura.Suspensa, Calcular(null, Hoje.AddDays(-40), primeira, Fatura(Hoje.AddDays(-10))));
    }

    [Fact]
    public void SemTesteENuncaPagouAguardaPagamentoAteSuspender()
    {
        Assert.Equal(SituacaoAssinatura.AguardandoPagamento, Calcular(null, Hoje, Fatura(Hoje)));
        Assert.Equal(SituacaoAssinatura.AguardandoPagamento, Calcular(null, Hoje.AddDays(-3), Fatura(Hoje.AddDays(-3))));
        Assert.Equal(SituacaoAssinatura.Suspensa, Calcular(null, Hoje.AddDays(-8), Fatura(Hoje.AddDays(-8))));
    }

    [Fact]
    public void SemFaturasConhecidasUsaOPrimeiroVencimento()
    {
        Assert.Equal(SituacaoAssinatura.EmTeste, Calcular(Hoje.AddDays(2), Hoje.AddDays(2)));
        Assert.Equal(SituacaoAssinatura.Suspensa, Calcular(Hoje.AddDays(-9), Hoje.AddDays(-9)));
    }

    [Fact]
    public void CanceladaVenceTudo() =>
        Assert.Equal(SituacaoAssinatura.Cancelada,
            SituacaoAssinaturaCalculo.Calcular(Hoje, null, Hoje, Tolerancia, cancelada: true, [Fatura(Hoje.AddDays(-1), paga: true)]));

    [Theory]
    [InlineData(SituacaoAssinatura.AguardandoPagamento, true)]
    [InlineData(SituacaoAssinatura.EmTeste, false)]
    [InlineData(SituacaoAssinatura.Ativa, false)]
    [InlineData(SituacaoAssinatura.EmAtraso, false)]
    [InlineData(SituacaoAssinatura.Suspensa, true)]
    [InlineData(SituacaoAssinatura.Cancelada, true)]
    public void SoBloqueiaQuemNaoPagou(SituacaoAssinatura situacao, bool bloqueia) =>
        Assert.Equal(bloqueia, SituacaoAssinaturaCalculo.Bloqueia(situacao));

    [Fact]
    public void ValorEhFixoMaisPorAtleta() =>
        Assert.Equal(219m, new OpcoesAssinatura { PrecoFixo = 99m, PrecoPorAtleta = 3m }.CalcularValor(40));
}
