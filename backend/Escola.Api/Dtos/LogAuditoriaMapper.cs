using Escola.Domain.Entities;

namespace Escola.Api.Dtos;

public static class LogAuditoriaMapper
{
    public static LogAuditoriaDto ToDto(this LogAuditoria l) => new(
        l.Id,
        l.Acao.ToString(),
        l.UsuarioId is null ? "Sistema" : l.Usuario?.Nome ?? "Usuário removido",
        l.Detalhe,
        l.RegistradoEm);
}
