using Escola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Escola.Infrastructure.Data.Configurations;

public class RedefinicaoSenhaConfiguration : IEntityTypeConfiguration<RedefinicaoSenha>
{
    public void Configure(EntityTypeBuilder<RedefinicaoSenha> builder)
    {
        builder.Property(r => r.TokenHash).IsRequired().HasMaxLength(64);
        builder.HasIndex(r => r.TokenHash).IsUnique();
        builder.HasIndex(r => new { r.UsuarioId, r.CriadoEm });

        // Se a conta for excluída, o pedido pendente some junto (não há exclusão de usuário pela UI, mas por SQL pode haver).
        builder.HasOne(r => r.Usuario).WithMany().HasForeignKey(r => r.UsuarioId).OnDelete(DeleteBehavior.Cascade);
    }
}
