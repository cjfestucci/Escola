namespace Escola.Domain.Entities;

/// <summary>Competição da qual o clube participa. Um jogo pode (ou não — amistoso) pertencer a um campeonato.</summary>
public class Campeonato
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public DateOnly DataInicio { get; set; }
    public DateOnly? DataFim { get; set; }
    public string? Observacao { get; set; }

    /// <summary>Quantos cartões amarelos acumulados geram suspensão. Nulo = sem controle disciplinar
    /// (nem amarelos nem vermelhos geram alerta nesse campeonato).</summary>
    public int? AmarelosParaSuspensao { get; set; }

    public bool Ativo { get; set; } = true;
    public DateTime RegistradoEm { get; set; }

    public ICollection<Jogo> Jogos { get; set; } = new List<Jogo>();
}
