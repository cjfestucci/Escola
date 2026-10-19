using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Escola.Api.Servicos;
using Escola.Domain.Enums;

namespace Escola.Tests;

public class UrlArquivoTests
{
    private static readonly DateTime Agora = new(2026, 10, 19, 15, 0, 0, DateTimeKind.Utc);

    public UrlArquivoTests() => UrlArquivo.Configurar(ApiFactory.JwtChave);

    [Fact]
    public void AssinaSoOsCaminhosDeUploadsEOLinkValeAteDoisDias()
    {
        var assinado = UrlArquivo.Assinar("/uploads/foto.jpg", Agora)!;
        Assert.StartsWith("/uploads/foto.jpg?exp=", assinado);
        Assert.Equal(assinado, UrlArquivo.Assinar("/uploads/foto.jpg", Agora.AddHours(8))); // mesmo link o dia todo (cache do navegador)

        var consulta = System.Web.HttpUtility.ParseQueryString(assinado[(assinado.IndexOf('?') + 1)..]);
        Assert.True(UrlArquivo.Valida("/uploads/foto.jpg", consulta["exp"], consulta["sig"], Agora.AddDays(1)));
        Assert.False(UrlArquivo.Valida("/uploads/foto.jpg", consulta["exp"], consulta["sig"], Agora.AddDays(2))); // venceu
        Assert.False(UrlArquivo.Valida("/uploads/outra.jpg", consulta["exp"], consulta["sig"], Agora)); // assinatura de outro arquivo
        Assert.False(UrlArquivo.Valida("/uploads/foto.jpg", (long.Parse(consulta["exp"]!) + 86400).ToString(), consulta["sig"], Agora)); // prazo esticado
        Assert.False(UrlArquivo.Valida("/uploads/foto.jpg", null, null, Agora));

        Assert.Null(UrlArquivo.Assinar(null));
        Assert.Equal("https://exemplo.test/x.png", UrlArquivo.Assinar("https://exemplo.test/x.png"));
    }

    [Fact]
    public void NormalizarTiraAAssinaturaAntesDeGravar()
    {
        Assert.Equal("/uploads/foto.jpg", UrlArquivo.Normalizar(UrlArquivo.Assinar("/uploads/foto.jpg", Agora)));
        Assert.Equal("/uploads/foto.jpg", UrlArquivo.Normalizar("/uploads/foto.jpg"));
        Assert.Null(UrlArquivo.Normalizar(null));
    }
}

/// <summary>As fotos em /uploads só abrem com o link assinado que a API entrega.</summary>
public class FotosAssinadasHttpTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0x0D, 0x49, 0x48, 0x44, 0x52];

    [Fact]
    public async Task FotoSoAbreComOLinkAssinado()
    {
        var equipe = await api.ClienteLogadoAsync(PapelUsuario.Admin);
        var arquivo = new MultipartFormDataContent { { new ByteArrayContent(Png) { Headers = { ContentType = new MediaTypeHeaderValue("image/png") } }, "arquivo", "foto.png" } };
        var resposta = await equipe.PostAsync("/api/uploads", arquivo);
        resposta.EnsureSuccessStatusCode();
        var url = (await resposta.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("url").GetString()!;
        Assert.Contains("?exp=", url);

        var semLogin = api.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await semLogin.GetAsync(url)).StatusCode); // o link assinado basta (é o que o <img> usa)

        var caminho = UrlArquivo.Normalizar(url)!;
        Assert.Equal(HttpStatusCode.NotFound, (await semLogin.GetAsync(caminho)).StatusCode); // sem assinatura
        Assert.Equal(HttpStatusCode.NotFound, (await semLogin.GetAsync(url[..^2] + "xx")).StatusCode); // assinatura adulterada
        var vencido = UrlArquivo.Assinar(caminho, DateTime.UtcNow.AddDays(-3))!;
        Assert.Equal(HttpStatusCode.NotFound, (await semLogin.GetAsync(vencido)).StatusCode); // prazo vencido
    }
}
