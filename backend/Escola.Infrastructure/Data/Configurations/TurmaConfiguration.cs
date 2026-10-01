using Escola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Escola.Infrastructure.Data.Configurations;

public class TurmaConfiguration : IEntityTypeConfiguration<Turma>
{
    public void Configure(EntityTypeBuilder<Turma> builder)
    {
        builder.Property(t => t.Nome).IsRequired().HasMaxLength(100);

        builder.HasOne(t => t.Unidade)
            .WithMany(u => u.Turmas)
            .HasForeignKey(t => t.UnidadeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
