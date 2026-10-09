using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Escola.Tests;

/// <summary>Imita a API v3 do Asaas o suficiente pros testes (subconta, chave Pix, cliente, cobrança Pix, consulta). Guarda as chamadas
/// recebidas e deixa o teste decidir a situação de cada pagamento.</summary>
public sealed class AsaasFalso
{
    public const string ApiKeySubconta = "$aact_hmlg_subconta_de_teste";

    private int _sequencia;
    public List<(string Metodo, string Caminho, string? ApiKey, JsonNode? Corpo)> Chamadas { get; } = [];
    /// <summary>pagamentoId → (status, valor, data do pagamento).</summary>
    public Dictionary<string, (string Status, decimal Valor, string? DataPagamento)> Pagamentos { get; } = [];

    public HttpMessageHandler NovoHandler() => new Handler(this);

    private HttpResponseMessage Responder(HttpRequestMessage req, JsonNode? corpo)
    {
        var caminho = req.RequestUri!.AbsolutePath;
        caminho = caminho[(caminho.IndexOf("/v3", StringComparison.Ordinal) + 3)..];
        var apiKey = req.Headers.TryGetValues("access_token", out var v) ? v.FirstOrDefault() : null;
        lock (Chamadas) Chamadas.Add((req.Method.Method, caminho, apiKey, corpo));

        object? resposta = (req.Method.Method, caminho) switch
        {
            ("POST", "/accounts") => new { id = "acc_teste", walletId = "wallet_teste", apiKey = ApiKeySubconta },
            ("GET", "/pix/addressKeys") => new { data = Array.Empty<object>() },
            ("POST", "/pix/addressKeys") => new { id = "chave_teste", status = "ACTIVE" },
            ("GET", "/myAccount/status") => new { general = "APPROVED" },
            ("POST", "/customers") => new { id = $"cus_{Interlocked.Increment(ref _sequencia)}" },
            ("POST", "/payments") => NovoPagamento(corpo!),
            ("POST", "/subscriptions") => NovaAssinatura(corpo!),
            ("GET", var c) when c.StartsWith("/subscriptions/") && c.EndsWith("/payments") => FaturasDaAssinatura(c.Split('/')[2]),
            ("GET", var c) when c.EndsWith("/pixQrCode") => new { payload = $"00020126ASAAS{c.Split('/')[2]}6304ABCD", encodedImage = "" },
            ("GET", var c) when c.StartsWith("/payments/") => Consultar(c["/payments/".Length..]),
            _ => null
        };
        if (resposta is null) return new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("{}") };
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(resposta), Encoding.UTF8, "application/json")
        };
    }

    private object NovoPagamento(JsonNode corpo)
    {
        var id = $"pay_{Interlocked.Increment(ref _sequencia)}";
        lock (Pagamentos) Pagamentos[id] = ("PENDING", corpo["value"]!.GetValue<decimal>(), null);
        return new { id, status = "PENDING" };
    }

    /// <summary>assinaturaId → faturas (id do pagamento + vencimento), na ordem em que foram geradas.</summary>
    public Dictionary<string, List<(string PagamentoId, string Vencimento)>> Assinaturas { get; } = [];

    /// <summary>A fatura passa a constar como paga hoje (como se o clube tivesse pagado na página do Asaas).</summary>
    public void Pagar(string pagamentoId)
    {
        lock (Pagamentos)
        {
            var p = Pagamentos[pagamentoId];
            Pagamentos[pagamentoId] = ("RECEIVED", p.Valor, DateTime.Today.ToString("yyyy-MM-dd"));
        }
    }

    /// <summary>Muda o vencimento de uma fatura (pra simular o tempo passando).</summary>
    public void MudarVencimento(string assinaturaId, int indice, DateOnly vencimento)
    {
        lock (Pagamentos)
        {
            var lista = Assinaturas[assinaturaId];
            lista[indice] = (lista[indice].PagamentoId, vencimento.ToString("yyyy-MM-dd"));
        }
    }

    private object NovaAssinatura(JsonNode corpo)
    {
        var id = $"sub_{Interlocked.Increment(ref _sequencia)}";
        var pagamento = $"pay_{Interlocked.Increment(ref _sequencia)}";
        lock (Pagamentos)
        {
            Pagamentos[pagamento] = ("PENDING", corpo["value"]!.GetValue<decimal>(), null);
            Assinaturas[id] = [(pagamento, corpo["nextDueDate"]!.GetValue<string>())];
        }
        return new { id, status = "ACTIVE" };
    }

    private object? FaturasDaAssinatura(string id)
    {
        lock (Pagamentos)
        {
            if (!Assinaturas.TryGetValue(id, out var lista)) return null;
            return new
            {
                data = lista.Select(f => new
                {
                    id = f.PagamentoId, status = Pagamentos[f.PagamentoId].Status, value = Pagamentos[f.PagamentoId].Valor,
                    dueDate = f.Vencimento, clientPaymentDate = Pagamentos[f.PagamentoId].DataPagamento,
                    invoiceUrl = $"https://asaas.teste/i/{f.PagamentoId}"
                }).ToList()
            };
        }
    }

    private object? Consultar(string id)
    {
        lock (Pagamentos)
            return Pagamentos.TryGetValue(id, out var p)
                ? new { id, status = p.Status, value = p.Valor, clientPaymentDate = p.DataPagamento }
                : null;
    }

    private sealed class Handler(AsaasFalso asaas) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var texto = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            return asaas.Responder(request, string.IsNullOrWhiteSpace(texto) ? null : JsonNode.Parse(texto));
        }
    }
}
