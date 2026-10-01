using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Escola.Api.Auth;

public static class ControllerBaseExtensions
{
    /// <summary>Id do usuário autenticado (claim `NameIdentifier`, presente em todo request autenticado)
    /// — usado pra saber quem registrar como autor de um log de auditoria.</summary>
    public static Guid UsuarioIdAtual(this ControllerBase controller) =>
        Guid.Parse(controller.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}
