using Escola.Domain.Entities;
using Escola.Infrastructure.Tempo;

namespace Escola.Tests;

/// <summary>"Hoje" é sempre o dia no fuso da escola, nunca em UTC — no Brasil o dia virava às 21h antes dessa regra.</summary>
public class RelogioEscolaTests
{
    [Fact]
    public async Task IntervaloDoDiaEmSaoPauloComecaTresHorasDepoisDaMeiaNoiteUtc()
    {
        using var banco = new BancoDeTeste();
        using var db = banco.Contexto(BancoDeTeste.ClienteA);

        var (inicio, fim) = await new RelogioEscola(db).IntervaloUtcDoDiaAsync(new DateOnly(2026, 10, 20));

        Assert.Equal(new DateTime(2026, 10, 20, 3, 0, 0), inicio); // sem horário de verão desde 2019: UTC-3
        Assert.Equal(new DateTime(2026, 10, 21, 3, 0, 0), fim);
    }

    [Fact]
    public async Task NoiteDeSaoPauloAindaEhOMesmoDiaMesmoJaSendoOutroDiaEmUtc()
    {
        using var banco = new BancoDeTeste();
        using var db = banco.Contexto(BancoDeTeste.ClienteA);

        // 22h do dia 30 em São Paulo = 01h do dia 1º em UTC
        var dia = await new RelogioEscola(db).DataLocalAsync(new DateTime(2026, 11, 1, 1, 0, 0, DateTimeKind.Utc));

        Assert.Equal(new DateOnly(2026, 10, 31), dia);
    }

    [Fact]
    public async Task RespeitaOFusoConfiguradoNaEscola()
    {
        using var banco = new BancoDeTeste();
        using var db = banco.Contexto(BancoDeTeste.ClienteA);
        db.ConfiguracoesEscola.Add(new ConfiguracaoEscola { Id = Guid.NewGuid(), FusoHorario = "America/Manaus" });
        await db.SaveChangesAsync();

        var (inicio, _) = await new RelogioEscola(db).IntervaloUtcDoDiaAsync(new DateOnly(2026, 10, 20));

        Assert.Equal(new DateTime(2026, 10, 20, 4, 0, 0), inicio); // UTC-4
    }

    [Fact]
    public async Task FusoInvalidoCaiNoPadraoEmVezDeQuebrar()
    {
        using var banco = new BancoDeTeste();
        using var db = banco.Contexto(BancoDeTeste.ClienteA);
        db.ConfiguracoesEscola.Add(new ConfiguracaoEscola { Id = Guid.NewGuid(), FusoHorario = "Marte/Olympus" });
        await db.SaveChangesAsync();

        var (inicio, _) = await new RelogioEscola(db).IntervaloUtcDoDiaAsync(new DateOnly(2026, 10, 20));

        Assert.Equal(new DateTime(2026, 10, 20, 3, 0, 0), inicio);
    }

    [Theory]
    [InlineData("America/Sao_Paulo", true)]
    [InlineData("Marte/Olympus", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void FusoValidoReconheceSoIdsIana(string? id, bool esperado) =>
        Assert.Equal(esperado, RelogioEscola.FusoValido(id));
}
