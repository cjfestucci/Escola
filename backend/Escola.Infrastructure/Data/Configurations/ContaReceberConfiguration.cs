using Escola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Escola.Infrastructure.Data.Configurations;

public class ContaReceberConfiguration : IEntityTypeConfiguration<ContaReceber>
{
    public void Configure(EntityTypeBuilder<ContaReceber> builder)
    {
        builder.Property(c => c.Descricao).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Origem).HasMaxLength(150);
        builder.Property(c => c.Valor).HasPrecision(10, 2);

        builder.HasIndex(c => c.Vencimento);
    }
}
