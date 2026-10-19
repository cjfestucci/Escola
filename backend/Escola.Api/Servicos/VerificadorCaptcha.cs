using System.Text.Json;

namespace Escola.Api.Servicos;

/// <summary>
/// Captcha do cadastro pelo site (Cloudflare Turnstile). Ligado só com <c>Turnstile:SiteKey</c> + <c>Turnstile:SecretKey</c> na
/// configuração do deploy (variáveis <c>TURNSTILE_SITE_KEY</c>/<c>TURNSTILE_SECRET_KEY</c> no compose); sem elas (dev, testes) o
/// cadastro segue sem captcha. O token que a tela manda é conferido direto na Cloudflare (<c>siteverify</c>) e vale uma vez só.
/// <para><b>Cloudflare fora do ar não barra ninguém</b> (registra no log e deixa passar): o freio por IP continua valendo, e perder um
/// clube de verdade por uma falha do captcha custa mais do que um cadastro falso.</para>
/// </summary>
public sealed class VerificadorCaptcha(HttpClient http, IConfiguration config, ILogger<VerificadorCaptcha> logger)
{
    public const string Endereco = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

    private string? Segredo => config["Turnstile:SecretKey"];

    /// <summary>Chave pública que a tela usa pra mostrar o widget; nula = captcha desligado.</summary>
    public string? ChavePublica => Ativo ? config["Turnstile:SiteKey"] : null;

    public bool Ativo => !string.IsNullOrWhiteSpace(Segredo) && !string.IsNullOrWhiteSpace(config["Turnstile:SiteKey"]);

    public async Task<bool> ValidarAsync(string? token, string? ip, CancellationToken ct)
    {
        if (!Ativo) return true;
        if (string.IsNullOrWhiteSpace(token) || token.Length > 2048) return false;

        var campos = new Dictionary<string, string> { ["secret"] = Segredo!, ["response"] = token };
        if (!string.IsNullOrEmpty(ip)) campos["remoteip"] = ip;
        try
        {
            using var resposta = await http.PostAsync(Endereco, new FormUrlEncodedContent(campos), ct);
            if (!resposta.IsSuccessStatusCode)
            {
                logger.LogWarning("Turnstile respondeu {Status}; cadastro liberado sem captcha.", (int)resposta.StatusCode);
                return true;
            }
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync(ct));
            var ok = json.RootElement.TryGetProperty("success", out var sucesso) && sucesso.GetBoolean();
            if (!ok && json.RootElement.TryGetProperty("error-codes", out var erros))
                logger.LogInformation("Captcha recusado no cadastro: {Erros}.", erros.ToString());
            return ok;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex, "Não foi possível falar com o Turnstile; cadastro liberado sem captcha.");
            return true;
        }
    }
}
