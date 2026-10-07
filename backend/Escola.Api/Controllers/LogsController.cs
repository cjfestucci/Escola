using Escola.Api.Auth;
using Escola.Api.Dtos;
using Escola.Domain.Entities;
using Escola.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Escola.Api.Controllers;

/// <summary>Consulta genérica da trilha de auditoria (quem criou/editou/excluiu o quê) — usada pelo
/// modal "Ver histórico" que toda tela com dado editável expõe. Mesma visibilidade de quem já pode
/// ver a entidade em si: qualquer papel de Equipe.</summary>
[ApiController]
[Route("api/logs")]
[Authorize(Roles = GruposDePapeis.EquipeOuSuporte)]
public class LogsController(EscolaDbContext db) : ControllerBase
{
    /// <summary>Tipos de histórico que o Suporte pode consultar: só o das configurações do ambiente e do cliente.</summary>
    private static readonly string[] TiposDoSuporte = [nameof(ConfiguracaoEscola), nameof(ConfiguracaoFinanceira), nameof(Cliente), nameof(ContaPagamento)];

    [HttpGet]
    public async Task<ActionResult<List<LogAuditoriaDto>>> Listar([FromQuery] string entidadeTipo, [FromQuery] Guid entidadeId)
    {
        if (User.IsInRole(GruposDePapeis.Suporte) && !TiposDoSuporte.Contains(entidadeTipo)) return Forbid();

        var logs = await db.LogsAuditoria
            .Include(l => l.Usuario)
            .Where(l => l.EntidadeTipo == entidadeTipo && l.EntidadeId == entidadeId)
            .OrderByDescending(l => l.RegistradoEm)
            .ToListAsync();

        return Ok(logs.Select(l => l.ToDto()));
    }

    /// <summary>Tudo que aconteceu numa turma, num dia (ex.: todo registro de Diário de Classe criado/
    /// editado/excluído) — diferente de <see cref="Listar"/>, não precisa de um registro específico ainda
    /// existindo: um registro excluído continua aparecendo aqui, porque a entrada de log guarda a turma e
    /// o dia direto, sem depender do registro original sobreviver.</summary>
    [HttpGet("turma")]
    [Authorize(Roles = GruposDePapeis.Equipe)]
    public async Task<ActionResult<List<LogAuditoriaDto>>> ListarPorTurma([FromQuery] Guid turmaId, [FromQuery] DateOnly data)
    {
        var logs = await db.LogsAuditoria
            .Include(l => l.Usuario)
            .Where(l => l.TurmaId == turmaId && l.Data == data)
            .OrderByDescending(l => l.RegistradoEm)
            .ToListAsync();

        return Ok(logs.Select(l => l.ToDto()));
    }
}
