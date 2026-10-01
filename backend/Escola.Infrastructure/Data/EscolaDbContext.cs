using Escola.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Escola.Infrastructure.Data;

public class EscolaDbContext(DbContextOptions<EscolaDbContext> options) : DbContext(options)
{
    public DbSet<Turma> Turmas => Set<Turma>();
    public DbSet<Unidade> Unidades => Set<Unidade>();
    public DbSet<Aluno> Alunos => Set<Aluno>();
    public DbSet<FichaSaude> FichasSaude => Set<FichaSaude>();
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
    public DbSet<ConfiguracaoFinanceira> ConfiguracoesFinanceiras => Set<ConfiguracaoFinanceira>();
    public DbSet<ConfiguracaoEscola> ConfiguracoesEscola => Set<ConfiguracaoEscola>();
    public DbSet<LogAuditoria> LogsAuditoria => Set<LogAuditoria>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EscolaDbContext).Assembly);

        modelBuilder.Entity<RegistroRotina>()
            .HasDiscriminator<string>("Categoria")
            .HasValue<RegistroAlimentacao>("Alimentacao")
            .HasValue<RegistroSono>("Sono")
            .HasValue<RegistroHigiene>("Higiene")
            .HasValue<RegistroHumor>("Humor")
            .HasValue<RegistroMomento>("Momento");
    }
}
