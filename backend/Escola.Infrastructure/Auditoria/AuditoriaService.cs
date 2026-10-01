using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Escola.Infrastructure.Data;

namespace Escola.Infrastructure.Auditoria;

public class AuditoriaService(EscolaDbContext db) : IAuditoriaService
{
    public void Registrar(string entidadeTipo, Guid entidadeId, AcaoAuditoria acao, Guid usuarioId, string? detalhe = null, Guid? turmaId = null, DateOnly? data = null) =>
        db.LogsAuditoria.Add(new LogAuditoria
        {
            Id = Guid.NewGuid(),
            EntidadeTipo = entidadeTipo,
            EntidadeId = entidadeId,
            Acao = acao,
            UsuarioId = usuarioId,
            Detalhe = detalhe,
            TurmaId = turmaId,
            Data = data,
            RegistradoEm = DateTime.UtcNow
        });
}
