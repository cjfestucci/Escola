using Escola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Escola.Infrastructure.Data.Configurations;

public class FichaSaudeConfiguration : IEntityTypeConfiguration<FichaSaude>
{
    public void Configure(EntityTypeBuilder<FichaSaude> builder)
    {
        builder.HasIndex(f => f.AlunoId).IsUnique();

        builder.HasOne(f => f.Aluno)
            .WithOne()
            .HasForeignKey<FichaSaude>(f => f.AlunoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
