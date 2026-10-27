namespace Escola.Infrastructure.Pagamentos.Asaas;

/// <summary>Identificação do Asaas como prestador dos serviços financeiros, exigida no modelo BaaS (Playbook de BaaS do Asaas,
/// Resolução Conjunta nº 16/2025 do Banco Central) onde houver movimentação de valores. Os endereços dos selos (com o nosso
/// <c>id</c>) vieram do Asaas e são públicos. O mesmo selo e texto existem no frontend (<c>app-aviso-asaas</c>).</summary>
public static class SeloAsaas
{
    public const string UrlPositivo =
        "https://baas.asaas.com/selos/Servicos_financeiros_Asaas-Reduzida-Positivo.svg?id=99abe421-e0b4-4a10-a6cb-5254a527daba";

    /// <summary>Rodapé dos e-mails de cobrança cujo Pix sai da subconta do Asaas.</summary>
    public const string AvisoEmailHtml = $"""
        <hr style="border:0;border-top:1px solid #E5E7EB;margin:20px 0 12px;"/>
        <p style="margin:0 0 8px;"><a href="https://www.asaas.com"><img src="{UrlPositivo}" alt="Serviços financeiros prestados pelo Asaas" width="160" height="48" style="display:inline-block;"/></a></p>
        <p style="margin:0;font-size:12px;color:#6B7280;">O pagamento por Pix é processado pelo Asaas Gestão Financeira Instituição de Pagamentos S.A., instituição de pagamento autorizada a funcionar pelo Banco Central do Brasil. Suporte do Asaas para operações financeiras: 0800 009 0037 (somente pessoa jurídica e mensagens) ou contato@asaas.com.br.</p>
        """;
}
