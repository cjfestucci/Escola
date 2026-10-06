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

    /// <summary>Só valem os tokens emitidos a partir deste instante: o que permite derrubar as sessões já abertas (conta desativada, senha
    /// trocada, "encerrar sessões"). Nulo = nenhuma revogação feita. O JWT dura dias e não tem como ser "desemitido"; por isso cada
    /// requisição confere esta data (e <see cref="Ativo"/>) no banco.</summary>
    public DateTime? SessoesValidasDesde { get; set; }

    /// <summary>Invalida todos os tokens emitidos até agora (o próximo login emite um novo, que vale).</summary>
    public void EncerrarSessoes() => SessoesValidasDesde = DateTime.UtcNow;

    /// <summary>Preenchido quando Papel == Responsavel, ligando o login ao cadastro do responsável.</summary>
    public Guid? ResponsavelId { get; set; }
    public Responsavel? Responsavel { get; set; }

    /// <summary>Turmas do educador (Papel == Educador) — pode ter mais de uma.</summary>
    public ICollection<TurmaEducador> Turmas { get; set; } = new List<TurmaEducador>();
}
