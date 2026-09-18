using Escola.Api.Dtos;
using Escola.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Escola.Api.Controllers;

[ApiController]
[Route("api/alunos")]
public class AlunosController(EscolaDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<AlunoDto>>> Listar([FromQuery] Guid? turmaId)
    {
        var query = db.Alunos.Include(a => a.Turma).AsQueryable();

        if (turmaId is not null)
            query = query.Where(a => a.TurmaId == turmaId);

        var alunos = await query
            .Select(a => new AlunoDto(a.Id, a.Nome, a.DataNascimento, a.FotoUrl, a.TurmaId, a.Turma.Nome))
            .ToListAsync();

        return Ok(alunos);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AlunoDto>> ObterPorId(Guid id)
    {
        var aluno = await db.Alunos.Include(a => a.Turma)
            .Where(a => a.Id == id)
            .Select(a => new AlunoDto(a.Id, a.Nome, a.DataNascimento, a.FotoUrl, a.TurmaId, a.Turma.Nome))
            .FirstOrDefaultAsync();

        return aluno is null ? NotFound() : Ok(aluno);
    }
}
