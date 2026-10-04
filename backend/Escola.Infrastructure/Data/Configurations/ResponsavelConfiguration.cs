using Escola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Escola.Infrastructure.Data.Configurations;

public class ResponsavelConfiguration : IEntityTypeConfiguration<Responsavel>
{
    public void Configure(EntityTypeBuilder<Responsavel> builder)
    {
        builder.Property(r => r.Nome).IsRequired().HasMaxLength(200);
        builder.Property(r => r.Email).IsRequired().HasMaxLength(256);
        builder.HasIndex("ClienteId", nameof(Responsavel.Email)).IsUnique();
    }
}
