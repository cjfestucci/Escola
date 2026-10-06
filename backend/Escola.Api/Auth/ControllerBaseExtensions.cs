using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Escola.Api.Auth;

public static class ControllerBaseExtensions
{
    /// <summary>Id do usuário autenticado (claim `NameIdentifier`, presente em todo request autenticado)
    /// — usado pra saber quem registrar como autor de um log de auditoria.</summary>
    public static Guid UsuarioIdAtual(this ControllerBase controller) =>
        Guid.Parse(controller.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    /// <summary>Id do responsável do usuário logado (claim <c>responsavelId</c>), ou <see cref="Guid.Empty"/> se não houver — nunca bate com
    /// um responsável real, então a checagem "esse filho é meu?" falha fechada. Compara-se <b>Guid com Guid</b> (e não o texto do Guid no SQL:
    /// a representação em texto varia entre bancos).</summary>
    public static Guid ResponsavelIdAtual(this ControllerBase controller) =>
        Guid.TryParse(controller.User.FindFirst("responsavelId")?.Value, out var id) ? id : Guid.Empty;
}
