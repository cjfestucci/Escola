namespace Escola.Domain.Entities;

/// <summary>Aceite do termo de matrícula e de tratamento de dados (LGPD) por um responsável, para um aluno. Guarda o <b>texto exato</b>
/// que a pessoa viu (além da versão), quando, de qual IP e navegador — é a prova do consentimento. Nunca é editado nem excluído.</summary>
public class TermoAceite
{
    public Guid Id { get; set; }

    public Guid AlunoId { get; set; }
    public Aluno Aluno { get; set; } = null!;

    public Guid ResponsavelId { get; set; }
    public Responsavel Responsavel { get; set; } = null!;

    /// <summary>A conta (login) que clicou em "aceito".</summary>
    public Guid UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public string Versao { get; set; } = string.Empty;
    public string TextoAceito { get; set; } = string.Empty;
    public DateTime AceitoEm { get; set; }
    public string? Ip { get; set; }
    public string? NavegadorUserAgent { get; set; }
}
