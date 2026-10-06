using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Escola.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;

namespace Escola.Infrastructure.Data;

/// <summary>Dados de exemplo para desenvolvimento e demonstração, enquanto o módulo de Matrícula não existe.</summary>
public static class DbInitializer
{
    /// <summary>Senha padrão de todas as contas de desenvolvimento (seed e backfill) — nunca usar em produção.</summary>
    public const string SenhaDev = "escola123";

    public static async Task SeedAsync(EscolaDbContext context)
    {
        if (await context.Turmas.AnyAsync())
            return;

        var agora = DateTime.UtcNow;

        var turmaBercario1 = new Turma
        {
            Id = Guid.NewGuid(), Nome = "Berçário 1", Periodo = Periodo.Manha,
            HorarioEntrada = new TimeOnly(7, 0), HorarioSaida = new TimeOnly(12, 0)
        };
        var turmaBercario2 = new Turma
        {
            Id = Guid.NewGuid(), Nome = "Berçário 2", Periodo = Periodo.Tarde,
            HorarioEntrada = new TimeOnly(13, 0), HorarioSaida = new TimeOnly(18, 0)
        };
        var turmaMaternal1 = new Turma
        {
            Id = Guid.NewGuid(), Nome = "Maternal 1", Periodo = Periodo.Manha,
            HorarioEntrada = new TimeOnly(7, 30), HorarioSaida = new TimeOnly(12, 30)
        };
        var turmaJardim1 = new Turma
        {
            Id = Guid.NewGuid(), Nome = "Jardim I", Periodo = Periodo.Integral,
            HorarioEntrada = new TimeOnly(7, 0), HorarioSaida = new TimeOnly(18, 0)
        };
        var turmas = new[] { turmaBercario1, turmaBercario2, turmaMaternal1, turmaJardim1 };

        // Turma exige Unidade (desde 2026-09-24). O seed é anterior a isso: sem esta linha, num banco/cliente novo as turmas iam com
        // UnidadeId vazio e a subida em Development caía na FK (a "Unidade Principal" da migration só existe pro cliente padrão).
        var unidade = await context.Unidades.FirstOrDefaultAsync();
        if (unidade is null)
        {
            unidade = new Unidade { Id = Guid.NewGuid(), Nome = "Unidade Principal" };
            context.Unidades.Add(unidade);
        }
        foreach (var turma in turmas) turma.UnidadeId = unidade.Id;

        var senhaDevHash = SenhaHasher.Hash(SenhaDev);

        var professoraAna = new Usuario
        {
            Id = Guid.NewGuid(), Nome = "Professora Ana", Email = "ana@escola.dev",
            SenhaHash = senhaDevHash, Papel = PapelUsuario.Educador
        };
        var professoraBia = new Usuario
        {
            Id = Guid.NewGuid(), Nome = "Professora Bia", Email = "bia@escola.dev",
            SenhaHash = senhaDevHash, Papel = PapelUsuario.Educador
        };
        var professorCaio = new Usuario
        {
            Id = Guid.NewGuid(), Nome = "Professor Caio", Email = "caio@escola.dev",
            SenhaHash = senhaDevHash, Papel = PapelUsuario.Educador
        };
        var educadores = new[] { professoraAna, professoraBia, professorCaio };

        var admin = new Usuario
        {
            Id = Guid.NewGuid(), Nome = "Admin Geral", Email = "admin@escola.dev",
            SenhaHash = senhaDevHash, Papel = PapelUsuario.Admin
        };
        var coordenadora = new Usuario
        {
            Id = Guid.NewGuid(), Nome = "Coordenadora Marina", Email = "coordenacao@escola.dev",
            SenhaHash = senhaDevHash, Papel = PapelUsuario.Coordenador
        };
        var financeiro = new Usuario
        {
            Id = Guid.NewGuid(), Nome = "Financeiro Tatiane", Email = "financeiro@escola.dev",
            SenhaHash = senhaDevHash, Papel = PapelUsuario.Financeiro
        };

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
        var loginsResponsaveis = new List<Usuario>();

        Usuario LoginDoResponsavel(Responsavel responsavel) => new()
        {
            Id = Guid.NewGuid(), Nome = responsavel.Nome, Email = responsavel.Email,
            SenhaHash = senhaDevHash, Papel = PapelUsuario.Responsavel, ResponsavelId = responsavel.Id
        };

        foreach (var d in dados)
        {
            var aluno = new Aluno { Id = Guid.NewGuid(), Nome = d.Aluno, DataNascimento = d.Nasc, TurmaId = d.Turma.Id };
            alunos.Add(aluno);

            var responsavel1 = new Responsavel { Id = Guid.NewGuid(), Nome = d.Resp1, Email = d.Email1, Telefone = d.Tel1 };
            responsaveis.Add(responsavel1);
            loginsResponsaveis.Add(LoginDoResponsavel(responsavel1));
            vinculos.Add(new AlunoResponsavel { AlunoId = aluno.Id, ResponsavelId = responsavel1.Id, ResponsavelFinanceiro = true });

            if (d.Resp2 is not null)
            {
                var responsavel2 = new Responsavel
                {
                    Id = Guid.NewGuid(), Nome = d.Resp2,
                    Email = $"{d.Resp2.Split(' ')[0].ToLowerInvariant()}@example.com"
                };
                responsaveis.Add(responsavel2);
                loginsResponsaveis.Add(LoginDoResponsavel(responsavel2));
                vinculos.Add(new AlunoResponsavel { AlunoId = aluno.Id, ResponsavelId = responsavel2.Id, ResponsavelFinanceiro = false });
            }
        }

        context.Turmas.AddRange(turmas);
        context.Usuarios.AddRange(educadores);
        context.Usuarios.AddRange(admin, coordenadora, financeiro);
        context.Usuarios.AddRange(loginsResponsaveis);
        context.Alunos.AddRange(alunos);
        context.Responsaveis.AddRange(responsaveis);
        context.AlunoResponsaveis.AddRange(vinculos);

        // Professora Ana fica com duas turmas, pra já nascer demonstrando o vínculo múltiplo.
        context.TurmaEducadores.AddRange(
            new TurmaEducador { TurmaId = turmaBercario1.Id, UsuarioId = professoraAna.Id },
            new TurmaEducador { TurmaId = turmaBercario2.Id, UsuarioId = professoraAna.Id },
            new TurmaEducador { TurmaId = turmaMaternal1.Id, UsuarioId = professoraBia.Id },
            new TurmaEducador { TurmaId = turmaJardim1.Id, UsuarioId = professorCaio.Id });

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

    /// <summary>
    /// Backfill idempotente pra bancos de dev que já existiam antes do login de verdade: garante senha
    /// real nas contas seed antigas, cria o login de cada responsável que ainda não tem um, e garante as
    /// contas de Admin/Coordenador/Financeiro. Roda toda vez (mesmo se SeedAsync não fez nada), sem duplicar.
    /// </summary>
    public static async Task GarantirAcessosAsync(EscolaDbContext context)
    {
        var senhaDevHash = SenhaHasher.Hash(SenhaDev);
        var houveAlteracao = false;

        var usuariosSemSenhaReal = await context.Usuarios
            .Where(u => u.SenhaHash == "seed-sem-senha")
            .ToListAsync();
        foreach (var usuario in usuariosSemSenhaReal)
        {
            usuario.SenhaHash = senhaDevHash;
            houveAlteracao = true;
        }

        var responsaveisSemLogin = await context.Responsaveis
            .Where(r => !context.Usuarios.Any(u => u.ResponsavelId == r.Id))
            .ToListAsync();
        foreach (var responsavel in responsaveisSemLogin)
        {
            // E-mail pode já estar em uso por uma conta de equipe (ex.: alguém que também é responsável) —
            // nesse caso não cria um segundo login, só deixa sem vínculo mesmo.
            if (await context.Usuarios.AnyAsync(u => u.Email.ToLower() == responsavel.Email.ToLower()))
                continue;

            context.Usuarios.Add(new Usuario
            {
                Id = Guid.NewGuid(), Nome = responsavel.Nome, Email = responsavel.Email,
                SenhaHash = senhaDevHash, Papel = PapelUsuario.Responsavel, ResponsavelId = responsavel.Id
            });
            houveAlteracao = true;
        }

        var contasEquipe = new (string Nome, string Email, PapelUsuario Papel)[]
        {
            ("Admin Geral", "admin@escola.dev", PapelUsuario.Admin),
            ("Coordenadora Marina", "coordenacao@escola.dev", PapelUsuario.Coordenador),
            ("Financeiro Tatiane", "financeiro@escola.dev", PapelUsuario.Financeiro)
        };
        foreach (var conta in contasEquipe)
        {
            if (await context.Usuarios.AnyAsync(u => u.Email.ToLower() == conta.Email))
                continue;

            context.Usuarios.Add(new Usuario
            {
                Id = Guid.NewGuid(), Nome = conta.Nome, Email = conta.Email,
                SenhaHash = senhaDevHash, Papel = conta.Papel
            });
            houveAlteracao = true;
        }

        if (houveAlteracao)
            await context.SaveChangesAsync();
    }
}
