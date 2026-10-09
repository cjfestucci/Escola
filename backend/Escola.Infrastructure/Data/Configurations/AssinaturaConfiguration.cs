using Escola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Escola.Infrastructure.Data.Configurations;

public class AssinaturaConfiguration : IEntityTypeConfiguration<Assinatura>
{
    public void Configure(EntityTypeBuilder<Assinatura> builder)
    {
        builder.Property(a => a.ValorMensal).HasPrecision(12, 2);
        builder.Property(a => a.CpfCnpj).IsRequired().HasMaxLength(14);
        builder.Property(a => a.Cidade).IsRequired().HasMaxLength(120);
        builder.Property(a => a.Celular).IsRequired().HasMaxLength(20);
        builder.Property(a => a.EmailCobranca).IsRequired().HasMaxLength(256);
        builder.Property(a => a.Ambiente).HasMaxLength(20);
        builder.Property(a => a.IdClienteGateway).HasMaxLength(100);
        builder.Property(a => a.IdAssinaturaGateway).HasMaxLength(100);
        builder.Property(a => a.LinkPagamento).HasMaxLength(500);
        builder.Property(a => a.TermosVersao).IsRequired().HasMaxLength(40);
        builder.Property(a => a.TermosIp).HasMaxLength(64);
        builder.Property(a => a.TermosNavegador).HasMaxLength(500);
        // Uma assinatura por cliente; o webhook procura pelo id da assinatura no gateway.
        builder.HasIndex("ClienteId").IsUnique();
        builder.HasIndex(a => a.IdAssinaturaGateway);
    }
}
