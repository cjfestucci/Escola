using Escola.Api.Dtos;
using Escola.Api.Dtos.Requests;
using Escola.Domain.Entities;
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

        var alunos = await query.OrderBy(a => a.Nome).ToListAsync();
        return Ok(alunos.Select(a => a.ToDto()));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AlunoDetalheDto>> ObterPorId(Guid id)
    {
        var aluno = await ComIncludes().FirstOrDefaultAsync(a => a.Id == id);
        return aluno is null ? NotFound("Aluno não encontrado.") : Ok(aluno.ToDetalheDto());
    }

    [HttpPost]
    public async Task<ActionResult<AlunoDetalheDto>> Criar(CriarOuEditarAlunoRequest request)
    {
        var erro = await ValidarAsync(request);
        if (erro is not null) return BadRequest(erro);

        var aluno = new Aluno
        {
            Id = Guid.NewGuid(),
            Nome = request.Nome.Trim(),
            DataNascimento = request.DataNascimento,
            FotoUrl = request.FotoUrl,
            TurmaId = request.TurmaId
        };
        db.Alunos.Add(aluno);

        await SincronizarResponsaveisAsync(aluno, request.Responsaveis);
        await db.SaveChangesAsync();

        var criado = await ComIncludes().FirstAsync(a => a.Id == aluno.Id);
        return CreatedAtAction(nameof(ObterPorId), new { id = aluno.Id }, criado.ToDetalheDto());
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AlunoDetalheDto>> Editar(Guid id, CriarOuEditarAlunoRequest request)
    {
        var erro = await ValidarAsync(request);
        if (erro is not null) return BadRequest(erro);

        var aluno = await ComIncludes().FirstOrDefaultAsync(a => a.Id == id);
        if (aluno is null) return NotFound("Aluno não encontrado.");

        aluno.Nome = request.Nome.Trim();
        aluno.DataNascimento = request.DataNascimento;
        aluno.FotoUrl = request.FotoUrl;
        aluno.TurmaId = request.TurmaId;

        await SincronizarResponsaveisAsync(aluno, request.Responsaveis);
        await db.SaveChangesAsync();

        var editado = await ComIncludes().FirstAsync(a => a.Id == aluno.Id);
        return Ok(editado.ToDetalheDto());
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id)
    {
        var aluno = await db.Alunos.FirstOrDefaultAsync(a => a.Id == id);
        if (aluno is null) return NotFound("Aluno não encontrado.");

        db.Alunos.Remove(aluno);
        await db.SaveChangesAsync();

        return NoContent();
    }

    private async Task<string?> ValidarAsync(CriarOuEditarAlunoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
            return "Nome é obrigatório.";

        if (request.DataNascimento == default || request.DataNascimento > DateOnly.FromDateTime(DateTime.UtcNow))
            return "Data de nascimento inválida.";

        if (!await db.Turmas.AnyAsync(t => t.Id == request.TurmaId))
            return "Turma inválida.";

        if (request.Responsaveis.Count == 0)
            return "Informe ao menos um responsável.";

        if (request.Responsaveis.Any(r => string.IsNullOrWhiteSpace(r.Nome) || string.IsNullOrWhiteSpace(r.Email)))
            return "Nome e e-mail são obrigatórios para cada responsável.";

        return null;
    }

    private async Task SincronizarResponsaveisAsync(Aluno aluno, List<ResponsavelInput> inputs)
    {
        var vinculosAtuais = aluno.Responsaveis.ToList();
        var responsavelIdsMantidos = new HashSet<Guid>();

        foreach (var input in inputs)
        {
            var vinculoExistente = input.Id is { } id ? vinculosAtuais.FirstOrDefault(v => v.ResponsavelId == id) : null;

            if (vinculoExistente is not null)
            {
                vinculoExistente.Responsavel.Nome = input.Nome.Trim();
                vinculoExistente.Responsavel.Email = input.Email.Trim();
                vinculoExistente.Responsavel.Telefone = input.Telefone;
                vinculoExistente.ResponsavelFinanceiro = input.ResponsavelFinanceiro;
                responsavelIdsMantidos.Add(vinculoExistente.ResponsavelId);
                continue;
            }

            // Sem Id: pode ser gente nova, ou um responsável que já existe no sistema (ex.: irmão
            // já matriculado com o mesmo responsável). Reaproveita pelo e-mail em vez de duplicar.
            var email = input.Email.Trim();
            var responsavel = await db.Responsaveis.FirstOrDefaultAsync(r => r.Email.ToLower() == email.ToLower());
            if (responsavel is null)
            {
                responsavel = new Responsavel { Id = Guid.NewGuid(), Nome = input.Nome.Trim(), Email = email, Telefone = input.Telefone };
                db.Responsaveis.Add(responsavel);
            }

            if (vinculosAtuais.All(v => v.ResponsavelId != responsavel.Id))
            {
                db.AlunoResponsaveis.Add(new AlunoResponsavel
                {
                    AlunoId = aluno.Id,
                    ResponsavelId = responsavel.Id,
                    ResponsavelFinanceiro = input.ResponsavelFinanceiro
                });
            }

            responsavelIdsMantidos.Add(responsavel.Id);
        }

        var vinculosRemover = vinculosAtuais.Where(v => !responsavelIdsMantidos.Contains(v.ResponsavelId)).ToList();
        if (vinculosRemover.Count > 0)
            db.AlunoResponsaveis.RemoveRange(vinculosRemover);
    }

    private IQueryable<Aluno> ComIncludes() =>
        db.Alunos.Include(a => a.Turma).Include(a => a.Responsaveis).ThenInclude(ar => ar.Responsavel);
}
