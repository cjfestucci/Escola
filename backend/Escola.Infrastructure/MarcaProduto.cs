namespace Escola.Infrastructure;

/// <summary>Marca do produto (a plataforma, não a escola). Aparece onde ainda não há escola: assunto de e-mail sem nome de cliente,
/// remetente padrão, app autenticador do Suporte, gateway de pagamento. O espelho no front é <c>NOME_PRODUTO</c> (segmento.service.ts).</summary>
public static class MarcaProduto
{
    public const string Nome = "Gestor Tático";

    /// <summary>Nome fantasia do CNPJ que vende o produto: é como a cobrança da assinatura aparece na fatura e no extrato (vem do cadastro
    /// no Asaas). Espelho de EMPRESA_COBRANCA no front.</summary>
    public const string EmpresaCobranca = "CJF Technology";
}
