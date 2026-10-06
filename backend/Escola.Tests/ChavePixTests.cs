using Escola.Domain.Enums;
using Escola.Infrastructure.Pagamentos;

namespace Escola.Tests;

public class ChavePixTests
{
    private static string? N(TipoChavePix tipo, string entrada) => ChavePix.Normalizar(tipo, entrada, out _);

    [Fact]
    public void CpfValidoGuardaSoNumeros() => Assert.Equal("52998224725", N(TipoChavePix.Cpf, "529.982.247-25"));

    [Theory]
    [InlineData("529.982.247-26")] // dígito verificador errado
    [InlineData("111.111.111-11")] // repetidos
    [InlineData("123")]
    public void CpfInvalidoEhRecusadoComMensagem(string cpf)
    {
        Assert.Null(ChavePix.Normalizar(TipoChavePix.Cpf, cpf, out var erro));
        Assert.False(string.IsNullOrEmpty(erro));
    }

    [Fact]
    public void CnpjValidoGuardaSoNumeros() => Assert.Equal("11444777000161", N(TipoChavePix.Cnpj, "11.444.777/0001-61"));

    [Fact]
    public void CnpjComDigitoErradoEhRecusado() => Assert.Null(N(TipoChavePix.Cnpj, "11.444.777/0001-62"));

    [Theory]
    [InlineData("11 99999-8888", "+5511999998888")]      // sem +55: acrescenta
    [InlineData("+55 11 99999-8888", "+5511999998888")]  // com +55: mantém
    [InlineData("(11) 3333-4444", "+551133334444")]      // fixo, 10 dígitos
    [InlineData("55 99999-8888", "+5555999998888")]      // DDD 55 sem código do país não se confunde
    public void TelefoneVaiParaOFormatoDoBrCode(string entrada, string esperado) =>
        Assert.Equal(esperado, N(TipoChavePix.Telefone, entrada));

    [Theory]
    [InlineData("999")]
    [InlineData("+1 415 555 2671")]
    [InlineData("10 99999-8888")] // DDD < 11
    public void TelefoneInvalidoEhRecusado(string entrada) => Assert.Null(N(TipoChavePix.Telefone, entrada));

    [Fact]
    public void EmailVaiParaMinusculas() => Assert.Equal("financeiro@escola.com.br", N(TipoChavePix.Email, "  Financeiro@Escola.com.br "));

    [Theory]
    [InlineData("sem-arroba")]
    [InlineData("a@@b.com")]
    [InlineData("a@semponto")]
    [InlineData("a b@c.com")]
    public void EmailInvalidoEhRecusado(string entrada) => Assert.Null(N(TipoChavePix.Email, entrada));

    [Fact]
    public void ChaveAleatoriaPrecisaSerUuidCompleto()
    {
        Assert.Equal("123e4567-e89b-12d3-a456-426614174000", N(TipoChavePix.Aleatoria, "123E4567-E89B-12D3-A456-426614174000"));
        Assert.Null(N(TipoChavePix.Aleatoria, "123e4567e89b12d3a456426614174000")); // sem hífens: o app do banco recusa
        Assert.Null(N(TipoChavePix.Aleatoria, "nao-e-uuid"));
    }
}
