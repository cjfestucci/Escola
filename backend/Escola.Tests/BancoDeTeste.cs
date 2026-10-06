using Escola.Domain.Entities;
using Escola.Infrastructure.Clientes;
using Escola.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Escola.Tests;

/// <summary>Banco SQLite em memória com o modelo real do app (filtro por cliente incluso). Cada instância é um banco novo;
/// vários <see cref="EscolaDbContext"/> (um por cliente) compartilham a mesma conexão, como os deploys compartilham o banco.</summary>
public sealed class BancoDeTeste : IDisposable
{
    public static readonly Guid ClienteA = Guid.Parse("00000000-0000-0000-0000-00000000000a");
    public static readonly Guid ClienteB = Guid.Parse("00000000-0000-0000-0000-00000000000b");

    private readonly SqliteConnection _conexao = new("DataSource=:memory:");
    private readonly DbContextOptions<EscolaDbContext> _opcoes;

    public BancoDeTeste()
    {
        _conexao.Open();
        _opcoes = new DbContextOptionsBuilder<EscolaDbContext>()
            .UseSqlite(_conexao)
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning))
            .Options;

        using var db = Contexto(ClienteA);
        db.Database.EnsureCreated();
        db.Clientes.AddRange(
            new Cliente { Id = ClienteA, Nome = "Cliente A", CriadoEm = DateTime.UtcNow },
            new Cliente { Id = ClienteB, Nome = "Cliente B", CriadoEm = DateTime.UtcNow });
        db.SaveChanges();
    }

    public EscolaDbContext Contexto(Guid clienteId) => new(_opcoes, new ClienteAtual(clienteId));

    /// <summary>Unidade + turma ativas pro cliente do contexto (todo aluno precisa de turma, e toda turma de unidade).</summary>
    public static Turma NovaTurma(EscolaDbContext db, string nome = "Sub-11")
    {
        var unidade = new Unidade { Id = Guid.NewGuid(), Nome = "Principal" };
        var turma = new Turma { Id = Guid.NewGuid(), Nome = nome, UnidadeId = unidade.Id, Unidade = unidade };
        db.Turmas.Add(turma);
        return turma;
    }

    public void Dispose() => _conexao.Dispose();
}
