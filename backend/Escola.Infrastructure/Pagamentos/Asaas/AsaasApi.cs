using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Escola.Infrastructure.Pagamentos.Asaas;

/// <summary>Configuração da plataforma no Asaas (seção <c>Asaas</c>, por deploy — credencial, nunca no banco nem no repositório).</summary>
public class OpcoesAsaas
{
    public const string Secao = "Asaas";

    /// <summary>Chave de API da <b>conta raiz</b> da plataforma (cria as subcontas das escolas). Sandbox começa com <c>$aact_hmlg_</c>.</summary>
    public string? ApiKey { get; set; }

    /// <summary><c>Sandbox</c> (padrão) ou <c>Producao</c>.</summary>
    public string Ambiente { get; set; } = "Sandbox";

    /// <summary>Sobrescreve o endereço da API (testes). Sem valor: o oficial do ambiente.</summary>
    public string? UrlApi { get; set; }

    /// <summary>Token que o Asaas manda no cabeçalho <c>asaas-access-token</c> de cada webhook. Sem ele, o webhook fica desligado.</summary>
    public string? WebhookToken { get; set; }

    /// <summary>Endereço público da API (ex.: https://app.exemplo.com.br). O webhook vai pra {WebhookUrlBase}/api/pagamentos/asaas/webhook.
    /// Sem ele, as subcontas são criadas sem webhook e a baixa vem só da conferência periódica.</summary>
    public string? WebhookUrlBase { get; set; }

    public bool Configurado => !string.IsNullOrWhiteSpace(ApiKey);

    public bool Producao => string.Equals(Ambiente, "Producao", StringComparison.OrdinalIgnoreCase);

    public string NomeAmbiente => Producao ? "Producao" : "Sandbox";

    public string UrlApiEfetiva => (UrlApi ?? (Producao ? "https://api.asaas.com/v3" : "https://api-sandbox.asaas.com/v3")).TrimEnd('/');

    public bool WebhookHabilitado => !string.IsNullOrWhiteSpace(WebhookToken) && !string.IsNullOrWhiteSpace(WebhookUrlBase);
}

/// <summary>Dados exigidos pelo Asaas pra abrir a subconta (KYC da escola).</summary>
public sealed record DadosSubconta(
    string Nome,
    string Email,
    string CpfCnpj,
    DateOnly? DataNascimento,
    string? TipoEmpresa,
    string Celular,
    string Endereco,
    string Numero,
    string? Complemento,
    string Bairro,
    string Cep,
    decimal FaturamentoMensal);

public sealed record SubcontaCriada(string Id, string? WalletId, string ApiKey);

public sealed record PagamentoAsaas(string Id, string Status, decimal Valor, DateOnly? DataPagamento, string? ReferenciaExterna);

/// <summary>Chamadas HTTP à API v3 do Asaas. Autenticação pelo cabeçalho <c>access_token</c> (+ <c>User-Agent</c>, obrigatório pra contas
/// raiz novas). As chamadas da escola usam a chave da <b>subconta</b>; criar subconta usa a da conta raiz.</summary>
public sealed class AsaasApi(HttpClient http, IOptions<OpcoesAsaas> opcoes, ILogger<AsaasApi> logger)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly OpcoesAsaas _o = opcoes.Value;

    public async Task<SubcontaCriada> CriarSubcontaAsync(DadosSubconta dados, CancellationToken ct = default)
    {
        var corpo = new JsonObject
        {
            ["name"] = dados.Nome,
            ["email"] = dados.Email,
            ["cpfCnpj"] = dados.CpfCnpj,
            ["mobilePhone"] = dados.Celular,
            ["address"] = dados.Endereco,
            ["addressNumber"] = dados.Numero,
            ["province"] = dados.Bairro,
            ["postalCode"] = dados.Cep,
            ["incomeValue"] = dados.FaturamentoMensal
        };
        if (!string.IsNullOrWhiteSpace(dados.Complemento)) corpo["complement"] = dados.Complemento;
        if (dados.DataNascimento is { } nascimento) corpo["birthDate"] = nascimento.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (!string.IsNullOrWhiteSpace(dados.TipoEmpresa)) corpo["companyType"] = dados.TipoEmpresa;

        // Webhook já na criação: os eventos de pagamento da subconta chegam direto na plataforma.
        if (_o.WebhookHabilitado)
        {
            corpo["webhooks"] = new JsonArray(new JsonObject
            {
                ["name"] = $"{MarcaProduto.Nome} — pagamentos",
                ["url"] = $"{_o.WebhookUrlBase!.TrimEnd('/')}/api/pagamentos/asaas/webhook",
                ["email"] = dados.Email,
                ["enabled"] = true,
                ["interrupted"] = false,
                ["authToken"] = _o.WebhookToken,
                ["sendType"] = "SEQUENTIALLY",
                ["events"] = new JsonArray("PAYMENT_RECEIVED", "PAYMENT_CONFIRMED", "PAYMENT_UPDATED", "PAYMENT_DELETED", "PAYMENT_REFUNDED")
            });
        }

        using var json = await EnviarAsync(_o.ApiKey, HttpMethod.Post, "/accounts", corpo, ct);
        var raiz = json!.RootElement;
        var id = Texto(raiz, "id");
        var apiKey = Texto(raiz, "apiKey");
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(apiKey))
            throw new PixProvedorException("O Asaas criou a conta, mas não devolveu o identificador ou a chave de API dela.");
        return new SubcontaCriada(id, Texto(raiz, "walletId"), apiKey);
    }

    /// <summary>Chave Pix aleatória na subconta — o Asaas precisa de uma pra gerar QR Code das cobranças. Idempotente do nosso lado:
    /// se já existir uma ativa, não cria outra.</summary>
    public async Task CriarChavePixAleatoriaAsync(string apiKeySubconta, CancellationToken ct = default)
    {
        using (var lista = await EnviarAsync(apiKeySubconta, HttpMethod.Get, "/pix/addressKeys?status=ACTIVE", null, ct))
        {
            if (lista!.RootElement.TryGetProperty("data", out var dados) && dados.ValueKind == JsonValueKind.Array && dados.GetArrayLength() > 0)
                return;
        }
        using var _ = await EnviarAsync(apiKeySubconta, HttpMethod.Post, "/pix/addressKeys", new JsonObject { ["type"] = "EVP" }, ct);
    }

    /// <summary>Situação geral da conta (ex.: APPROVED, PENDING, AWAITING_APPROVAL, REJECTED).</summary>
    public async Task<string?> SituacaoContaAsync(string apiKeySubconta, CancellationToken ct = default)
    {
        using var json = await EnviarAsync(apiKeySubconta, HttpMethod.Get, "/myAccount/status", null, ct);
        return Texto(json!.RootElement, "general");
    }

    public async Task<string> CriarClienteAsync(string apiKeySubconta, string nome, string cpfCnpj, string? email, string referenciaExterna, CancellationToken ct = default)
    {
        var corpo = new JsonObject
        {
            ["name"] = nome,
            ["cpfCnpj"] = cpfCnpj,
            ["externalReference"] = referenciaExterna,
            // As notificações à família são da escola (portal/e-mail do sistema), não do Asaas.
            ["notificationDisabled"] = true
        };
        if (!string.IsNullOrWhiteSpace(email)) corpo["email"] = email;

        using var json = await EnviarAsync(apiKeySubconta, HttpMethod.Post, "/customers", corpo, ct);
        return Texto(json!.RootElement, "id") ?? throw new PixProvedorException("O Asaas não devolveu o identificador do cliente.");
    }

    public async Task<string> CriarCobrancaPixAsync(string apiKeySubconta, string clienteId, decimal valor, DateOnly vencimento, string descricao, string referenciaExterna, CancellationToken ct = default)
    {
        var corpo = new JsonObject
        {
            ["customer"] = clienteId,
            ["billingType"] = "PIX",
            ["value"] = valor,
            ["dueDate"] = vencimento.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["description"] = descricao.Length > 500 ? descricao[..500] : descricao,
            ["externalReference"] = referenciaExterna
        };
        using var json = await EnviarAsync(apiKeySubconta, HttpMethod.Post, "/payments", corpo, ct);
        return Texto(json!.RootElement, "id") ?? throw new PixProvedorException("O Asaas não devolveu o identificador da cobrança.");
    }

    public async Task<string> CopiaEColaAsync(string apiKeySubconta, string pagamentoId, CancellationToken ct = default)
    {
        using var json = await EnviarAsync(apiKeySubconta, HttpMethod.Get, $"/payments/{Uri.EscapeDataString(pagamentoId)}/pixQrCode", null, ct);
        var payload = Texto(json!.RootElement, "payload");
        return string.IsNullOrWhiteSpace(payload)
            ? throw new PixProvedorException("O Asaas criou a cobrança, mas não devolveu o código Pix copia e cola.")
            : payload;
    }

    /// <summary>Nulo se o pagamento não existe (ou foi excluído).</summary>
    public async Task<PagamentoAsaas?> ConsultarPagamentoAsync(string apiKeySubconta, string pagamentoId, CancellationToken ct = default)
    {
        using var json = await EnviarAsync(apiKeySubconta, HttpMethod.Get, $"/payments/{Uri.EscapeDataString(pagamentoId)}", null, ct, aceitar404: true);
        if (json is null) return null;
        var raiz = json.RootElement;
        if (raiz.TryGetProperty("deleted", out var excluido) && excluido.ValueKind == JsonValueKind.True) return null;

        var data = Texto(raiz, "clientPaymentDate") ?? Texto(raiz, "paymentDate");
        return new PagamentoAsaas(
            Texto(raiz, "id") ?? pagamentoId,
            (Texto(raiz, "status") ?? "PENDING").ToUpperInvariant(),
            raiz.TryGetProperty("value", out var v) && v.TryGetDecimal(out var valor) ? valor : 0m,
            DateOnly.TryParse(data, CultureInfo.InvariantCulture, out var d) ? d : null,
            Texto(raiz, "externalReference"));
    }

    /// <summary>Prova que a chave da conta raiz e o endereço estão certos, sem criar nada.</summary>
    public async Task TestarContaRaizAsync(CancellationToken ct = default)
    {
        using var _ = await EnviarAsync(_o.ApiKey, HttpMethod.Get, "/myAccount/status", null, ct);
    }

    private static string? Texto(JsonElement raiz, string campo) =>
        raiz.TryGetProperty(campo, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private async Task<JsonDocument?> EnviarAsync(string? apiKey, HttpMethod metodo, string caminho, JsonObject? corpo, CancellationToken ct, bool aceitar404 = false)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new PixProvedorException("A integração com o Asaas não está configurada neste ambiente.");

        using var requisicao = new HttpRequestMessage(metodo, _o.UrlApiEfetiva + caminho);
        requisicao.Headers.TryAddWithoutValidation("access_token", apiKey);
        requisicao.Headers.UserAgent.Add(new ProductInfoHeaderValue("GestorTatico", "1.0"));
        requisicao.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (corpo is not null)
            requisicao.Content = new StringContent(corpo.ToJsonString(Json), Encoding.UTF8, "application/json");

        HttpResponseMessage resposta;
        try
        {
            resposta = await http.SendAsync(requisicao, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new PixProvedorException("Não foi possível falar com o Asaas agora. Tente de novo em instantes.", null, ex);
        }

        using (resposta)
        {
            var texto = await resposta.Content.ReadAsStringAsync(ct);
            if (aceitar404 && resposta.StatusCode == HttpStatusCode.NotFound) return null;
            if (resposta.IsSuccessStatusCode)
                return JsonDocument.Parse(string.IsNullOrWhiteSpace(texto) ? "{}" : texto);

            // O Asaas devolve { errors: [{ code, description }] } — a descrição é em português e segura de mostrar (não traz segredo).
            var descricao = ExtrairErro(texto);
            logger.LogWarning("Asaas respondeu {Status} em {Metodo} {Caminho}: {Erro}", (int)resposta.StatusCode, metodo, caminho, descricao);
            var mensagem = resposta.StatusCode == HttpStatusCode.Unauthorized
                ? "O Asaas recusou a chave de API (confira a configuração do ambiente)."
                : descricao ?? $"O Asaas recusou a operação (HTTP {(int)resposta.StatusCode}).";
            throw new PixProvedorException(mensagem, (int)resposta.StatusCode);
        }
    }

    private static string? ExtrairErro(string texto)
    {
        try
        {
            using var json = JsonDocument.Parse(texto);
            if (json.RootElement.TryGetProperty("errors", out var erros) && erros.ValueKind == JsonValueKind.Array)
                return string.Join(" ", erros.EnumerateArray().Select(e => Texto(e, "description")).Where(d => !string.IsNullOrWhiteSpace(d)));
        }
        catch (JsonException)
        {
            // corpo não é JSON
        }
        return null;
    }
}
