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

    /// <summary>Desconto fixo (0–100%) aplicado à mensalidade na geração em lote — bolsa, irmãos, etc.</summary>
    public decimal DescontoMensalidadePercentual { get; set; }
    public string? MotivoDesconto { get; set; }

    /// <summary>Quando um responsável aceitou o termo de matrícula no portal — só a partir daí a matrícula vale. Nulo = aguardando o
    /// aceite (não entra na geração de mensalidades em lote). As matrículas anteriores ao termo foram marcadas como confirmadas na migration.</summary>
    public DateTime? MatriculaConfirmadaEm { get; set; }

    public Guid TurmaId { get; set; }
    public Turma Turma { get; set; } = null!;

    public ICollection<AlunoResponsavel> Responsaveis { get; set; } = new List<AlunoResponsavel>();
    public ICollection<RegistroRotina> RegistrosRotina { get; set; } = new List<RegistroRotina>();
    public ICollection<Cobranca> Cobrancas { get; set; } = new List<Cobranca>();
}
