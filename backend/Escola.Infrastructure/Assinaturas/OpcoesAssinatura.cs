namespace Escola.Infrastructure.Assinaturas;

/// <summary>Plano e regras da assinatura pelo site (seção <c>Assinatura</c>, por deploy). A tabela de preços do site institucional
/// (<c>site/precos.js</c>) precisa ficar igual a <see cref="PrecoFixo"/>/<see cref="PrecoPorAtleta"/>.</summary>
public class OpcoesAssinatura
{
    public const string Secao = "Assinatura";

    /// <summary>Dias de teste grátis. 0 = sem teste: o clube paga a 1ª fatura antes de receber o acesso.</summary>
    public int DiasTeste { get; set; } = 7;

    public decimal PrecoFixo { get; set; } = 99m;
    public decimal PrecoPorAtleta { get; set; } = 3m;

    /// <summary>Dias depois do vencimento até suspender o acesso.</summary>
    public int DiasToleranciaAtraso { get; set; } = 7;

    /// <summary>Dias depois da suspensão de um teste que nunca pagou até apagar os dados do clube. (Ainda não aplicado: a limpeza
    /// automática é da fase 3 — ver docs/ASSINATURA.md.)</summary>
    public int DiasRetencaoAposSuspensao { get; set; } = 30;

    /// <summary>Token que o Asaas manda no cabeçalho <c>asaas-access-token</c> do webhook da conta raiz. Sem ele o webhook fica desligado
    /// (a situação continua sendo atualizada pela tarefa periódica).</summary>
    public string? WebhookToken { get; set; }

    /// <summary>De quanto em quanto tempo a tarefa de fundo confere as assinaturas no gateway.</summary>
    public int IntervaloAtualizacaoMinutos { get; set; } = 60;

    public decimal CalcularValor(int atletas) => decimal.Round(PrecoFixo + PrecoPorAtleta * Math.Max(0, atletas), 2);
}
