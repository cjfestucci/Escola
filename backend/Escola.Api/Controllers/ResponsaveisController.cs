using Escola.Api.Auth;
using Escola.Api.Dtos;
using Escola.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Escola.Api.Controllers;

[ApiController]
[Route("api/responsaveis")]
[Authorize]
public class ResponsaveisController(EscolaDbContext db) : ControllerBase
{
    [HttpGet("{id:guid}/alunos")]
    public async Task<ActionResult<List<AlunoDto>>> ListarAlunos(Guid id)
    {
        // Um responsável só pode ver os próprios filhos; a equipe pode consultar qualquer um (suporte).
        var ehEquipe = User.IsInRole("Admin") || User.IsInRole("Coordenador") || User.IsInRole("Educador") || User.IsInRole("Financeiro");
        if (!ehEquipe && User.FindFirst("responsavelId")?.Value != id.ToString())
            return Forbid();

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
