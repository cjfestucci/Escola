namespace Escola.Domain.Entities;

/// <summary>Vínculo entre um educador e uma turma — um educador pode ter mais de uma turma.</summary>
public class TurmaEducador
{
    public Guid TurmaId { get; set; }
    public Turma Turma { get; set; } = null!;

    public Guid UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
}
