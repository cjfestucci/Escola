namespace Escola.Domain.Entities;

/// <summary>Vínculo entre um aluno e um responsável (um aluno pode ter mais de um responsável).</summary>
public class AlunoResponsavel
{
    public Guid AlunoId { get; set; }
    public Aluno Aluno { get; set; } = null!;

    public Guid ResponsavelId { get; set; }
    public Responsavel Responsavel { get; set; } = null!;

    public bool ResponsavelFinanceiro { get; set; }
    public bool AutorizadoParaBuscar { get; set; } = true;
}
