using Escola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Escola.Infrastructure.Data.Configurations;

public class CobrancaConfiguration : IEntityTypeConfiguration<Cobranca>
{
    public void Configure(EntityTypeBuilder<Cobranca> builder)
    {
        builder.Property(c => c.Descricao).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Valor).HasPrecision(10, 2);

        builder.HasOne(c => c.Aluno)
            .WithMany(a => a.Cobrancas)
            .HasForeignKey(c => c.AlunoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => new { c.AlunoId, c.Vencimento });
    }
}
