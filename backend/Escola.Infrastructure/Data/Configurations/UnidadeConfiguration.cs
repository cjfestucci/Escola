using Escola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Escola.Infrastructure.Data.Configurations;

public class UnidadeConfiguration : IEntityTypeConfiguration<Unidade>
{
    public void Configure(EntityTypeBuilder<Unidade> builder)
    {
        builder.Property(u => u.Nome).IsRequired().HasMaxLength(100);
        builder.Property(u => u.Endereco).HasMaxLength(200);
        builder.Property(u => u.Telefone).HasMaxLength(20);
    }
}
