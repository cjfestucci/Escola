using Escola.Domain.Enums;

namespace Escola.Domain.Entities;

/// <summary>Um cliente do produto (uma escola, um clube...). O banco é compartilhado entre clientes, mas cada um tem
/// o próprio site/login e só enxerga os próprios dados: toda tabela de dados guarda o `ClienteId` (propriedade oculta,
/// definida no `EscolaDbContext`) e as consultas são filtradas por ele automaticamente.</summary>
public class Cliente
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;

    /// <summary>Escola infantil ou clube/academia — define o vocabulário e as telas do app. Definido ao provisionar o
    /// cliente (config <c>Cliente:Segmento</c>) e depois só alterado por quem opera o produto, nunca pela tela de configuração.</summary>
    public SegmentoCliente Segmento { get; set; } = SegmentoCliente.Escola;
    public DateTime CriadoEm { get; set; }

    /// <summary>Quando os dados do cliente foram apagados (retenção/LGPD). Cliente excluído fica só como registro anonimizado: nenhuma conta
    /// é recriada nele (nem a do Suporte).</summary>
    public DateTime? ExcluidoEm { get; set; }
}
