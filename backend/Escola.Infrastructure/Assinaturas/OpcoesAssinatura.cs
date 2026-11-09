using System.Globalization;

namespace Escola.Infrastructure.Assinaturas;

/// <summary>Uma faixa de preço: vale até <paramref name="AtletasAte"/> atletas.</summary>
public record FaixaPreco(int AtletasAte, decimal Preco);

/// <summary>Plano e regras da assinatura pelo site (seção <c>Assinatura</c>, por deploy). A tabela de preços do site institucional
/// (<c>site/precos.js</c>) precisa ficar igual a <see cref="Faixas"/>/<see cref="PrecoPorAtletaAcima"/>.</summary>
public class OpcoesAssinatura
{
    public const string Secao = "Assinatura";

    /// <summary>Faixas padrão (desde 2026-11-09): R$ 59 até 20 atletas, R$ 69 até 30, R$ 129 até 100, R$ 189 até 200 e R$ 249 até 300.</summary>
    public const string FaixasPadrao = "20=59;30=69;100=129;200=189;300=249";

    /// <summary>Dias de teste grátis. 0 = sem teste: o clube paga a 1ª fatura antes de receber o acesso.</summary>
    public int DiasTeste { get; set; } = 7;

    /// <summary>Faixas de preço por quantidade de atletas, no formato <c>"ate=preco;ate=preco"</c>. O valor é o da <b>primeira faixa que
    /// comporta</b> o número de atletas. Configuração inválida cai nas faixas padrão (nunca cobra um valor errado por engano).</summary>
    public string Faixas { get; set; } = FaixasPadrao;

    /// <summary>Valor de cada atleta <b>acima da última faixa</b> (soma-se ao preço dela).</summary>
    public decimal PrecoPorAtletaAcima { get; set; } = 0.60m;

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

    /// <summary>As faixas já lidas, em ordem crescente. Texto inválido, faixa repetida ou preço zerado: volta às faixas padrão.</summary>
    public IReadOnlyList<FaixaPreco> ListarFaixas()
    {
        var lidas = new List<FaixaPreco>();
        foreach (var parte in (Faixas ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var par = parte.Split('=', 2, StringSplitOptions.TrimEntries);
            if (par.Length != 2 || !int.TryParse(par[0], NumberStyles.None, CultureInfo.InvariantCulture, out var ate) || ate < 1
                || !decimal.TryParse(par[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var preco) || preco <= 0)
                return Padrao();
            lidas.Add(new FaixaPreco(ate, preco));
        }
        if (lidas.Count == 0) return Padrao();
        lidas.Sort((a, b) => a.AtletasAte.CompareTo(b.AtletasAte));
        for (var i = 1; i < lidas.Count; i++)
            if (lidas[i].AtletasAte == lidas[i - 1].AtletasAte) return Padrao();
        return lidas;

        static IReadOnlyList<FaixaPreco> Padrao() =>
            new[] { new FaixaPreco(20, 59m), new FaixaPreco(30, 69m), new FaixaPreco(100, 129m), new FaixaPreco(200, 189m), new FaixaPreco(300, 249m) };
    }

    /// <summary>Valor da primeira faixa que comporta os atletas; acima da última, o preço dela + <see cref="PrecoPorAtletaAcima"/> por atleta
    /// extra (com os valores padrão: R$ 59 até 20, R$ 69 até 30, R$ 129 até 100, R$ 189 até 200, R$ 249 até 300, depois + R$ 0,60 por atleta).</summary>
    public decimal CalcularValor(int atletas)
    {
        var faixas = ListarFaixas();
        foreach (var faixa in faixas)
            if (atletas <= faixa.AtletasAte) return decimal.Round(faixa.Preco, 2);
        var ultima = faixas[^1];
        return decimal.Round(ultima.Preco + Math.Max(0m, PrecoPorAtletaAcima) * (atletas - ultima.AtletasAte), 2);
    }
}
