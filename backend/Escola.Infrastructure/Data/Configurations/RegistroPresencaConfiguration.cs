using Escola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Escola.Infrastructure.Data.Configurations;

public class RegistroPresencaConfiguration : IEntityTypeConfiguration<RegistroPresenca>
{
    public void Configure(EntityTypeBuilder<RegistroPresenca> builder)
    {
        builder.HasOne(r => r.Turma).WithMany().HasForeignKey(r => r.TurmaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.Aluno).WithMany().HasForeignKey(r => r.AlunoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.Usuario).WithMany().HasForeignKey(r => r.UsuarioId).OnDelete(DeleteBehavior.Restrict);

        // Um aluno não pode ter duas presenças no mesmo dia (mesmo em turmas diferentes).
        builder.HasIndex(r => new { r.AlunoId, r.Data }).IsUnique();
        builder.HasIndex(r => new { r.TurmaId, r.Data });
    }
}
