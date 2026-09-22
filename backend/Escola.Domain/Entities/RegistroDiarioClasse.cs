namespace Escola.Domain.Entities;

/// <summary>Registro pedagógico da turma inteira (atividades do dia) — diferente da rotina individual de cada aluno.</summary>
public class RegistroDiarioClasse
{
    public Guid Id { get; set; }

    public Guid TurmaId { get; set; }
    public Turma Turma { get; set; } = null!;

    public Guid CriadoPorUsuarioId { get; set; }
    public Usuario CriadoPor { get; set; } = null!;
    public DateTime RegistradoEm { get; set; }

    public string Titulo { get; set; } = string.Empty;
    public string? Descricao { get; set; }

    public ICollection<FotoDiarioClasse> Fotos { get; set; } = new List<FotoDiarioClasse>();
}
