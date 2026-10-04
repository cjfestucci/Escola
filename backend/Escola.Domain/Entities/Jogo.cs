using Escola.Domain.Enums;

namespace Escola.Domain.Entities;

/// <summary>Partida de uma das turmas (categorias) do clube contra um adversário. Só registra o nosso lado:
/// placar é "gols pró × gols contra", e o resultado (V/E/D) é derivado dele, nunca gravado.</summary>
public class Jogo
{
    public Guid Id { get; set; }

    public Guid? CampeonatoId { get; set; }
    public Campeonato? Campeonato { get; set; }

    public Guid TurmaId { get; set; }
    public Turma Turma { get; set; } = null!;

    public string Adversario { get; set; } = string.Empty;
    public DateOnly Data { get; set; }
    public TimeOnly Hora { get; set; }
    public string? Local { get; set; }
    public LocalJogo Mando { get; set; }
    public StatusJogo Status { get; set; }

    /// <summary>Só preenchidos quando <see cref="Status"/> é Realizado.</summary>
    public int? GolsPro { get; set; }
    public int? GolsContra { get; set; }

    public string? Observacao { get; set; }
    public DateTime RegistradoEm { get; set; }

    public ICollection<JogoAtleta> Convocados { get; set; } = new List<JogoAtleta>();
}
