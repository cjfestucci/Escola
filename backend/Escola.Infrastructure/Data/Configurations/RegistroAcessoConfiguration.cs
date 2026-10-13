using Escola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Escola.Infrastructure.Data.Configurations;

public class RegistroAcessoConfiguration : IEntityTypeConfiguration<RegistroAcesso>
{
    public void Configure(EntityTypeBuilder<RegistroAcesso> builder)
    {
        builder.ToTable("RegistrosAcesso");
        builder.Property(r => r.Email).IsRequired().HasMaxLength(256);
        builder.Property(r => r.Ip).HasMaxLength(64);
        builder.Property(r => r.Navegador).HasMaxLength(300);
        builder.HasIndex(r => new { r.UsuarioId, r.OcorridoEm });
        builder.HasIndex(r => r.OcorridoEm); // limpeza dos que passaram de 6 meses
    }
}
