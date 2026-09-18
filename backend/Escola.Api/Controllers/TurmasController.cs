using Escola.Api.Auth;
using Escola.Api.Dtos;
using Escola.Api.Dtos.Requests;
using Escola.Domain.Entities;
using Escola.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Escola.Api.Controllers;

[ApiController]
[Route("api/turmas")]
[Authorize(Roles = GruposDePapeis.Equipe)]
public class TurmasController(EscolaDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<TurmaDto>>> Listar()
    {
        var turmas = await ComIncludes().OrderBy(t => t.Nome).ToListAsync();
        return Ok(turmas.Select(t => t.ToDto()));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TurmaDto>> ObterPorId(Guid id)
    {
        var turma = await ComIncludes().FirstOrDefaultAsync(t => t.Id == id);
        return turma is null ? NotFound("Turma não encontrada.") : Ok(turma.ToDto());
    }

    [HttpPost]
    [Authorize(Roles = GruposDePapeis.Gestao)]
    public async Task<ActionResult<TurmaDto>> Criar(CriarOuEditarTurmaRequest request)
    {
        var erro = await ValidarAsync(request);
        if (erro is not null) return BadRequest(erro);

        var turma = new Turma
        {
            Id = Guid.NewGuid(),
            Nome = request.Nome.Trim(),
            Periodo = request.Periodo,
            HorarioEntrada = request.HorarioEntrada,
            HorarioSaida = request.HorarioSaida
        };
        db.Turmas.Add(turma);

        if (request.ProfessorId is { } professorId)
            db.TurmaEducadores.Add(new TurmaEducador { TurmaId = turma.Id, UsuarioId = professorId });

        await db.SaveChangesAsync();

        var criada = await ComIncludes().FirstAsync(t => t.Id == turma.Id);
        return CreatedAtAction(nameof(ObterPorId), new { id = turma.Id }, criada.ToDto());
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = GruposDePapeis.Gestao)]
    public async Task<ActionResult<TurmaDto>> Editar(Guid id, CriarOuEditarTurmaRequest request)
    {
        var erro = await ValidarAsync(request);
        if (erro is not null) return BadRequest(erro);

        var turma = await db.Turmas.Include(t => t.Educadores).FirstOrDefaultAsync(t => t.Id == id);
        if (turma is null) return NotFound("Turma não encontrada.");

        turma.Nome = request.Nome.Trim();
        turma.Periodo = request.Periodo;
        turma.HorarioEntrada = request.HorarioEntrada;
        turma.HorarioSaida = request.HorarioSaida;

        db.TurmaEducadores.RemoveRange(turma.Educadores.ToList());
        if (request.ProfessorId is { } professorId)
            db.TurmaEducadores.Add(new TurmaEducador { TurmaId = turma.Id, UsuarioId = professorId });

        await db.SaveChangesAsync();

        var editada = await ComIncludes().FirstAsync(t => t.Id == turma.Id);
        return Ok(editada.ToDto());
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = GruposDePapeis.Gestao)]
    public async Task<IActionResult> Excluir(Guid id)
    {
        var turma = await db.Turmas.Include(t => t.Alunos).FirstOrDefaultAsync(t => t.Id == id);
        if (turma is null) return NotFound("Turma não encontrada.");

        if (turma.Alunos.Count > 0)
            return BadRequest("Não é possível excluir uma turma com alunos matriculados.");

        db.Turmas.Remove(turma);
        await db.SaveChangesAsync();

        return NoContent();
    }

    private async Task<string?> ValidarAsync(CriarOuEditarTurmaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
            return "Nome é obrigatório.";

        if (request.HorarioSaida <= request.HorarioEntrada)
            return "Horário de saída deve ser depois do horário de entrada.";

        if (request.ProfessorId is { } professorId && !await db.Usuarios.AnyAsync(u => u.Id == professorId))
            return "Professor inválido.";

        return null;
    }

    private IQueryable<Turma> ComIncludes() =>
        db.Turmas.Include(t => t.Alunos).Include(t => t.Educadores).ThenInclude(te => te.Usuario);
}
