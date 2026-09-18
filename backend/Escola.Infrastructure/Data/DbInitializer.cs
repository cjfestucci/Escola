using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Escola.Infrastructure.Data;

/// <summary>Dados de exemplo para desenvolvimento local, enquanto o módulo de Matrícula não existe.</summary>
public static class DbInitializer
{
    public static async Task SeedAsync(EscolaDbContext context)
    {
        if (await context.Turmas.AnyAsync())
            return;

        var turma = new Turma { Id = Guid.NewGuid(), Nome = "Berçário 1" };

        var educadora = new Usuario
        {
            Id = Guid.NewGuid(),
            Nome = "Professora Ana",
            Email = "ana@escola.dev",
            SenhaHash = "seed-sem-senha",
            Papel = PapelUsuario.Educador
        };

        var responsavel = new Responsavel
        {
            Id = Guid.NewGuid(),
            Nome = "Carla Souza",
            Email = "carla@example.com",
            Telefone = "11999990000"
        };

        var aluno1 = new Aluno
        {
            Id = Guid.NewGuid(),
            Nome = "Lucas Souza",
            DataNascimento = new DateOnly(2024, 3, 10),
            TurmaId = turma.Id
        };

        var aluno2 = new Aluno
        {
            Id = Guid.NewGuid(),
            Nome = "Maria Eduarda",
            DataNascimento = new DateOnly(2023, 11, 2),
            TurmaId = turma.Id
        };

        context.Turmas.Add(turma);
        context.Usuarios.Add(educadora);
        context.Responsaveis.Add(responsavel);
        context.Alunos.AddRange(aluno1, aluno2);
        context.AlunoResponsaveis.Add(new AlunoResponsavel
        {
            AlunoId = aluno1.Id,
            ResponsavelId = responsavel.Id,
            ResponsavelFinanceiro = true
        });

        await context.SaveChangesAsync();
    }
}
