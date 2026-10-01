namespace Escola.Api.Dtos;

public record LogAuditoriaDto(
    Guid Id,
    string Acao,
    string UsuarioNome,
    string? Detalhe,
    DateTime RegistradoEm);
