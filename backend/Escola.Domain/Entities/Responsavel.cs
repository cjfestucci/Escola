namespace Escola.Domain.Entities;

public class Responsavel
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefone { get; set; }

    public ICollection<AlunoResponsavel> Alunos { get; set; } = new List<AlunoResponsavel>();
}
