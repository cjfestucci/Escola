using Escola.Domain.Enums;

namespace Escola.Domain.Entities;

public class Usuario
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public PapelUsuario Papel { get; set; }

    /// <summary>Preenchido quando Papel == Responsavel, ligando o login ao cadastro do responsável.</summary>
    public Guid? ResponsavelId { get; set; }
    public Responsavel? Responsavel { get; set; }
}
