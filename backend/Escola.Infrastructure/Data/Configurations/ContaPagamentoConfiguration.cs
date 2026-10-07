using Escola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Escola.Infrastructure.Data.Configurations;

public class ContaPagamentoConfiguration : IEntityTypeConfiguration<ContaPagamento>
{
    public void Configure(EntityTypeBuilder<ContaPagamento> builder)
    {
        builder.Property(c => c.Ambiente).IsRequired().HasMaxLength(20);
        builder.Property(c => c.IdExterno).IsRequired().HasMaxLength(100);
        builder.Property(c => c.WalletId).HasMaxLength(100);
        builder.Property(c => c.ApiKeyCriptografada).IsRequired().HasMaxLength(1000);
        builder.Property(c => c.TitularNome).IsRequired().HasMaxLength(200);
        builder.Property(c => c.TitularCpfCnpj).IsRequired().HasMaxLength(14);
        builder.Property(c => c.TitularEmail).IsRequired().HasMaxLength(256);
        builder.Property(c => c.SituacaoGateway).HasMaxLength(50);
        // Uma conta de pagamento por escola.
        builder.HasIndex("ClienteId").IsUnique();
    }
}
