using Escola.Domain.Enums;
using Escola.Infrastructure.Documentos;
using Escola.Infrastructure.Pagamentos;

namespace Escola.Tests;

/// <summary>CPF e CNPJ, inclusive o CNPJ alfanumérico da Receita (letras nas 12 primeiras posições, dígitos verificadores numéricos).</summary>
public class DocumentoBrTests
{
    [Fact]
    public void CnpjAlfanumericoDoExemploOficialEhValido()
    {
        // Exemplo publicado pela Receita: 12.ABC.345/01DE-35.
        Assert.Equal("12ABC34501DE35", DocumentoBr.Normalizar("12.abc.345/01de-35"));
        Assert.True(DocumentoBr.CnpjValido("12ABC34501DE35"));
        Assert.True(DocumentoBr.CpfOuCnpjValido("12ABC34501DE35"));
    }

    [Fact]
    public void CnpjAlfanumericoComDigitoErradoOuLetraNoDigitoEhRecusado()
    {
        Assert.False(DocumentoBr.CnpjValido("12ABC34501DE36"));
        Assert.False(DocumentoBr.CnpjValido("12ABC34501DE3A")); // dígito verificador é sempre número
        Assert.False(DocumentoBr.CnpjValido("12ABC34501DE3"));  // 13 caracteres
    }

    [Fact]
    public void CnpjSoDeNumerosContinuaValendoECpfSegueSoNumeros()
    {
        Assert.True(DocumentoBr.CnpjValido("11444777000161"));
        Assert.False(DocumentoBr.CnpjValido("11111111111111"));
        Assert.True(DocumentoBr.CpfValido("52998224725"));
        Assert.False(DocumentoBr.CpfValido("5299822472A"));
        Assert.True(DocumentoBr.CpfOuCnpjValido(DocumentoBr.Normalizar("529.982.247-25")));
    }

    [Fact]
    public void ChavePixCnpjAceitaLetras()
    {
        Assert.Equal("12ABC34501DE35", ChavePix.Normalizar(TipoChavePix.Cnpj, "12.ABC.345/01DE-35", out var erro));
        Assert.Null(erro);
        Assert.Null(ChavePix.Normalizar(TipoChavePix.Cnpj, "12.ABC.345/01DE-36", out _));
    }
}
