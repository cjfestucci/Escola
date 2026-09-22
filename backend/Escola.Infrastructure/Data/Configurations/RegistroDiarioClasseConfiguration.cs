using Escola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Escola.Infrastructure.Data.Configurations;

public class RegistroDiarioClasseConfiguration : IEntityTypeConfiguration<RegistroDiarioClasse>
{
    public void Configure(EntityTypeBuilder<RegistroDiarioClasse> builder)
    {
        builder.Property(r => r.Titulo).IsRequired().HasMaxLength(200);
        builder.Property(r => r.Descricao).HasMaxLength(2000);

        builder.HasOne(r => r.Turma)
            .WithMany(t => t.RegistrosDiario)
            .HasForeignKey(r => r.TurmaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.CriadoPor)
            .WithMany()
            .HasForeignKey(r => r.CriadoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => new { r.TurmaId, r.RegistradoEm });
    }
}
