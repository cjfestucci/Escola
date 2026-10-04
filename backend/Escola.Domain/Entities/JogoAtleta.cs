namespace Escola.Domain.Entities;

/// <summary>Atleta convocado pra um jogo, com a participação dele na súmula.</summary>
public class JogoAtleta
{
    public Guid Id { get; set; }

    public Guid JogoId { get; set; }
    public Jogo Jogo { get; set; } = null!;

    public Guid AlunoId { get; set; }
    public Aluno Aluno { get; set; } = null!;

    public bool Titular { get; set; }
    public int Gols { get; set; }
    public int CartoesAmarelos { get; set; }
    public bool CartaoVermelho { get; set; }
}
