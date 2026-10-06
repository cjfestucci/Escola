using Escola.Domain.Enums;

namespace Escola.Domain.Entities;

/// <summary>Presença de um aluno/atleta num dia, na chamada de uma turma. No máximo um registro por aluno por dia.
/// "Dia com chamada" não é uma entidade: é qualquer dia em que a turma tem ao menos um registro.</summary>
public class RegistroPresenca
{
    public Guid Id { get; set; }

    /// <summary>Turma em que a chamada foi feita (o aluno pode mudar de turma depois, o registro não muda).</summary>
    public Guid TurmaId { get; set; }
    public Turma Turma { get; set; } = null!;

    public Guid AlunoId { get; set; }
    public Aluno Aluno { get; set; } = null!;

    public DateOnly Data { get; set; }
    public StatusPresenca Status { get; set; }

    public Guid UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public DateTime RegistradoEm { get; set; }
}
