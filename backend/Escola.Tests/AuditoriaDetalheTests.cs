using Escola.Infrastructure.Auditoria;

namespace Escola.Tests;

public class AuditoriaDetalheTests
{
    [Fact]
    public void SemMudancaNaoGeraTexto() =>
        Assert.Null(AuditoriaDetalhe.MontarAlteracoes(("Nome", "A", "A"), ("Ativo", true, true)));

    [Fact]
    public void SoOsCamposAlteradosEntramUmPorLinha()
    {
        var texto = AuditoriaDetalhe.MontarAlteracoes(("Nome", "A", "B"), ("Telefone", "1", "1"), ("Ativo", true, false));
        Assert.Equal("Nome alterado de \"A\" para \"B\"\nAtivo alterado de \"Sim\" para \"Não\"", texto);
    }

    [Fact]
    public void FormataValoresEmPortugues()
    {
        var texto = AuditoriaDetalhe.MontarAlteracoes(
            ("Vencimento", new DateOnly(2026, 10, 5), new DateOnly(2026, 11, 5)),
            ("Entrada", new TimeOnly(7, 0), new TimeOnly(7, 30)),
            ("Valor", 100m, 150.5m),
            ("Obs", null, "x"));

        Assert.Contains("\"05/10/2026\" para \"05/11/2026\"", texto);
        Assert.Contains("\"07:00\" para \"07:30\"", texto);
        Assert.Contains("\"100.00\" para \"150.50\"", texto);
        Assert.Contains("\"vazio\" para \"x\"", texto);
    }
}
