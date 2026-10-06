using Escola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Escola.Infrastructure.Data.Configurations;

public class ConfiguracaoFinanceiraConfiguration : IEntityTypeConfiguration<ConfiguracaoFinanceira>
{
    public void Configure(EntityTypeBuilder<ConfiguracaoFinanceira> builder)
    {
        builder.Property(c => c.PixChave).HasMaxLength(200);
        builder.Property(c => c.PixNomeRecebedor).HasMaxLength(200);
        builder.Property(c => c.PixCidade).HasMaxLength(100);
        builder.Property(c => c.MultaAtrasoPercentual).HasPrecision(5, 2);
        builder.Property(c => c.JurosMensaisPercentual).HasPrecision(5, 2);
    }
}
