using Escola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Escola.Infrastructure.Data.Configurations;

public class CobrancaPixConfiguration : IEntityTypeConfiguration<CobrancaPix>
{
    public void Configure(EntityTypeBuilder<CobrancaPix> builder)
    {
        builder.Property(c => c.TxId).IsRequired().HasMaxLength(35);
        builder.Property(c => c.PixCopiaECola).IsRequired().HasMaxLength(1000);
        builder.Property(c => c.EndToEndId).HasMaxLength(64);
        builder.Property(c => c.Valor).HasPrecision(10, 2);
        builder.Property(c => c.ValorRecebido).HasPrecision(10, 2);

        builder.HasOne(c => c.Cobranca).WithMany().HasForeignKey(c => c.CobrancaId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.TxId).IsUnique();
        builder.HasIndex(c => new { c.CobrancaId, c.Status });
        builder.HasIndex(c => c.Status);
    }
}
