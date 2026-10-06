namespace Escola.Domain.Entities;

/// <summary>Registro de saúde do aluno — 1:1 com Aluno, criado sob demanda (nem todo aluno tem um ainda).</summary>
public class FichaSaude
{
    public Guid Id { get; set; }

    public Guid AlunoId { get; set; }
    public Aluno Aluno { get; set; } = null!;

    public string? TipoSanguineo { get; set; }
    public string? Alergias { get; set; }
    public string? RestricoesAlimentares { get; set; }
    public string? MedicamentosEmUso { get; set; }
    public string? CondicoesSaude { get; set; }
    public string? PlanoSaude { get; set; }
    public string? PediatraNome { get; set; }
    public string? PediatraTelefone { get; set; }
    public string? ContatoEmergenciaNome { get; set; }
    public string? ContatoEmergenciaTelefone { get; set; }
    public bool VacinacaoEmDia { get; set; }
    public bool AutorizaUsoImagem { get; set; }

    /// <summary>Último dia de validade do atestado médico (aptidão para atividade física). Nulo = não informado. "Vencido"/"vence em
    /// breve" é sempre derivado desta data contra o "hoje" da escola — nunca gravado.</summary>
    public DateOnly? AtestadoValidoAte { get; set; }

    public DateTime AtualizadoEm { get; set; }
}
