using Escola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Escola.Infrastructure.Data.Configurations;

public class TurmaEducadorConfiguration : IEntityTypeConfiguration<TurmaEducador>
{
    public void Configure(EntityTypeBuilder<TurmaEducador> builder)
    {
        builder.HasKey(te => new { te.TurmaId, te.UsuarioId });

        builder.HasOne(te => te.Turma)
            .WithMany(t => t.Educadores)
            .HasForeignKey(te => te.TurmaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(te => te.Usuario)
            .WithMany(u => u.Turmas)
            .HasForeignKey(te => te.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
