using Escola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Escola.Infrastructure.Data.Configurations;

public class AlunoResponsavelConfiguration : IEntityTypeConfiguration<AlunoResponsavel>
{
    public void Configure(EntityTypeBuilder<AlunoResponsavel> builder)
    {
        builder.HasKey(ar => new { ar.AlunoId, ar.ResponsavelId });

        builder.HasOne(ar => ar.Aluno)
            .WithMany(a => a.Responsaveis)
            .HasForeignKey(ar => ar.AlunoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ar => ar.Responsavel)
            .WithMany(r => r.Alunos)
            .HasForeignKey(ar => ar.ResponsavelId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
