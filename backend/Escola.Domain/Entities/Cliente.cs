namespace Escola.Domain.Entities;

/// <summary>Um cliente do produto (uma escola, um clube...). O banco é compartilhado entre clientes, mas cada um tem
/// o próprio site/login e só enxerga os próprios dados: toda tabela de dados guarda o `ClienteId` (propriedade oculta,
/// definida no `EscolaDbContext`) e as consultas são filtradas por ele automaticamente.</summary>
public class Cliente
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; }
}
