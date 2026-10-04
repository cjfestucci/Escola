using Escola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Escola.Infrastructure.Data.Configurations;

public class CampeonatoConfiguration : IEntityTypeConfiguration<Campeonato>
{
    public void Configure(EntityTypeBuilder<Campeonato> builder)
    {
        builder.Property(c => c.Nome).IsRequired().HasMaxLength(150);
        builder.Property(c => c.Observacao).HasMaxLength(500);
    }
}

public class JogoConfiguration : IEntityTypeConfiguration<Jogo>
{
    public void Configure(EntityTypeBuilder<Jogo> builder)
    {
        builder.Property(j => j.Adversario).IsRequired().HasMaxLength(150);
        builder.Property(j => j.Local).HasMaxLength(150);
        builder.Property(j => j.Observacao).HasMaxLength(500);

        builder.HasOne(j => j.Campeonato)
            .WithMany(c => c.Jogos)
            .HasForeignKey(j => j.CampeonatoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(j => j.Turma)
            .WithMany()
            .HasForeignKey(j => j.TurmaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(j => j.Data);
    }
}

public class JogoAtletaConfiguration : IEntityTypeConfiguration<JogoAtleta>
{
    public void Configure(EntityTypeBuilder<JogoAtleta> builder)
    {
        builder.HasOne(ja => ja.Jogo)
            .WithMany(j => j.Convocados)
            .HasForeignKey(ja => ja.JogoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ja => ja.Aluno)
            .WithMany()
            .HasForeignKey(ja => ja.AlunoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(ja => new { ja.JogoId, ja.AlunoId }).IsUnique();
    }
}
