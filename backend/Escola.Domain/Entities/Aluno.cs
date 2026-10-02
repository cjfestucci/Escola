using Escola.Domain.Enums;

namespace Escola.Domain.Entities;

public class Aluno
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public DateOnly DataNascimento { get; set; }
    public string? FotoUrl { get; set; }
    public bool Ativo { get; set; } = true;

    /// <summary>Só faz sentido no segmento clube (escola de futebol); fica nulo nos demais.</summary>
    public PosicaoAtleta? Posicao { get; set; }

    public Guid TurmaId { get; set; }
    public Turma Turma { get; set; } = null!;

    public ICollection<AlunoResponsavel> Responsaveis { get; set; } = new List<AlunoResponsavel>();
    public ICollection<RegistroRotina> RegistrosRotina { get; set; } = new List<RegistroRotina>();
    public ICollection<Cobranca> Cobrancas { get; set; } = new List<Cobranca>();
}
