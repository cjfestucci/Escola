using Escola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Escola.Infrastructure.Data.Configurations;

public class FotoDiarioClasseConfiguration : IEntityTypeConfiguration<FotoDiarioClasse>
{
    public void Configure(EntityTypeBuilder<FotoDiarioClasse> builder)
    {
        builder.Property(f => f.Url).IsRequired().HasMaxLength(500);

        builder.HasOne(f => f.RegistroDiarioClasse)
            .WithMany(r => r.Fotos)
            .HasForeignKey(f => f.RegistroDiarioClasseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
