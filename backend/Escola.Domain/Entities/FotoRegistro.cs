namespace Escola.Domain.Entities;

/// <summary>Uma foto anexada a um registro de rotina (até 4 por registro).</summary>
public class FotoRegistro
{
    public Guid Id { get; set; }

    public Guid RegistroRotinaId { get; set; }
    public RegistroRotina RegistroRotina { get; set; } = null!;

    public string Url { get; set; } = string.Empty;
    public int Ordem { get; set; }
}
