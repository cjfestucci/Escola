using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Escola.Infrastructure.Data;

/// <summary>Dados de exemplo para desenvolvimento e demonstração, enquanto o módulo de Matrícula não existe.</summary>
public static class DbInitializer
{
    public static async Task SeedAsync(EscolaDbContext context)
    {
        if (await context.Turmas.AnyAsync())
            return;

        var agora = DateTime.UtcNow;

        var turmaBercario1 = new Turma { Id = Guid.NewGuid(), Nome = "Berçário 1" };
        var turmaBercario2 = new Turma { Id = Guid.NewGuid(), Nome = "Berçário 2" };
        var turmaMaternal1 = new Turma { Id = Guid.NewGuid(), Nome = "Maternal 1" };
        var turmaJardim1 = new Turma { Id = Guid.NewGuid(), Nome = "Jardim I" };
        var turmas = new[] { turmaBercario1, turmaBercario2, turmaMaternal1, turmaJardim1 };

        var professoraAna = new Usuario
        {
            Id = Guid.NewGuid(), Nome = "Professora Ana", Email = "ana@escola.dev",
            SenhaHash = "seed-sem-senha", Papel = PapelUsuario.Educador
        };
        var professoraBia = new Usuario
        {
            Id = Guid.NewGuid(), Nome = "Professora Bia", Email = "bia@escola.dev",
            SenhaHash = "seed-sem-senha", Papel = PapelUsuario.Educador
        };
        var professorCaio = new Usuario
        {
            Id = Guid.NewGuid(), Nome = "Professor Caio", Email = "caio@escola.dev",
            SenhaHash = "seed-sem-senha", Papel = PapelUsuario.Educador
        };
        var educadores = new[] { professoraAna, professoraBia, professorCaio };

        // (nome do aluno, data de nascimento, turma, educador responsável pelos registros,
        //  nome do responsável, e-mail do responsável, telefone, segundo responsável opcional)
        var dados = new (string Aluno, DateOnly Nasc, Turma Turma, Usuario Educador, string Resp1, string Email1, string Tel1, string? Resp2)[]
        {
            ("Lucas Souza", new DateOnly(2024, 3, 10), turmaBercario1, professoraAna, "Carla Souza", "carla@example.com", "11999990000", null),
            ("Maria Eduarda", new DateOnly(2023, 11, 2), turmaBercario1, professoraAna, "Roberto Lima", "roberto.lima@example.com", "11988880001", null),
            ("Pedro Henrique", new DateOnly(2024, 1, 20), turmaBercario2, professoraAna, "Fernanda Alves", "fernanda.alves@example.com", "11988880002", null),
            ("Sofia Martins", new DateOnly(2023, 9, 15), turmaBercario2, professoraAna, "Diego Martins", "diego.martins@example.com", "11988880003", null),
            ("Miguel Costa", new DateOnly(2022, 6, 5), turmaMaternal1, professoraBia, "Juliana Costa", "juliana.costa@example.com", "11988880004", null),
            ("Alice Ferreira", new DateOnly(2022, 8, 22), turmaMaternal1, professoraBia, "Bruno Ferreira", "bruno.ferreira@example.com", "11988880005", "Patrícia Ferreira"),
            ("Arthur Santos", new DateOnly(2022, 4, 30), turmaMaternal1, professoraBia, "Camila Santos", "camila.santos@example.com", "11988880006", null),
            ("Laura Oliveira", new DateOnly(2021, 5, 18), turmaJardim1, professorCaio, "Rafael Oliveira", "rafael.oliveira@example.com", "11988880007", null),
            ("Heitor Almeida", new DateOnly(2021, 2, 9), turmaJardim1, professorCaio, "Vanessa Almeida", "vanessa.almeida@example.com", "11988880008", null),
            ("Valentina Rocha", new DateOnly(2021, 7, 27), turmaJardim1, professorCaio, "Marcos Rocha", "marcos.rocha@example.com", "11988880009", null)
        };

        var alunos = new List<Aluno>();
        var responsaveis = new List<Responsavel>();
        var vinculos = new List<AlunoResponsavel>();

        foreach (var d in dados)
        {
            var aluno = new Aluno { Id = Guid.NewGuid(), Nome = d.Aluno, DataNascimento = d.Nasc, TurmaId = d.Turma.Id };
            alunos.Add(aluno);

            var responsavel1 = new Responsavel { Id = Guid.NewGuid(), Nome = d.Resp1, Email = d.Email1, Telefone = d.Tel1 };
            responsaveis.Add(responsavel1);
            vinculos.Add(new AlunoResponsavel { AlunoId = aluno.Id, ResponsavelId = responsavel1.Id, ResponsavelFinanceiro = true });

            if (d.Resp2 is not null)
            {
                var responsavel2 = new Responsavel
                {
                    Id = Guid.NewGuid(), Nome = d.Resp2,
                    Email = $"{d.Resp2.Split(' ')[0].ToLowerInvariant()}@example.com"
                };
                responsaveis.Add(responsavel2);
                vinculos.Add(new AlunoResponsavel { AlunoId = aluno.Id, ResponsavelId = responsavel2.Id, ResponsavelFinanceiro = false });
            }
        }

        context.Turmas.AddRange(turmas);
        context.Usuarios.AddRange(educadores);
        context.Alunos.AddRange(alunos);
        context.Responsaveis.AddRange(responsaveis);
        context.AlunoResponsaveis.AddRange(vinculos);

        Aluno AlunoPor(string nome) => alunos.Single(a => a.Nome == nome);
        Usuario EducadorDe(string nomeAluno) => dados.Single(d => d.Aluno == nomeAluno).Educador;

        RegistroRotina ComBase(RegistroRotina r, string nomeAluno, int minutosAtras)
        {
            r.Id = Guid.NewGuid();
            r.AlunoId = AlunoPor(nomeAluno).Id;
            r.CriadoPorUsuarioId = EducadorDe(nomeAluno).Id;
            r.RegistradoEm = agora.AddMinutes(-minutosAtras);
            return r;
        }

        var registros = new List<RegistroRotina>
        {
            ComBase(new RegistroAlimentacao { Refeicao = Refeicao.Cafe, Status = StatusAlimentacao.ComeuTudo }, "Lucas Souza", 95),
            ComBase(new RegistroSono { HoraInicio = new TimeOnly(9, 10), HoraFim = new TimeOnly(10, 20) }, "Lucas Souza", 70),
            ComBase(new RegistroHumor { Humor = Humor.Feliz, Observacao = "Bem disposto hoje" }, "Lucas Souza", 20),

            ComBase(new RegistroAlimentacao { Refeicao = Refeicao.Cafe, Status = StatusAlimentacao.Parcial }, "Maria Eduarda", 90),
            ComBase(new RegistroHigiene { Tipo = TipoHigiene.TrocaFralda }, "Maria Eduarda", 45),
            ComBase(new RegistroHumor { Humor = Humor.Sonolento }, "Maria Eduarda", 15),

            ComBase(new RegistroSono { HoraInicio = new TimeOnly(8, 30), HoraFim = new TimeOnly(9, 45) }, "Pedro Henrique", 60),
            ComBase(new RegistroAlimentacao { Refeicao = Refeicao.Almoco, Status = StatusAlimentacao.ComeuTudo }, "Pedro Henrique", 10),

            ComBase(new RegistroHigiene { Tipo = TipoHigiene.TrocaFralda }, "Sofia Martins", 80),
            ComBase(new RegistroHumor { Humor = Humor.Agitado, Observacao = "Bastante animada na atividade em grupo" }, "Sofia Martins", 25),

            ComBase(new RegistroAlimentacao { Refeicao = Refeicao.Almoco, Status = StatusAlimentacao.ComeuTudo }, "Miguel Costa", 30),
            ComBase(new RegistroMomento { Observacao = "Desenho livre da manhã" }, "Miguel Costa", 12),

            ComBase(new RegistroHigiene { Tipo = TipoHigiene.Banheiro }, "Alice Ferreira", 50),
            ComBase(new RegistroHumor { Humor = Humor.Feliz }, "Alice Ferreira", 18),

            ComBase(new RegistroAlimentacao { Refeicao = Refeicao.Almoco, Status = StatusAlimentacao.Recusou, Observacao = "Não quis experimentar o prato de hoje" }, "Arthur Santos", 22),

            ComBase(new RegistroMomento { Observacao = "Roda de leitura" }, "Laura Oliveira", 40),
            ComBase(new RegistroHumor { Humor = Humor.Feliz }, "Laura Oliveira", 8),

            ComBase(new RegistroAlimentacao { Refeicao = Refeicao.Lanche, Status = StatusAlimentacao.ComeuTudo }, "Heitor Almeida", 5),

            ComBase(new RegistroHumor { Humor = Humor.Choroso, Observacao = "Sentiu saudade da família de manhã, melhorou após o lanche" }, "Valentina Rocha", 35)
        };

        context.RegistrosRotina.AddRange(registros);

        await context.SaveChangesAsync();
    }
}
