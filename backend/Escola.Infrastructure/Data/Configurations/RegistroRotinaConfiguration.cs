using Escola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Escola.Infrastructure.Data.Configurations;

public class RegistroRotinaConfiguration : IEntityTypeConfiguration<RegistroRotina>
{
    public void Configure(EntityTypeBuilder<RegistroRotina> builder)
    {
        builder.Property(r => r.Observacao).HasMaxLength(1000);

        builder.HasOne(r => r.Aluno)
            .WithMany(a => a.RegistrosRotina)
            .HasForeignKey(r => r.AlunoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.CriadoPor)
            .WithMany()
            .HasForeignKey(r => r.CriadoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => new { r.AlunoId, r.RegistradoEm });
    }
}
