using Escola.Infrastructure.Auth;

namespace Escola.Tests;

public class SenhaHasherTests
{
    [Fact]
    public void SenhaCertaVerificaESenhaErradaNao()
    {
        var hash = SenhaHasher.Hash("Senha-Forte-2026");
        Assert.True(SenhaHasher.Verificar("Senha-Forte-2026", hash));
        Assert.False(SenhaHasher.Verificar("senha-forte-2026", hash));
    }

    [Fact]
    public void MesmaSenhaGeraHashesDiferentesPorCausaDoSalt()
    {
        Assert.NotEqual(SenhaHasher.Hash("abc12345"), SenhaHasher.Hash("abc12345"));
    }

    [Fact]
    public void ConvitePendenteNuncaAceitaNenhumaSenha()
    {
        Assert.False(SenhaHasher.Verificar("", SenhaHasher.ConvitePendente));
        Assert.False(SenhaHasher.Verificar(SenhaHasher.ConvitePendente, SenhaHasher.ConvitePendente));
        Assert.False(SenhaHasher.Verificar("qualquer coisa", SenhaHasher.ConvitePendente));
    }
}
