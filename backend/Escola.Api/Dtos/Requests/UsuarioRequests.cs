using Escola.Domain.Enums;

namespace Escola.Api.Dtos.Requests;

public record CriarUsuarioRequest(string Nome, string Email, PapelUsuario Papel);

public record EditarUsuarioRequest(string Nome, string Email, PapelUsuario Papel);
