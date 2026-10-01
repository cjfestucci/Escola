namespace Escola.Domain.Entities;

public class Fornecedor
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Documento { get; set; }
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public bool Ativo { get; set; } = true;

    public ICollection<ContaPagar> ContasPagar { get; set; } = new List<ContaPagar>();
}
