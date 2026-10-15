using Escola.Domain.Enums;
using Escola.Infrastructure.Assinaturas;

namespace Escola.Tests;

public class AvisosFaturaCalculoTests
{
    private static readonly DateOnly Hoje = new(2026, 10, 14);

    private static FaturaAssinatura Aberta(string id, int diasAteVencer) => new(id, Hoje.AddDays(diasAteVencer), 60m, false, null, $"https://asaas/{id}");
    private static FaturaAssinatura Paga(string id, int diasDesdePagamento) =>
        new(id, Hoje.AddDays(-diasDesdePagamento), 60m, true, Hoje.AddDays(-diasDesdePagamento), $"https://asaas/{id}");

    [Theory]
    [InlineData(3, true)]
    [InlineData(0, true)]
    [InlineData(4, false)]
    public void LembreteSaiNosUltimosDiasAntesDoVencimento(int dias, bool sai)
    {
        var aviso = AvisosFaturaCalculo.Proximo(Hoje, SituacaoAssinatura.Ativa, [Aberta("pay_2", dias)], null, null, null);
        Assert.Equal(sai, aviso?.Tipo == TipoAvisoFatura.Lembrete);
    }

    [Fact]
    public void CadaAvisoSaiUmaVezPorFatura()
    {
        Assert.Null(AvisosFaturaCalculo.Proximo(Hoje, SituacaoAssinatura.Ativa, [Aberta("pay_2", 1)], "pay_2", null, null));
        Assert.Null(AvisosFaturaCalculo.Proximo(Hoje, SituacaoAssinatura.EmAtraso, [Aberta("pay_2", -2)], null, "pay_2", null));
        Assert.Null(AvisosFaturaCalculo.Proximo(Hoje, SituacaoAssinatura.Ativa, [Paga("pay_1", 1)], null, null, "pay_1"));
        // Fatura nova = aviso novo.
        Assert.Equal(TipoAvisoFatura.Lembrete, AvisosFaturaCalculo.Proximo(Hoje, SituacaoAssinatura.Ativa, [Aberta("pay_3", 1)], "pay_2", null, null)?.Tipo);
    }

    [Fact]
    public void NoTesteOLembreteDaPrimeiraFaturaFicaComOAvisoDoFimDoTeste()
    {
        Assert.Null(AvisosFaturaCalculo.Proximo(Hoje, SituacaoAssinatura.EmTeste, [Aberta("pay_1", 1)], null, null, null));
    }

    [Theory]
    [InlineData(SituacaoAssinatura.EmAtraso, true)]
    [InlineData(SituacaoAssinatura.Suspensa, false)]            // tem o próprio e-mail
    [InlineData(SituacaoAssinatura.AguardandoPagamento, false)] // ainda nem entrou no sistema
    public void AtrasoSoAvisaQuemAindaUsaOSistema(SituacaoAssinatura situacao, bool sai)
    {
        var aviso = AvisosFaturaCalculo.Proximo(Hoje, situacao, [Aberta("pay_1", -2)], null, null, null);
        Assert.Equal(sai, aviso?.Tipo == TipoAvisoFatura.Atraso);
    }

    [Fact]
    public void ReciboSoDePagamentoRecenteEAntesDoLembrete()
    {
        // Pagou ontem e a próxima já vence em 2 dias: primeiro o recibo; o lembrete sai na rodada seguinte.
        List<FaturaAssinatura> faturas = [Paga("pay_1", 1), Aberta("pay_2", 2)];
        Assert.Equal(TipoAvisoFatura.Recibo, AvisosFaturaCalculo.Proximo(Hoje, SituacaoAssinatura.Ativa, faturas, null, null, null)?.Tipo);
        Assert.Equal(TipoAvisoFatura.Lembrete, AvisosFaturaCalculo.Proximo(Hoje, SituacaoAssinatura.Ativa, faturas, null, null, "pay_1")?.Tipo);

        // Pagamento antigo (ex.: primeira leitura depois de ligar os avisos) não gera recibo.
        Assert.Null(AvisosFaturaCalculo.Proximo(Hoje, SituacaoAssinatura.Ativa, [Paga("pay_1", AvisosFaturaCalculo.DiasRecibo + 1)], null, null, null));
    }
}
