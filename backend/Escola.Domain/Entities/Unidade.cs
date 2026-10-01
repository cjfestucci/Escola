namespace Escola.Domain.Entities;

public class Unidade
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Endereco { get; set; }
    public string? Telefone { get; set; }
    public bool Ativa { get; set; } = true;

    public ICollection<Turma> Turmas { get; set; } = new List<Turma>();
}
