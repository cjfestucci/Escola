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
        builder.Property(u => u.TotpSegredoCriptografado).HasMaxLength(300);
        builder.Property(u => u.TotpSegredoPendenteCriptografado).HasMaxLength(300);
        builder.Property(u => u.CodigosRecuperacaoHash).HasMaxLength(1000);
        builder.Property(u => u.CodigoEmailHash).HasMaxLength(64);
        builder.Ignore(u => u.DoisFatoresAtivo);
        builder.Ignore(u => u.DoisFatoresPorEmail);

        builder.HasOne(u => u.Responsavel)
            .WithMany()
            .HasForeignKey(u => u.ResponsavelId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
