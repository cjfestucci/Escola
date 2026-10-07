using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Escola.Infrastructure.Clientes;

namespace Escola.Infrastructure.Pagamentos;

/// <summary>Cliente da API Pix do Banco do Brasil (padrão do Banco Central: <c>/cob</c>, <c>/webhook</c>). OAuth2 por
/// <i>client credentials</i>; em produção o BB exige certificado de cliente (mTLS), configurado no <see cref="HttpClient"/> (ver Program.cs).
/// <b>Atenção:</b> endereços, escopos e nome do parâmetro da chave de aplicação seguem o que o BB documenta, mas por serem de
/// terceiro estão todos em <see cref="OpcoesPixBb"/> e devem ser conferidos na homologação do BB antes de ir pra produção.</summary>
public sealed class BbPixClient(HttpClient http, IOptions<OpcoesPixBb> opcoes, CacheTokenBb cache, IClienteAtual clienteAtual, ILogger<BbPixClient> logger) : IProvedorPix
{
    private readonly OpcoesPixBb _o = opcoes.Value;

    /// <summary>Configurado E é o cliente dono das credenciais (ver <see cref="OpcoesPixBb.ClienteId"/>): pros demais clientes, a
    /// integração simplesmente não existe e eles seguem com o Pix estático.</summary>
    public bool Configurado => _o.Configurado && clienteAtual.Definido && clienteAtual.Id == _o.ClienteId;

    public async Task<CobrancaPixCriada> CriarCobrancaAsync(string txid, string chavePix, decimal valor, int expiracaoSegundos, string? solicitacaoPagador, CancellationToken ct = default)
    {
        var corpo = new Dictionary<string, object?>
        {
            ["calendario"] = new { expiracao = expiracaoSegundos },
            ["valor"] = new { original = valor.ToString("F2", CultureInfo.InvariantCulture) },
            ["chave"] = chavePix
        };
        if (!string.IsNullOrWhiteSpace(solicitacaoPagador))
            corpo["solicitacaoPagador"] = solicitacaoPagador.Length > 140 ? solicitacaoPagador[..140] : solicitacaoPagador;

        using var resposta = await EnviarAsync(HttpMethod.Put, $"/cob/{txid}", corpo, ct);
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync(ct));

        var copiaECola = json.RootElement.TryGetProperty("pixCopiaECola", out var campo) ? campo.GetString() : null;
        if (string.IsNullOrWhiteSpace(copiaECola))
            throw new PixProvedorException("O banco criou a cobrança, mas não devolveu o código Pix copia e cola.");
        return new CobrancaPixCriada(copiaECola);
    }

    public async Task<CobrancaPixConsultada?> ConsultarCobrancaAsync(string txid, CancellationToken ct = default)
    {
        using var resposta = await EnviarAsync(HttpMethod.Get, $"/cob/{txid}", null, ct, aceitar404: true);
        if (resposta.StatusCode == HttpStatusCode.NotFound) return null;

        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync(ct));
        var raiz = json.RootElement;
        var status = (raiz.TryGetProperty("status", out var s) ? s.GetString() : null)?.ToUpperInvariant() ?? "ATIVA";

        var recebidos = new List<PixRecebido>();
        if (raiz.TryGetProperty("pix", out var pix) && pix.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in pix.EnumerateArray())
            {
                var e2e = item.TryGetProperty("endToEndId", out var e) ? e.GetString() : null;
                var valorTexto = item.TryGetProperty("valor", out var v) ? v.GetString() : null;
                var horarioTexto = item.TryGetProperty("horario", out var h) ? h.GetString() : null;
                if (e2e is null || !decimal.TryParse(valorTexto, NumberStyles.Number, CultureInfo.InvariantCulture, out var valor)) continue;

                var horario = DateTimeOffset.TryParse(horarioTexto, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var h2)
                    ? h2.UtcDateTime
                    : DateTime.UtcNow;
                recebidos.Add(new PixRecebido(e2e, valor, horario));
            }
        }

        return new CobrancaPixConsultada(status, recebidos);
    }

    public async Task RegistrarWebhookAsync(string chavePix, string urlWebhook, CancellationToken ct = default)
    {
        using var _ = await EnviarAsync(HttpMethod.Put, $"/webhook/{Uri.EscapeDataString(chavePix)}", new { webhookUrl = urlWebhook }, ct);
    }

    public async Task TestarConexaoAsync(CancellationToken ct = default) => await ObterTokenAsync(ct);

    // ---------- HTTP ----------

    private async Task<HttpResponseMessage> EnviarAsync(HttpMethod metodo, string caminho, object? corpo, CancellationToken ct, bool aceitar404 = false)
    {
        var token = await ObterTokenAsync(ct);
        var url = $"{_o.UrlApiEfetiva}{caminho}?{_o.ParametroChaveAplicacaoEfetivo}={Uri.EscapeDataString(_o.ChaveAplicacao!)}";

        using var requisicao = new HttpRequestMessage(metodo, url);
        requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (corpo is not null)
            requisicao.Content = new StringContent(JsonSerializer.Serialize(corpo), Encoding.UTF8, "application/json");

        HttpResponseMessage resposta;
        try
        {
            resposta = await http.SendAsync(requisicao, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new PixProvedorException("Não foi possível falar com o Banco do Brasil (rede ou certificado).", inner: ex);
        }

        if (resposta.IsSuccessStatusCode || (aceitar404 && resposta.StatusCode == HttpStatusCode.NotFound))
            return resposta;

        var detalhe = await resposta.Content.ReadAsStringAsync(ct);
        var status = (int)resposta.StatusCode;
        resposta.Dispose();
        logger.LogWarning("BB Pix {Metodo} {Caminho} respondeu {Status}: {Detalhe}", metodo, caminho, status, Truncar(detalhe));
        throw new PixProvedorException($"O Banco do Brasil recusou a requisição (HTTP {status}).", status);
    }

    private async Task<string> ObterTokenAsync(CancellationToken ct)
    {
        if (cache.Token is not null && DateTime.UtcNow < cache.ExpiraEm) return cache.Token;

        await cache.Lock.WaitAsync(ct);
        try
        {
            if (cache.Token is not null && DateTime.UtcNow < cache.ExpiraEm) return cache.Token;

            using var requisicao = new HttpRequestMessage(HttpMethod.Post, _o.UrlOauthEfetiva);
            var basico = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_o.ClientId}:{_o.ClientSecret}"));
            requisicao.Headers.Authorization = new AuthenticationHeaderValue("Basic", basico);
            requisicao.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["scope"] = _o.Escopos
            });

            HttpResponseMessage resposta;
            try
            {
                resposta = await http.SendAsync(requisicao, ct);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                throw new PixProvedorException("Não foi possível falar com o serviço de autenticação do Banco do Brasil (rede ou certificado).", inner: ex);
            }

            using (resposta)
            {
                var texto = await resposta.Content.ReadAsStringAsync(ct);
                if (!resposta.IsSuccessStatusCode)
                {
                    logger.LogWarning("BB OAuth respondeu {Status}: {Detalhe}", (int)resposta.StatusCode, Truncar(texto));
                    throw new PixProvedorException($"O Banco do Brasil recusou as credenciais (HTTP {(int)resposta.StatusCode}). Confira Client ID, Client Secret e escopos.", (int)resposta.StatusCode);
                }

                using var json = JsonDocument.Parse(texto);
                var token = json.RootElement.GetProperty("access_token").GetString()!;
                var segundos = json.RootElement.TryGetProperty("expires_in", out var e) && e.TryGetInt32(out var n) ? n : 300;
                // Renova um pouco antes de vencer, pra nunca mandar um token no limite.
                cache.Token = token;
                cache.ExpiraEm = DateTime.UtcNow.AddSeconds(Math.Max(30, segundos - 30));
                return token;
            }
        }
        finally
        {
            cache.Lock.Release();
        }
    }

    private static string Truncar(string texto) => texto.Length <= 300 ? texto : texto[..300] + "…";

}

/// <summary>Guarda o token OAuth entre as chamadas. É um singleton porque o <see cref="BbPixClient"/> é criado de novo a cada uso
/// (cliente tipado do HttpClient) — sem isso pediríamos um token novo ao banco a cada consulta.</summary>
public sealed class CacheTokenBb
{
    public SemaphoreSlim Lock { get; } = new(1, 1);
    public string? Token { get; set; }
    public DateTime ExpiraEm { get; set; }
}
