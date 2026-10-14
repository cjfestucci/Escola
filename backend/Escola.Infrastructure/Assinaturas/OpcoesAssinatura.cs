namespace Escola.Infrastructure.Assinaturas;

/// <summary>Plano e regras da assinatura pelo site (seção <c>Assinatura</c>, por deploy). A tabela de preços do site institucional
/// (<c>site/precos.js</c>) precisa ficar igual a <see cref="PrecoFixo"/>/<see cref="AtletasIncluidos"/>/<see cref="PrecoPorAtleta"/>.</summary>
public class OpcoesAssinatura
{
    public const string Secao = "Assinatura";

    /// <summary>Dias de teste grátis. 0 = sem teste: o clube paga a 1ª fatura antes de receber o acesso.</summary>
    public int DiasTeste { get; set; } = 7;

    /// <summary>Valor mensal mínimo, que já cobre até <see cref="AtletasIncluidos"/> atletas (desde 2026-10-13: R$ 60 com até 20).</summary>
    public decimal PrecoFixo { get; set; } = 60m;
    /// <summary>Atletas cobertos pelo <see cref="PrecoFixo"/>. Acima disso, cada atleta custa <see cref="PrecoPorAtleta"/>.</summary>
    public int AtletasIncluidos { get; set; } = 20;
    /// <summary>Valor de cada atleta <b>acima</b> dos <see cref="AtletasIncluidos"/>.</summary>
    public decimal PrecoPorAtleta { get; set; } = 4m;

    /// <summary>Dias depois do vencimento até suspender o acesso.</summary>
    public int DiasToleranciaAtraso { get; set; } = 7;

    /// <summary>Dias depois do fim do acesso (cancelada) ou da suspensão de um teste que nunca pagou até apagar os dados do clube. O aviso
    /// por e-mail sai 7 dias antes.</summary>
    public int DiasRetencaoAposSuspensao { get; set; } = 30;

    /// <summary>Liga a exclusão de verdade. <b>Desligada por padrão</b>: só registra no log o que seria apagado (e manda o aviso), pra quem
    /// opera conferir antes de ligar.</summary>
    public bool ExclusaoAutomatica { get; set; }

    /// <summary>Token que o Asaas manda no cabeçalho <c>asaas-access-token</c> do webhook da conta raiz. Sem ele o webhook fica desligado
    /// (a situação continua sendo atualizada pela tarefa periódica).</summary>
    public string? WebhookToken { get; set; }

    /// <summary>De quanto em quanto tempo a tarefa de fundo confere as assinaturas no gateway.</summary>
    public int IntervaloAtualizacaoMinutos { get; set; } = 60;

    /// <summary>R$ 60 até 20 atletas; acima disso, + R$ 4 por atleta (com os valores padrão).</summary>
    public decimal CalcularValor(int atletas) =>
        decimal.Round(PrecoFixo + PrecoPorAtleta * Math.Max(0, atletas - Math.Max(0, AtletasIncluidos)), 2);
}
