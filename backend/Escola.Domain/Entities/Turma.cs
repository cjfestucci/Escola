namespace Escola.Domain.Entities;

public class Turma
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;

    public ICollection<Aluno> Alunos { get; set; } = new List<Aluno>();
    public ICollection<TurmaEducador> Educadores { get; set; } = new List<TurmaEducador>();
}
