using Escola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Escola.Infrastructure.Data.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.Property(u => u.Nome).IsRequired().HasMaxLength(200);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
        // Único por cliente (a coluna ClienteId é definida pelo EscolaDbContext): a mesma pessoa pode ter conta em dois clientes.
        builder.HasIndex("ClienteId", nameof(Usuario.Email)).IsUnique();

        builder.HasOne(u => u.Responsavel)
            .WithMany()
            .HasForeignKey(u => u.ResponsavelId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
