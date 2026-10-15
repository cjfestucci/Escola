using Escola.Infrastructure.Email;

namespace Escola.Tests;

public class ModeloEmailTests
{
    [Fact]
    public void LinkSozinhoNumParagrafoViraBotaoEOsOutrosLinksGanhamACorDaMarca()
    {
        var html = ModeloEmail.Html("Assunto", """
            <p>Olá, Maria!</p>
            <p><a href="https://app.teste/redefinir?token=abc&amp;convite=1">Criar minha senha</a></p>
            <p>Dúvidas? <a href="https://app.teste/ajuda">veja a ajuda</a>.</p>
            """, "https://app.teste/");

        Assert.Contains("background:#0E2A3A;border-radius:8px;", html); // o botão
        Assert.Contains("""href="https://app.teste/redefinir?token=abc&amp;convite=1">Criar minha senha</a>""", html);
        Assert.Contains("""<a style="color:#0E2A3A;font-weight:700;" href="https://app.teste/ajuda">""", html);
        Assert.Contains("https://app.teste/icons/icon-96x96.png", html);
        Assert.DoesNotContain("<p>", html); // todo parágrafo ganhou o espaçamento do layout
    }

    [Fact]
    public void SemEnderecoDoAppOCabecalhoSaiSemImagem()
    {
        Assert.DoesNotContain("<img", ModeloEmail.Html("Assunto", "<p>Oi</p>", null));
    }

    [Fact]
    public void TextoPuroTrazOLinkPorExtensoESemTags()
    {
        var texto = ModeloEmail.Texto("""
            <p>Olá, <strong>Maria</strong>!</p>
            <p><a href="https://app.teste/x?a=1&amp;b=2">Criar minha senha</a></p>
            """);

        Assert.StartsWith("Olá, Maria!", texto);
        Assert.Contains("Criar minha senha: https://app.teste/x?a=1&b=2", texto);
        Assert.DoesNotContain("<", texto);
    }
}
