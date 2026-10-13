using Escola.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Escola.Tests;

/// <summary>Vários clientes dividem o mesmo banco: vazar dado de um pro outro é o pior bug possível deste sistema.</summary>
public class IsolamentoPorClienteTests
{
    [Fact]
    public async Task SaveChangesPreencheOClienteDoContextoNaEntidadeNova()
    {
        using var banco = new BancoDeTeste();
        using (var db = banco.Contexto(BancoDeTeste.ClienteA))
        {
            db.Fornecedores.Add(new Fornecedor { Id = Guid.NewGuid(), Nome = "Do A" });
            await db.SaveChangesAsync();
        }

        using var leitura = banco.Contexto(BancoDeTeste.ClienteA);
        var fornecedor = await leitura.Fornecedores.SingleAsync();
        Assert.Equal(BancoDeTeste.ClienteA, leitura.Entry(fornecedor).Property<Guid>("ClienteId").CurrentValue);
    }

    [Fact]
    public async Task UmClienteNaoEnxergaOsDadosDoOutro()
    {
        using var banco = new BancoDeTeste();
        using (var a = banco.Contexto(BancoDeTeste.ClienteA))
        {
            a.Fornecedores.Add(new Fornecedor { Id = Guid.NewGuid(), Nome = "Fornecedor do A" });
            await a.SaveChangesAsync();
        }
        using (var b = banco.Contexto(BancoDeTeste.ClienteB))
        {
            b.Fornecedores.Add(new Fornecedor { Id = Guid.NewGuid(), Nome = "Fornecedor do B" });
            await b.SaveChangesAsync();
        }

        using var lerA = banco.Contexto(BancoDeTeste.ClienteA);
        using var lerB = banco.Contexto(BancoDeTeste.ClienteB);
        Assert.Equal(["Fornecedor do A"], await lerA.Fornecedores.Select(f => f.Nome).ToListAsync());
        Assert.Equal(["Fornecedor do B"], await lerB.Fornecedores.Select(f => f.Nome).ToListAsync());
    }

    [Fact]
    public async Task BuscarPorIdDeOutroClienteNaoEncontra()
    {
        using var banco = new BancoDeTeste();
        var id = Guid.NewGuid();
        using (var a = banco.Contexto(BancoDeTeste.ClienteA))
        {
            a.Fornecedores.Add(new Fornecedor { Id = id, Nome = "Secreto" });
            await a.SaveChangesAsync();
        }

        using var b = banco.Contexto(BancoDeTeste.ClienteB);
        Assert.Null(await b.Fornecedores.FirstOrDefaultAsync(f => f.Id == id));
        Assert.Equal(0, await b.Fornecedores.CountAsync());
    }

    [Fact]
    public async Task MesmoEmailPodeExistirEmDoisClientes()
    {
        using var banco = new BancoDeTeste();
        foreach (var cliente in new[] { BancoDeTeste.ClienteA, BancoDeTeste.ClienteB })
        {
            using var db = banco.Contexto(cliente);
            db.Usuarios.Add(new Usuario { Id = Guid.NewGuid(), Nome = "Maria", Email = "maria@escola.com", SenhaHash = "x" });
            await db.SaveChangesAsync(); // o índice único é (ClienteId, Email): não pode estourar
        }

        using var a = banco.Contexto(BancoDeTeste.ClienteA);
        Assert.Equal(1, await a.Usuarios.CountAsync());
    }

    [Fact]
    public async Task IgnoreQueryFiltersEnxergaTudoEPorIssoSoDeveSerUsadoDePropositoCom()
    {
        using var banco = new BancoDeTeste();
        foreach (var cliente in new[] { BancoDeTeste.ClienteA, BancoDeTeste.ClienteB })
        {
            using var db = banco.Contexto(cliente);
            db.Fornecedores.Add(new Fornecedor { Id = Guid.NewGuid(), Nome = "F" });
            await db.SaveChangesAsync();
        }

        using var leitura = banco.Contexto(BancoDeTeste.ClienteA);
        Assert.Equal(1, await leitura.Fornecedores.CountAsync());
        Assert.Equal(2, await leitura.Fornecedores.IgnoreQueryFilters().CountAsync());
    }

    [Fact]
    public void TodaEntidadeDeDadosTemClienteIdENenhumaEsqueceuDoFiltro()
    {
        using var banco = new BancoDeTeste();
        using var db = banco.Contexto(BancoDeTeste.ClienteA);

        var semIsolamento = db.Model.GetEntityTypes()
            .Where(t => t.ClrType != typeof(Cliente) && t.BaseType is null && !typeof(IEntidadePlataforma).IsAssignableFrom(t.ClrType))
            .Where(t => t.FindProperty("ClienteId") is null || t.GetQueryFilter() is null)
            .Select(t => t.ClrType.Name)
            .ToList();

        Assert.True(semIsolamento.Count == 0, "Entidades sem ClienteId/filtro: " + string.Join(", ", semIsolamento));
    }

    /// <summary>A exceção de plataforma (sem cliente) é uma lista fechada: entidade nova marcada com IEntidadePlataforma tem que entrar
    /// aqui de propósito — dado de escola, aluno ou família nunca pode ficar fora do isolamento.</summary>
    [Fact]
    public void SoOsCuponsSaoDaPlataforma()
    {
        using var banco = new BancoDeTeste();
        using var db = banco.Contexto(BancoDeTeste.ClienteA);

        var daPlataforma = db.Model.GetEntityTypes()
            .Where(t => typeof(IEntidadePlataforma).IsAssignableFrom(t.ClrType))
            .Select(t => t.ClrType.Name)
            .ToList();

        Assert.Equal([nameof(Cupom)], daPlataforma);
        Assert.Null(db.Model.FindEntityType(typeof(Cupom))!.FindProperty("ClienteId"));
    }
}
