using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Escola.Api.Servicos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Escola.Tests;

/// <summary>Captcha (Turnstile) no cadastro pelo site: com as chaves configuradas, só cadastra com o token que a Cloudflare aprova.</summary>
public class CaptchaCadastroHttpTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Imita o siteverify da Cloudflare: aprova só o token "token-bom"; "cloudflare-fora" simula a Cloudflare fora do ar.</summary>
    private sealed class TurnstileFalso : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Assert.Equal(VerificadorCaptcha.Endereco, request.RequestUri!.ToString());
            var form = await request.Content!.ReadAsStringAsync(ct);
            if (form.Contains("response=cloudflare-fora")) throw new HttpRequestException("fora do ar");
            Assert.Contains("secret=segredo-de-teste", form);
            var ok = form.Contains("response=token-bom");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { success = ok }) };
        }
    }

    private HttpClient ApiComCaptcha() => api.WithWebHostBuilder(b =>
    {
        b.UseSetting("Turnstile:SiteKey", "chave-publica-de-teste");
        b.UseSetting("Turnstile:SecretKey", "segredo-de-teste");
        b.ConfigureTestServices(s => s.AddHttpClient<VerificadorCaptcha>().ConfigurePrimaryHttpMessageHandler(() => new TurnstileFalso()));
    }).CreateClient();

    private static object Cadastro(string? token) => new
    {
        nomeClube = "Clube do Captcha", cpfCnpj = "11222333000181", cidade = "Campinas", atletas = 15,
        nomeAdmin = "Dono", email = $"captcha.{Guid.NewGuid():N}@clube.test", celular = "(19) 99999-8888", aceiteTermos = true,
        tokenCaptcha = token
    };

    [Fact]
    public async Task ComCaptchaLigadoSoCadastraComTokenAprovado()
    {
        var http = ApiComCaptcha();

        var plano = await http.GetFromJsonAsync<JsonElement>("/api/assinaturas/plano", Json);
        Assert.Equal("chave-publica-de-teste", plano.GetProperty("chaveCaptcha").GetString());

        Assert.Equal(HttpStatusCode.BadRequest, (await http.PostAsJsonAsync("/api/assinaturas", Cadastro(null))).StatusCode);
        var recusado = await http.PostAsJsonAsync("/api/assinaturas", Cadastro("token-ruim"));
        Assert.Equal(HttpStatusCode.BadRequest, recusado.StatusCode);
        Assert.Contains("robô", await recusado.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, (await http.PostAsJsonAsync("/api/assinaturas", Cadastro("token-bom"))).StatusCode);
        // Cloudflare fora do ar não barra o clube.
        Assert.Equal(HttpStatusCode.OK, (await http.PostAsJsonAsync("/api/assinaturas", Cadastro("cloudflare-fora"))).StatusCode);
    }

    [Fact]
    public async Task SemChavesConfiguradasOCadastroSegueSemCaptcha()
    {
        var plano = await api.CreateClient().GetFromJsonAsync<JsonElement>("/api/assinaturas/plano", Json);
        Assert.Equal(JsonValueKind.Null, plano.GetProperty("chaveCaptcha").ValueKind);
    }
}
