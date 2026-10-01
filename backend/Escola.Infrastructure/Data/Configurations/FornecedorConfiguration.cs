using Escola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Escola.Infrastructure.Data.Configurations;

public class FornecedorConfiguration : IEntityTypeConfiguration<Fornecedor>
{
    public void Configure(EntityTypeBuilder<Fornecedor> builder)
    {
        builder.Property(f => f.Nome).IsRequired().HasMaxLength(150);
        builder.Property(f => f.Documento).HasMaxLength(20);
        builder.Property(f => f.Telefone).HasMaxLength(20);
        builder.Property(f => f.Email).HasMaxLength(150);
    }
}
