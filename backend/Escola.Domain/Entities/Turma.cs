using Escola.Domain.Enums;

namespace Escola.Domain.Entities;

public class Turma
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public Periodo Periodo { get; set; }
    public TimeOnly HorarioEntrada { get; set; }
    public TimeOnly HorarioSaida { get; set; }
    public bool Ativa { get; set; } = true;

    /// <summary>Valor mensal cobrado de cada aluno da turma (antes do desconto do aluno). Nulo = a turma não entra
    /// na geração de mensalidades em lote.</summary>
    public decimal? ValorMensalidade { get; set; }

    public Guid UnidadeId { get; set; }
    public Unidade Unidade { get; set; } = null!;

    public ICollection<Aluno> Alunos { get; set; } = new List<Aluno>();

    /// <summary>Educadores vinculados — o formulário de cadastro só define um "professor" opcional,
    /// mas o modelo já suporta mais de um (mesmo vínculo usado por "educador tem várias turmas").</summary>
    public ICollection<TurmaEducador> Educadores { get; set; } = new List<TurmaEducador>();

    public ICollection<RegistroDiarioClasse> RegistrosDiario { get; set; } = new List<RegistroDiarioClasse>();
}
