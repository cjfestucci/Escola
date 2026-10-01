using Escola.Domain.Enums;

namespace Escola.Domain.Entities;

/// <summary>
/// Trilha de auditoria: quem criou, editou ou excluiu o quê. Toda mudança em dados
/// sensíveis (hoje, os registros da Rotina Diária) grava uma entrada aqui.
/// </summary>
public class LogAuditoria
{
    public Guid Id { get; set; }

    public string EntidadeTipo { get; set; } = string.Empty;
    public Guid EntidadeId { get; set; }
    public AcaoAuditoria Acao { get; set; }

    /// <summary>Turma dona do registro auditado, só preenchida pra entidades vinculadas a uma turma
    /// (hoje, só RegistroDiarioClasse) — permite consultar "tudo que aconteceu nessa turma nesse dia"
    /// mesmo depois que um registro individual foi excluído e não existe mais pra ancorar um "Ver histórico".</summary>
    public Guid? TurmaId { get; set; }

    /// <summary>Dia (no fuso da escola) a que o registro auditado se refere — não é a data do log em si
    /// (isso já é RegistradoEm), é o dia do RegistroDiarioClasse/RegistroRotina sendo auditado.</summary>
    public DateOnly? Data { get; set; }

    public Guid UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public string? Detalhe { get; set; }
    public DateTime RegistradoEm { get; set; }
}
