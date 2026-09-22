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
[Route("api/financeiro/cobrancas")]
[Authorize]
public class FinanceiroController(EscolaDbContext db) : ControllerBase
{
    /// <summary>Visão administrativa: todas as cobranças da escola, com filtros opcionais.</summary>
    [HttpGet]
    [Authorize(Roles = GruposDePapeis.Financeiro)]
    public async Task<ActionResult<List<CobrancaDto>>> Listar(
        [FromQuery] Guid? turmaId, [FromQuery] Guid? alunoId, [FromQuery] bool? paga)
    {
        var query = ComIncludes();

        if (turmaId is { } t) query = query.Where(c => c.Aluno.TurmaId == t);
        if (alunoId is { } a) query = query.Where(c => c.AlunoId == a);
        if (paga is { } p) query = query.Where(c => c.Paga == p);

        var cobrancas = await query.OrderBy(c => c.Vencimento).ToListAsync();
        return Ok(cobrancas.Select(c => c.ToDto()));
    }

    [HttpPost]
    [Authorize(Roles = GruposDePapeis.Financeiro)]
    public async Task<ActionResult<CobrancaDto>> Criar(CriarCobrancaRequest request)
    {
        var erro = Validar(request.Descricao, request.Valor);
        if (erro is not null) return BadRequest(erro);

        if (!await db.Alunos.AnyAsync(a => a.Id == request.AlunoId))
            return BadRequest("Aluno inválido.");

        var cobranca = new Cobranca
        {
            Id = Guid.NewGuid(),
            AlunoId = request.AlunoId,
            Descricao = request.Descricao.Trim(),
            Valor = request.Valor,
            Vencimento = request.Vencimento,
            RegistradoEm = DateTime.UtcNow
        };
        db.Cobrancas.Add(cobranca);
        await db.SaveChangesAsync();

        var criada = await ComIncludes().FirstAsync(c => c.Id == cobranca.Id);
        return CreatedAtAction(nameof(Listar), criada.ToDto());
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = GruposDePapeis.Financeiro)]
    public async Task<ActionResult<CobrancaDto>> Editar(Guid id, EditarCobrancaRequest request)
    {
        var erro = Validar(request.Descricao, request.Valor);
        if (erro is not null) return BadRequest(erro);

        var cobranca = await db.Cobrancas.FirstOrDefaultAsync(c => c.Id == id);
        if (cobranca is null) return NotFound("Cobrança não encontrada.");

        cobranca.Descricao = request.Descricao.Trim();
        cobranca.Valor = request.Valor;
        cobranca.Vencimento = request.Vencimento;
        await db.SaveChangesAsync();

        var editada = await ComIncludes().FirstAsync(c => c.Id == id);
        return Ok(editada.ToDto());
    }

    [HttpPost("{id:guid}/marcar-paga")]
    [Authorize(Roles = GruposDePapeis.Financeiro)]
    public async Task<ActionResult<CobrancaDto>> MarcarPaga(Guid id)
    {
        var cobranca = await db.Cobrancas.FirstOrDefaultAsync(c => c.Id == id);
        if (cobranca is null) return NotFound("Cobrança não encontrada.");

        cobranca.Paga = true;
        cobranca.PagoEm = DateOnly.FromDateTime(DateTime.UtcNow);
        await db.SaveChangesAsync();

        var atualizada = await ComIncludes().FirstAsync(c => c.Id == id);
        return Ok(atualizada.ToDto());
    }

    [HttpPost("{id:guid}/desmarcar-paga")]
    [Authorize(Roles = GruposDePapeis.Financeiro)]
    public async Task<ActionResult<CobrancaDto>> DesmarcarPaga(Guid id)
    {
        var cobranca = await db.Cobrancas.FirstOrDefaultAsync(c => c.Id == id);
        if (cobranca is null) return NotFound("Cobrança não encontrada.");

        cobranca.Paga = false;
        cobranca.PagoEm = null;
        await db.SaveChangesAsync();

        var atualizada = await ComIncludes().FirstAsync(c => c.Id == id);
        return Ok(atualizada.ToDto());
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = GruposDePapeis.Financeiro)]
    public async Task<IActionResult> Excluir(Guid id)
    {
        var cobranca = await db.Cobrancas.FirstOrDefaultAsync(c => c.Id == id);
        if (cobranca is null) return NotFound("Cobrança não encontrada.");

        db.Cobrancas.Remove(cobranca);
        await db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>Visão do Portal dos Pais: cobranças de um aluno específico. Equipe vê qualquer aluno;
    /// um Responsável só o(s) próprio(s) filho(s).</summary>
    [HttpGet("/api/alunos/{alunoId:guid}/cobrancas")]
    [Authorize(Roles = $"{GruposDePapeis.Equipe},{GruposDePapeis.Responsavel}")]
    public async Task<ActionResult<List<CobrancaDto>>> ListarDoAluno(Guid alunoId)
    {
        if (!await db.Alunos.AnyAsync(a => a.Id == alunoId))
            return NotFound("Aluno não encontrado.");

        if (User.IsInRole("Responsavel"))
        {
            var responsavelId = User.FindFirst("responsavelId")?.Value;
            var ehFilho = await db.AlunoResponsaveis.AnyAsync(ar => ar.AlunoId == alunoId && ar.ResponsavelId.ToString() == responsavelId);
            if (!ehFilho) return Forbid();
        }

        var cobrancas = await ComIncludes()
            .Where(c => c.AlunoId == alunoId)
            .OrderByDescending(c => c.Vencimento)
            .ToListAsync();

        return Ok(cobrancas.Select(c => c.ToDto()));
    }

    private static string? Validar(string descricao, decimal valor)
    {
        if (string.IsNullOrWhiteSpace(descricao))
            return "Informe uma descrição.";

        if (valor <= 0)
            return "O valor deve ser maior que zero.";

        return null;
    }

    private IQueryable<Cobranca> ComIncludes() =>
        db.Cobrancas.Include(c => c.Aluno).ThenInclude(a => a.Turma);
}
