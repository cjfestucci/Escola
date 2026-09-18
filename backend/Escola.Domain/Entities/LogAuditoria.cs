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

    public Guid UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public string? Detalhe { get; set; }
    public DateTime RegistradoEm { get; set; }
}
