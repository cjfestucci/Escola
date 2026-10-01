using Escola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Escola.Infrastructure.Data.Configurations;

public class ContaPagarConfiguration : IEntityTypeConfiguration<ContaPagar>
{
    public void Configure(EntityTypeBuilder<ContaPagar> builder)
    {
        builder.Property(c => c.Descricao).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Valor).HasPrecision(10, 2);

        builder.HasOne(c => c.Fornecedor)
            .WithMany(f => f.ContasPagar)
            .HasForeignKey(c => c.FornecedorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.Vencimento);
    }
}
