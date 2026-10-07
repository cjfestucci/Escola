using System.Linq.Expressions;
using Escola.Domain.Entities;
using Escola.Infrastructure.Clientes;
using Microsoft.EntityFrameworkCore;

namespace Escola.Infrastructure.Data;

public class EscolaDbContext(DbContextOptions<EscolaDbContext> options, IClienteAtual clienteAtual) : DbContext(options)
{
    /// <summary>Nome da coluna/propriedade-sombra do cliente em toda tabela de dados (pra consultas que precisam dela, ex.: login).</summary>
    public const string ColunaCliente = "ClienteId";

    /// <summary>Lido pelo filtro de consulta de toda entidade (EF o reavalia por instância de contexto).</summary>
    public Guid ClienteIdAtual => clienteAtual.Id;

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Turma> Turmas => Set<Turma>();
    public DbSet<Unidade> Unidades => Set<Unidade>();
    public DbSet<Aluno> Alunos => Set<Aluno>();
    public DbSet<FichaSaude> FichasSaude => Set<FichaSaude>();
    public DbSet<DocumentoSaude> DocumentosSaude => Set<DocumentoSaude>();
    public DbSet<Responsavel> Responsaveis => Set<Responsavel>();
    public DbSet<AlunoResponsavel> AlunoResponsaveis => Set<AlunoResponsavel>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<TurmaEducador> TurmaEducadores => Set<TurmaEducador>();
    public DbSet<RegistroRotina> RegistrosRotina => Set<RegistroRotina>();
    public DbSet<FotoRegistro> Fotos => Set<FotoRegistro>();
    public DbSet<RegistroDiarioClasse> RegistrosDiarioClasse => Set<RegistroDiarioClasse>();
    public DbSet<FotoDiarioClasse> FotosDiarioClasse => Set<FotoDiarioClasse>();
    public DbSet<Cobranca> Cobrancas => Set<Cobranca>();
    public DbSet<Fornecedor> Fornecedores => Set<Fornecedor>();
    public DbSet<ContaPagar> ContasPagar => Set<ContaPagar>();
    public DbSet<ContaReceber> ContasReceber => Set<ContaReceber>();
    public DbSet<Campeonato> Campeonatos => Set<Campeonato>();
    public DbSet<Jogo> Jogos => Set<Jogo>();
    public DbSet<JogoAtleta> JogoAtletas => Set<JogoAtleta>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<MovimentacaoEstoque> MovimentacoesEstoque => Set<MovimentacaoEstoque>();
    public DbSet<RegistroPresenca> RegistrosPresenca => Set<RegistroPresenca>();
    public DbSet<RedefinicaoSenha> RedefinicoesSenha => Set<RedefinicaoSenha>();
    public DbSet<TermoAceite> TermosAceite => Set<TermoAceite>();
    public DbSet<CobrancaPix> CobrancasPix => Set<CobrancaPix>();
    public DbSet<ConfiguracaoFinanceira> ConfiguracoesFinanceiras => Set<ConfiguracaoFinanceira>();
    public DbSet<ConfiguracaoEscola> ConfiguracoesEscola => Set<ConfiguracaoEscola>();
    public DbSet<LogAuditoria> LogsAuditoria => Set<LogAuditoria>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        AplicarIsolamentoPorCliente(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EscolaDbContext).Assembly);

        modelBuilder.Entity<RegistroRotina>()
            .HasDiscriminator<string>("Categoria")
            .HasValue<RegistroAlimentacao>("Alimentacao")
            .HasValue<RegistroSono>("Sono")
            .HasValue<RegistroHigiene>("Higiene")
            .HasValue<RegistroHumor>("Humor")
            .HasValue<RegistroMomento>("Momento");
    }

    /// <summary>Toda entidade (menos o próprio Cliente) ganha a propriedade oculta <c>ClienteId</c> — FK pra Clientes,
    /// indexada e usada num filtro global de consulta — pra que nenhum cliente enxergue dado de outro. Em hierarquias
    /// (TPH) só a raiz recebe. Roda ANTES das configurações por entidade pra elas poderem referenciar a coluna
    /// (ex.: índices únicos de e-mail por cliente).</summary>
    private void AplicarIsolamentoPorCliente(ModelBuilder modelBuilder)
    {
        var raizes = modelBuilder.Model.GetEntityTypes()
            .Where(e => e.BaseType is null && e.ClrType != typeof(Cliente) && !e.IsOwned())
            .Select(e => e.ClrType)
            .ToList();

        foreach (var tipo in raizes)
        {
            var entidade = modelBuilder.Entity(tipo);
            entidade.Property<Guid>(ColunaCliente);
            entidade.HasIndex(ColunaCliente);
            entidade.HasOne(typeof(Cliente)).WithMany().HasForeignKey(ColunaCliente).OnDelete(DeleteBehavior.Restrict);

            var parametro = Expression.Parameter(tipo, "e");
            var coluna = Expression.Call(typeof(EF), nameof(EF.Property), [typeof(Guid)], parametro, Expression.Constant(ColunaCliente));
            var igual = Expression.Equal(coluna, Expression.Property(Expression.Constant(this), nameof(ClienteIdAtual)));
            entidade.HasQueryFilter(Expression.Lambda(igual, parametro));
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        PreencherCliente();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        PreencherCliente();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>Todo registro novo nasce no cliente desta instalação — os controllers não precisam (nem podem) escolher.</summary>
    private void PreencherCliente()
    {
        foreach (var entrada in ChangeTracker.Entries().Where(e => e.State == EntityState.Added))
        {
            var propriedade = entrada.Metadata.FindProperty(ColunaCliente);
            if (propriedade is not null) entrada.Property(ColunaCliente).CurrentValue = ClienteIdAtual;
        }
    }
}
