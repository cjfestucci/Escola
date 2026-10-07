using Escola.Domain.Enums;

namespace Escola.Domain.Entities;

/// <summary>Conta da escola no gateway de pagamento — hoje, uma <b>subconta Asaas</b> criada pela plataforma (modelo marketplace): o
/// dinheiro das mensalidades cai nela e a escola não precisa entrar no painel do Asaas. Uma por cliente.
/// <para>A chave de API da subconta é <b>segredo</b>: fica só criptografada (<see cref="ApiKeyCriptografada"/>), com a chave que vem da
/// configuração do servidor (<c>Segredos:Chave</c>) — quem lê só o banco não consegue usar a conta.</para></summary>
public class ContaPagamento
{
    public Guid Id { get; set; }

    public ProvedorPagamento Provedor { get; set; } = ProvedorPagamento.Asaas;

    /// <summary>Sandbox ou Producao — a mesma conta não serve nos dois ambientes do Asaas.</summary>
    public string Ambiente { get; set; } = string.Empty;

    /// <summary>Id da subconta no Asaas.</summary>
    public string IdExterno { get; set; } = string.Empty;

    /// <summary>Carteira da subconta (usada pelo split, quando a plataforma reter a parte dela).</summary>
    public string? WalletId { get; set; }

    public string ApiKeyCriptografada { get; set; } = string.Empty;

    // Titular (só pra exibir; a fonte da verdade é o Asaas).
    public string TitularNome { get; set; } = string.Empty;
    public string TitularCpfCnpj { get; set; } = string.Empty;
    public string TitularEmail { get; set; } = string.Empty;

    /// <summary>Chave Pix aleatória criada na subconta (o Asaas precisa de uma pra gerar o QR Code das cobranças).</summary>
    public bool ChavePixCriada { get; set; }

    public bool WebhookConfigurado { get; set; }

    /// <summary>Situação da conta no Asaas na última consulta (ex.: "APPROVED", "PENDING", "AWAITING_APPROVAL").</summary>
    public string? SituacaoGateway { get; set; }

    public DateTime CriadaEm { get; set; }
    public DateTime? SituacaoConsultadaEm { get; set; }
}
