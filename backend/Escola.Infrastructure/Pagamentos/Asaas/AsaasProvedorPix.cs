using Escola.Domain.Enums;

namespace Escola.Infrastructure.Pagamentos.Asaas;

/// <summary>Pix com baixa automática pela <b>subconta Asaas da escola</b> (a chave de API é a da subconta, já descriptografada pelo
/// <see cref="ProvedorPixResolver"/>). O Asaas exige um cliente com CPF/CNPJ pra cobrar: é criado no primeiro uso e reaproveitado.</summary>
public sealed class AsaasProvedorPix(AsaasApi api, string apiKeySubconta) : IProvedorPix
{
    public ProvedorPagamento Tipo => ProvedorPagamento.Asaas;

    public bool Configurado => true;

    public bool ExigePagador => true;

    public async Task<CobrancaPixCriada> CriarCobrancaAsync(DadosNovaCobrancaPix dados, CancellationToken ct = default)
    {
        var pagador = dados.Pagador ?? throw new PixProvedorException("O Asaas exige o CPF do responsável financeiro pra gerar o Pix.");

        string? clienteCriado = null;
        var clienteId = pagador.IdClienteExterno;
        if (string.IsNullOrWhiteSpace(clienteId))
        {
            clienteId = await api.CriarClienteAsync(apiKeySubconta, pagador.Nome, pagador.CpfCnpj, pagador.Email, pagador.ResponsavelId.ToString(), ct);
            clienteCriado = clienteId;
        }

        // externalReference = a cobrança do sistema: dá pra achar no painel do Asaas e confere com o que volta no webhook.
        var pagamentoId = await api.CriarCobrancaPixAsync(apiKeySubconta, clienteId, dados.Valor, dados.Vencimento, dados.Descricao, dados.CobrancaId.ToString(), ct);
        var copiaECola = await api.CopiaEColaAsync(apiKeySubconta, pagamentoId, ct);
        return new CobrancaPixCriada(pagamentoId, copiaECola, clienteCriado);
    }

    public async Task<CobrancaPixConsultada?> ConsultarCobrancaAsync(string idExterno, CancellationToken ct = default)
    {
        var pagamento = await api.ConsultarPagamentoAsync(apiKeySubconta, idExterno, ct);
        if (pagamento is null) return null;

        // RECEIVED = Pix compensado; CONFIRMED = pago e confirmado; RECEIVED_IN_CASH = baixado à mão no Asaas.
        if (pagamento.Status is "RECEIVED" or "CONFIRMED" or "RECEIVED_IN_CASH")
        {
            // O Asaas devolve só a data do pagamento: meio-dia de Brasília (15h UTC) cai no mesmo dia em qualquer fuso do Brasil.
            var dia = pagamento.DataPagamento ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var horario = dia.ToDateTime(new TimeOnly(15, 0), DateTimeKind.Utc);
            return new CobrancaPixConsultada("CONCLUIDA", [new PixRecebido(pagamento.Id, pagamento.Valor, horario)]);
        }

        // Estornos e disputas: a cobrança não está mais "em aberto" do lado do gateway (tratar devolução fica fora do escopo).
        if (pagamento.Status.StartsWith("REFUND", StringComparison.Ordinal) || pagamento.Status.StartsWith("CHARGEBACK", StringComparison.Ordinal))
            return new CobrancaPixConsultada("REMOVIDA_PELO_PSP", []);

        return new CobrancaPixConsultada("ATIVA", []);
    }

    public Task RegistrarWebhookAsync(string chavePix, string urlWebhook, CancellationToken ct = default) =>
        throw new PixProvedorException("No Asaas o webhook é configurado na criação da conta da escola.");

    public async Task TestarConexaoAsync(CancellationToken ct = default) => await api.SituacaoContaAsync(apiKeySubconta, ct);
}
