using Escola.Api.Dtos;
using Escola.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Escola.Api.Controllers;

// Sem login ainda: o Portal dos Pais usa essa lista pra deixar o responsável
// se identificar, até a autenticação de verdade existir.
[ApiController]
[Route("api/responsaveis")]
public class ResponsaveisController(EscolaDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ResponsavelDto>>> Listar()
    {
        var responsaveis = await db.Responsaveis
            .Select(r => new ResponsavelDto(r.Id, r.Nome))
            .ToListAsync();

        return Ok(responsaveis);
    }

    [HttpGet("{id:guid}/alunos")]
    public async Task<ActionResult<List<AlunoDto>>> ListarAlunos(Guid id)
    {
        if (!await db.Responsaveis.AnyAsync(r => r.Id == id))
            return NotFound("Responsável não encontrado.");

        var alunos = await db.AlunoResponsaveis
            .Where(ar => ar.ResponsavelId == id)
            .Select(ar => new AlunoDto(
                ar.Aluno.Id, ar.Aluno.Nome, ar.Aluno.DataNascimento, ar.Aluno.FotoUrl,
                ar.Aluno.TurmaId, ar.Aluno.Turma.Nome))
            .ToListAsync();

        return Ok(alunos);
    }
}
