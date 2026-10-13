using Escola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Escola.Infrastructure.Data.Configurations;

public class AparelhoConfiavelConfiguration : IEntityTypeConfiguration<AparelhoConfiavel>
{
    public void Configure(EntityTypeBuilder<AparelhoConfiavel> builder)
    {
        builder.ToTable("AparelhosConfiaveis");
        builder.Property(a => a.TokenHash).IsRequired().HasMaxLength(64);
        builder.Property(a => a.Descricao).HasMaxLength(200);
        builder.HasIndex(a => a.TokenHash).IsUnique();
        builder.HasIndex(a => a.UsuarioId);
        builder.HasOne(a => a.Usuario).WithMany().HasForeignKey(a => a.UsuarioId).OnDelete(DeleteBehavior.Cascade);
    }
}
