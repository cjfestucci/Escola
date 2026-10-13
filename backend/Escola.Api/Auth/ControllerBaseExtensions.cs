using System.Security.Claims;
using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Escola.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
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

    /// <summary>O Educador (Professor/Treinador) só lança coisas — chamada, jogo, convocação, rotina, diário — nas turmas em que está
    /// vinculado (<see cref="TurmaEducador"/>). Gestão e Financeiro lançam em qualquer turma. Ver e consultar continua liberado pra toda
    /// a Equipe: a regra é só pra gravar.</summary>
    public static async Task<bool> PodeLancarNaTurmaAsync(this ControllerBase controller, EscolaDbContext db, Guid turmaId)
    {
        // A Secretaria consulta turmas, chamada e jogos, mas não lança nada de turma.
        if (controller.User.IsInRole(nameof(PapelUsuario.Secretaria))) return false;
        if (!controller.User.IsInRole(nameof(PapelUsuario.Educador))) return true;
        var usuarioId = controller.UsuarioIdAtual();
        return await db.TurmaEducadores.AnyAsync(te => te.TurmaId == turmaId && te.UsuarioId == usuarioId);
    }

    /// <summary>403 com a mensagem pro Educador que tentou lançar numa turma que não é dele.</summary>
    public static ObjectResult ForaDasSuasTurmas(this ControllerBase controller) =>
        controller.StatusCode(StatusCodes.Status403Forbidden, controller.User.IsInRole(nameof(PapelUsuario.Secretaria))
            ? "A secretaria consulta, mas não faz lançamentos de turma (chamada, jogos, rotina e diário)."
            : "Você só pode fazer lançamentos nas turmas em que é professor. Fale com a coordenação.");
}
