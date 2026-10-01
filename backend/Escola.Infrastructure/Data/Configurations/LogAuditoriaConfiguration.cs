using Escola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Escola.Infrastructure.Data.Configurations;

public class LogAuditoriaConfiguration : IEntityTypeConfiguration<LogAuditoria>
{
    public void Configure(EntityTypeBuilder<LogAuditoria> builder)
    {
        builder.Property(l => l.EntidadeTipo).IsRequired().HasMaxLength(50);
        builder.Property(l => l.Detalhe).HasMaxLength(1000);

        builder.HasOne(l => l.Usuario)
            .WithMany()
            .HasForeignKey(l => l.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => new { l.EntidadeTipo, l.EntidadeId });
        builder.HasIndex(l => new { l.TurmaId, l.Data });
    }
}
