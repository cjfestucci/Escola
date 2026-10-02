using Escola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Escola.Infrastructure.Data.Configurations;

public class ProdutoConfiguration : IEntityTypeConfiguration<Produto>
{
    public void Configure(EntityTypeBuilder<Produto> builder)
    {
        builder.Property(p => p.Nome).IsRequired().HasMaxLength(150);
        builder.Property(p => p.Codigo).HasMaxLength(50);
        builder.Property(p => p.UnidadeMedida).IsRequired().HasMaxLength(20);
        builder.Property(p => p.EstoqueMinimo).HasPrecision(12, 3);
    }
}
