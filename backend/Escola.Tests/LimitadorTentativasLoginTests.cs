using Escola.Infrastructure.Auth;

namespace Escola.Tests;

public class LimitadorTentativasLoginTests
{
    [Fact]
    public void BloqueiaSoDepoisDoLimiteDeFalhas()
    {
        var l = new LimitadorTentativasLogin();
        for (var i = 0; i < 7; i++) l.RegistrarFalha("a@x.com");
        Assert.False(l.EstaBloqueado("a@x.com", 8, out _));

        l.RegistrarFalha("a@x.com");
        Assert.True(l.EstaBloqueado("a@x.com", 8, out var restante));
        Assert.InRange(restante.TotalMinutes, 14, 15);
    }

    [Fact]
    public void LimiteMenorBloqueiaMaisCedo()
    {
        var l = new LimitadorTentativasLogin();
        for (var i = 0; i < 5; i++) l.RegistrarFalha("suporte");
        Assert.True(l.EstaBloqueado("suporte", 5, out _));
        Assert.False(l.EstaBloqueado("suporte", 8, out _));
    }

    [Fact]
    public void ContasSaoIndependentes()
    {
        var l = new LimitadorTentativasLogin();
        for (var i = 0; i < 8; i++) l.RegistrarFalha("a");
        Assert.True(l.EstaBloqueado("a", 8, out _));
        Assert.False(l.EstaBloqueado("b", 8, out _));
    }

    [Fact]
    public void LimparLiberaAConta()
    {
        var l = new LimitadorTentativasLogin();
        for (var i = 0; i < 8; i++) l.RegistrarFalha("a");
        l.Limpar("a");
        Assert.False(l.EstaBloqueado("a", 8, out _));
    }

    [Fact]
    public void GuardaOUltimoPassoTotpPorConta()
    {
        var l = new LimitadorTentativasLogin();
        Assert.Null(l.UltimoPasso("a"));
        l.RegistrarPasso("a", 42);
        Assert.Equal(42, l.UltimoPasso("a"));
        Assert.Null(l.UltimoPasso("b"));
    }
}
