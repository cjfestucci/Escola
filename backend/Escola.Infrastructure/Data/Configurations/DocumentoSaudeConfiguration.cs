using Escola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Escola.Infrastructure.Data.Configurations;

public class DocumentoSaudeConfiguration : IEntityTypeConfiguration<DocumentoSaude>
{
    public void Configure(EntityTypeBuilder<DocumentoSaude> builder)
    {
        builder.Property(d => d.NomeArquivo).IsRequired().HasMaxLength(200);
        builder.Property(d => d.ContentType).IsRequired().HasMaxLength(100);
        builder.Property(d => d.ArquivoArmazenado).IsRequired().HasMaxLength(100);

        builder.HasOne(d => d.Aluno)
            .WithMany()
            .HasForeignKey(d => d.AlunoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.Usuario)
            .WithMany()
            .HasForeignKey(d => d.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => d.AlunoId);
    }
}
