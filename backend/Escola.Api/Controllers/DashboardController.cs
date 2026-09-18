using Escola.Api.Dtos;
using Escola.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Escola.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController(EscolaDbContext db) : ControllerBase
{
    [HttpGet("resumo")]
    public async Task<ActionResult<ResumoDashboardDto>> Resumo()
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var inicio = hoje.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var fim = inicio.AddDays(1);

        var totalAlunos = await db.Alunos.CountAsync();
        var totalTurmas = await db.Turmas.CountAsync();
        var registrosHoje = await db.RegistrosRotina.CountAsync(r => r.RegistradoEm >= inicio && r.RegistradoEm < fim);

        return Ok(new ResumoDashboardDto(totalAlunos, totalTurmas, registrosHoje));
    }
}
