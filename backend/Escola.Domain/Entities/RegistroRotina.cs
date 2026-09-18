namespace Escola.Domain.Entities;

/// <summary>
/// Um evento da rotina diária de um aluno. Cada categoria (alimentação, sono, higiene,
/// humor, momento livre) é uma subclasse com os campos que só fazem sentido para ela.
/// </summary>
public abstract class RegistroRotina
{
    public Guid Id { get; set; }

    public Guid AlunoId { get; set; }
    public Aluno Aluno { get; set; } = null!;

    public Guid CriadoPorUsuarioId { get; set; }
    public Usuario CriadoPor { get; set; } = null!;

    public DateTime RegistradoEm { get; set; }
    public string? Observacao { get; set; }

    /// <summary>Até 4 fotos, na ordem em que foram anexadas.</summary>
    public ICollection<FotoRegistro> Fotos { get; set; } = new List<FotoRegistro>();
}
