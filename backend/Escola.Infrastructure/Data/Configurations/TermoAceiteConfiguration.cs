using Escola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Escola.Infrastructure.Data.Configurations;

public class TermoAceiteConfiguration : IEntityTypeConfiguration<TermoAceite>
{
    public void Configure(EntityTypeBuilder<TermoAceite> builder)
    {
        builder.Property(t => t.Versao).IsRequired().HasMaxLength(20);
        builder.Property(t => t.TextoAceito).IsRequired();
        builder.Property(t => t.Ip).HasMaxLength(64);
        builder.Property(t => t.NavegadorUserAgent).HasMaxLength(512);

        // Um aceite por responsável, aluno e versão do termo.
        builder.HasIndex(t => new { t.ResponsavelId, t.AlunoId, t.Versao }).IsUnique();
        builder.HasIndex(t => t.AlunoId);

        // Prova de consentimento: nada apaga em cascata.
        builder.HasOne(t => t.Aluno).WithMany().HasForeignKey(t => t.AlunoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.Responsavel).WithMany().HasForeignKey(t => t.ResponsavelId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.Usuario).WithMany().HasForeignKey(t => t.UsuarioId).OnDelete(DeleteBehavior.Restrict);
    }
}
