namespace Escola.Domain.Entities;

public class FotoDiarioClasse
{
    public Guid Id { get; set; }
    public Guid RegistroDiarioClasseId { get; set; }
    public RegistroDiarioClasse RegistroDiarioClasse { get; set; } = null!;
    public string Url { get; set; } = string.Empty;
    public int Ordem { get; set; }
}
