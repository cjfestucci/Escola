using Escola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Escola.Infrastructure.Data.Configurations;

public class FotoRegistroConfiguration : IEntityTypeConfiguration<FotoRegistro>
{
    public void Configure(EntityTypeBuilder<FotoRegistro> builder)
    {
        builder.Property(f => f.Url).IsRequired().HasMaxLength(500);

        builder.HasOne(f => f.RegistroRotina)
            .WithMany(r => r.Fotos)
            .HasForeignKey(f => f.RegistroRotinaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
