using Escola.Domain.Enums;

namespace Escola.Domain.Entities;

public class Usuario
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public PapelUsuario Papel { get; set; }
    public bool Ativo { get; set; } = true;

    /// <summary>Preenchido quando Papel == Responsavel, ligando o login ao cadastro do responsável.</summary>
    public Guid? ResponsavelId { get; set; }
    public Responsavel? Responsavel { get; set; }

    /// <summary>Turmas do educador (Papel == Educador) — pode ter mais de uma.</summary>
    public ICollection<TurmaEducador> Turmas { get; set; } = new List<TurmaEducador>();
}
