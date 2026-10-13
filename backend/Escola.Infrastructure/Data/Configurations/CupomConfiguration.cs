using Escola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Escola.Infrastructure.Data.Configurations;

public class CupomConfiguration : IEntityTypeConfiguration<Cupom>
{
    public void Configure(EntityTypeBuilder<Cupom> builder)
    {
        builder.ToTable("Cupons");
        builder.Property(c => c.Codigo).IsRequired().HasMaxLength(30);
        builder.HasIndex(c => c.Codigo).IsUnique();
        builder.Property(c => c.Observacao).HasMaxLength(300);
    }
}
