using Escola.Api.Auth;
using Escola.Api.Dtos;
using Escola.Infrastructure.Data;
using Escola.Infrastructure.Tempo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Escola.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = GruposDePapeis.Equipe)]
public class DashboardController(EscolaDbContext db, IRelogioEscola relogio) : ControllerBase
{
    [HttpGet("resumo")]
    public async Task<ActionResult<ResumoDashboardDto>> Resumo([FromQuery] Guid? turmaId)
    {
        var (inicio, fim) = await relogio.IntervaloUtcDoDiaAsync(await relogio.HojeAsync());

        var alunosQuery = db.Alunos.AsQueryable();
        var turmasQuery = db.Turmas.AsQueryable();
        var registrosQuery = db.RegistrosRotina.AsQueryable();

        if (turmaId is { } id)
        {
            alunosQuery = alunosQuery.Where(a => a.TurmaId == id);
            turmasQuery = turmasQuery.Where(t => t.Id == id);
            registrosQuery = registrosQuery.Where(r => r.Aluno.TurmaId == id);
        }

        var totalAlunos = await alunosQuery.CountAsync();
        var totalTurmas = await turmasQuery.CountAsync();
        var registrosHoje = await registrosQuery.CountAsync(r => r.RegistradoEm >= inicio && r.RegistradoEm < fim);

        return Ok(new ResumoDashboardDto(totalAlunos, totalTurmas, registrosHoje));
    }
}
