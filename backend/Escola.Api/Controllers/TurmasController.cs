using Escola.Api.Dtos;
using Escola.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Escola.Api.Controllers;

[ApiController]
[Route("api/turmas")]
public class TurmasController(EscolaDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<TurmaDto>>> Listar()
    {
        var turmas = await db.Turmas
            .Select(t => new TurmaDto(t.Id, t.Nome, t.Alunos.Count))
            .ToListAsync();

        return Ok(turmas);
    }
}
