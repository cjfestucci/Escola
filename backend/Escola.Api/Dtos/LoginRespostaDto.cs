namespace Escola.Api.Dtos;

public record LoginRespostaDto(string Token, Guid UsuarioId, string Nome, string Papel, Guid? ResponsavelId);
