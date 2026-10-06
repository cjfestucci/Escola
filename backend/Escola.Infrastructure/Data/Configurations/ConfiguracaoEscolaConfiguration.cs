using Escola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Escola.Infrastructure.Data.Configurations;

public class ConfiguracaoEscolaConfiguration : IEntityTypeConfiguration<ConfiguracaoEscola>
{
    public void Configure(EntityTypeBuilder<ConfiguracaoEscola> builder)
    {
        builder.Property(c => c.FusoHorario).HasMaxLength(64).IsRequired();
        builder.Property(c => c.CorPrincipal).HasMaxLength(7);
        builder.Property(c => c.LogoUrl).HasMaxLength(300);
    }
}
