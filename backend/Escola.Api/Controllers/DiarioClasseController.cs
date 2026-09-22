using Escola.Api.Auth;
using Escola.Api.Dtos;
using Escola.Api.Dtos.Requests;
using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Escola.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Escola.Api.Controllers;

[ApiController]
[Route("api/turmas/{turmaId:guid}/diario")]
[Authorize]
public class DiarioClasseController(EscolaDbContext db) : ControllerBase
{
    private const int MaxFotos = 4;

    /// <summary>Equipe vê o diário de qualquer turma; um Responsável só o de turma(s) onde tem filho matriculado.</summary>
    [HttpGet]
    [Authorize(Roles = $"{GruposDePapeis.Equipe},{GruposDePapeis.Responsavel}")]
    public async Task<ActionResult<List<RegistroDiarioClasseDto>>> Listar(Guid turmaId, [FromQuery] DateOnly? data)
    {
        if (!await db.Turmas.AnyAsync(t => t.Id == turmaId))
            return NotFound("Turma não encontrada.");

        if (User.IsInRole("Responsavel"))
        {
            var responsavelId = User.FindFirst("responsavelId")?.Value;
            var temFilhoNaTurma = await db.Alunos.AnyAsync(a =>
                a.TurmaId == turmaId && a.Responsaveis.Any(ar => ar.ResponsavelId.ToString() == responsavelId));
            if (!temFilhoNaTurma) return Forbid();
        }

        var dia = data ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var inicio = dia.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var fim = inicio.AddDays(1);

        var registros = await db.RegistrosDiarioClasse
            .Include(r => r.CriadoPor)
            .Include(r => r.Fotos)
            .Where(r => r.TurmaId == turmaId && r.RegistradoEm >= inicio && r.RegistradoEm < fim)
            .OrderByDescending(r => r.RegistradoEm)
            .ToListAsync();

        return Ok(registros.Select(r => r.ToDto()));
    }

    [HttpPost]
    [Authorize(Roles = GruposDePapeis.Equipe)]
    public async Task<ActionResult<RegistroDiarioClasseDto>> Criar(Guid turmaId, CriarOuEditarRegistroDiarioClasseRequest request)
    {
        var erro = await ValidarAsync(turmaId, request);
        if (erro is not null) return BadRequest(erro);

        var registro = new RegistroDiarioClasse
        {
            Id = Guid.NewGuid(),
            TurmaId = turmaId,
            CriadoPorUsuarioId = request.UsuarioId,
            RegistradoEm = DateTime.UtcNow,
            Titulo = request.Titulo.Trim(),
            Descricao = string.IsNullOrWhiteSpace(request.Descricao) ? null : request.Descricao.Trim()
        };
        foreach (var foto in MontarFotos(request.FotoUrls))
            registro.Fotos.Add(foto);

        db.RegistrosDiarioClasse.Add(registro);
        RegistrarLog(registro.Id, AcaoAuditoria.Criado, request.UsuarioId);
        await db.SaveChangesAsync();

        await db.Entry(registro).Reference(r => r.CriadoPor).LoadAsync();

        return CreatedAtAction(nameof(Listar), new { turmaId }, registro.ToDto());
    }

    [HttpPut("{registroId:guid}")]
    [Authorize(Roles = GruposDePapeis.Equipe)]
    public async Task<ActionResult<RegistroDiarioClasseDto>> Editar(Guid turmaId, Guid registroId, CriarOuEditarRegistroDiarioClasseRequest request)
    {
        var erro = await ValidarAsync(turmaId, request);
        if (erro is not null) return BadRequest(erro);

        var registro = await db.RegistrosDiarioClasse
            .Include(r => r.Fotos)
            .FirstOrDefaultAsync(r => r.Id == registroId && r.TurmaId == turmaId);
        if (registro is null) return NotFound("Registro não encontrado.");

        registro.Titulo = request.Titulo.Trim();
        registro.Descricao = string.IsNullOrWhiteSpace(request.Descricao) ? null : request.Descricao.Trim();

        db.FotosDiarioClasse.RemoveRange(registro.Fotos.ToList());
        foreach (var foto in MontarFotos(request.FotoUrls))
        {
            foto.RegistroDiarioClasseId = registro.Id;
            db.FotosDiarioClasse.Add(foto);
        }

        RegistrarLog(registro.Id, AcaoAuditoria.Editado, request.UsuarioId);
        await db.SaveChangesAsync();

        await db.Entry(registro).Reference(r => r.CriadoPor).LoadAsync();

        return Ok(registro.ToDto());
    }

    [HttpDelete("{registroId:guid}")]
    [Authorize(Roles = GruposDePapeis.Equipe)]
    public async Task<IActionResult> Excluir(Guid turmaId, Guid registroId, [FromQuery] Guid usuarioId)
    {
        if (!await db.Usuarios.AnyAsync(u => u.Id == usuarioId))
            return BadRequest("Usuário inválido.");

        var registro = await db.RegistrosDiarioClasse.FirstOrDefaultAsync(r => r.Id == registroId && r.TurmaId == turmaId);
        if (registro is null) return NotFound("Registro não encontrado.");

        db.RegistrosDiarioClasse.Remove(registro);
        RegistrarLog(registro.Id, AcaoAuditoria.Excluido, usuarioId);
        await db.SaveChangesAsync();

        return NoContent();
    }

    private async Task<string?> ValidarAsync(Guid turmaId, CriarOuEditarRegistroDiarioClasseRequest request)
    {
        if (request.FotoUrls is { Count: > MaxFotos })
            return $"No máximo {MaxFotos} fotos por registro.";

        if (!await db.Turmas.AnyAsync(t => t.Id == turmaId))
            return "Turma não encontrada.";

        if (!await db.Usuarios.AnyAsync(u => u.Id == request.UsuarioId))
            return "Usuário inválido.";

        if (string.IsNullOrWhiteSpace(request.Titulo))
            return "Informe um título.";

        return null;
    }

    private void RegistrarLog(Guid entidadeId, AcaoAuditoria acao, Guid usuarioId) =>
        db.LogsAuditoria.Add(new LogAuditoria
        {
            Id = Guid.NewGuid(),
            EntidadeTipo = nameof(RegistroDiarioClasse),
            EntidadeId = entidadeId,
            Acao = acao,
            UsuarioId = usuarioId,
            RegistradoEm = DateTime.UtcNow
        });

    private static List<FotoDiarioClasse> MontarFotos(List<string>? urls) =>
        (urls ?? []).Select((url, indice) => new FotoDiarioClasse { Id = Guid.NewGuid(), Url = url, Ordem = indice }).ToList();
}
