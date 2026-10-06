using Escola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Escola.Infrastructure.Data.Configurations;

public class AlunoConfiguration : IEntityTypeConfiguration<Aluno>
{
    public void Configure(EntityTypeBuilder<Aluno> builder)
    {
        builder.Property(a => a.Nome).IsRequired().HasMaxLength(200);
        builder.Property(a => a.DescontoMensalidadePercentual).HasPrecision(5, 2);
        builder.Property(a => a.MotivoDesconto).HasMaxLength(200);

        builder.HasOne(a => a.Turma)
            .WithMany(t => t.Alunos)
            .HasForeignKey(a => a.TurmaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
