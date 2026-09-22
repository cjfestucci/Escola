using Escola.Domain.Enums;

namespace Escola.Domain.Entities;

public class Turma
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public Periodo Periodo { get; set; }
    public TimeOnly HorarioEntrada { get; set; }
    public TimeOnly HorarioSaida { get; set; }

    public ICollection<Aluno> Alunos { get; set; } = new List<Aluno>();

    /// <summary>Educadores vinculados — o formulário de cadastro só define um "professor" opcional,
    /// mas o modelo já suporta mais de um (mesmo vínculo usado por "educador tem várias turmas").</summary>
    public ICollection<TurmaEducador> Educadores { get; set; } = new List<TurmaEducador>();

    public ICollection<RegistroDiarioClasse> RegistrosDiario { get; set; } = new List<RegistroDiarioClasse>();
}
