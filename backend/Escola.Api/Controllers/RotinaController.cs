using Escola.Api.Dtos;
using Escola.Api.Dtos.Requests;
using Escola.Domain.Entities;
using Escola.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Escola.Api.Controllers;

[ApiController]
[Route("api/alunos/{alunoId:guid}/rotina")]
public class RotinaController(EscolaDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<RegistroRotinaDto>>> Listar(Guid alunoId, [FromQuery] DateOnly? data)
    {
        if (!await db.Alunos.AnyAsync(a => a.Id == alunoId))
            return NotFound("Aluno não encontrado.");

        var dia = data ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var inicio = dia.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var fim = inicio.AddDays(1);

        var registros = await db.RegistrosRotina
            .Include(r => r.CriadoPor)
            .Where(r => r.AlunoId == alunoId && r.RegistradoEm >= inicio && r.RegistradoEm < fim)
            .OrderByDescending(r => r.RegistradoEm)
            .ToListAsync();

        return Ok(registros.Select(r => r.ToDto()));
    }

    [HttpPost("alimentacao")]
    public Task<ActionResult<RegistroRotinaDto>> Alimentacao(Guid alunoId, CriarRegistroAlimentacaoRequest request) =>
        Criar(alunoId, request.UsuarioId, request.Observacao, request.FotoUrl, new RegistroAlimentacao
        {
            Refeicao = request.Refeicao,
            Status = request.Status
        });

    [HttpPost("sono")]
    public Task<ActionResult<RegistroRotinaDto>> Sono(Guid alunoId, CriarRegistroSonoRequest request) =>
        Criar(alunoId, request.UsuarioId, request.Observacao, request.FotoUrl, new RegistroSono
        {
            HoraInicio = request.HoraInicio,
            HoraFim = request.HoraFim
        });

    [HttpPost("higiene")]
    public Task<ActionResult<RegistroRotinaDto>> Higiene(Guid alunoId, CriarRegistroHigieneRequest request) =>
        Criar(alunoId, request.UsuarioId, request.Observacao, request.FotoUrl, new RegistroHigiene
        {
            Tipo = request.Tipo
        });

    [HttpPost("humor")]
    public Task<ActionResult<RegistroRotinaDto>> Humor(Guid alunoId, CriarRegistroHumorRequest request) =>
        Criar(alunoId, request.UsuarioId, request.Observacao, request.FotoUrl, new RegistroHumor
        {
            Humor = request.Humor
        });

    [HttpPost("momento")]
    public Task<ActionResult<RegistroRotinaDto>> Momento(Guid alunoId, CriarRegistroMomentoRequest request) =>
        Criar(alunoId, request.UsuarioId, request.Observacao, request.FotoUrl, new RegistroMomento());

    private async Task<ActionResult<RegistroRotinaDto>> Criar(
        Guid alunoId, Guid usuarioId, string? observacao, string? fotoUrl, RegistroRotina registro)
    {
        if (!await db.Alunos.AnyAsync(a => a.Id == alunoId))
            return NotFound("Aluno não encontrado.");

        if (!await db.Usuarios.AnyAsync(u => u.Id == usuarioId))
            return BadRequest("Usuário (educador) inválido.");

        registro.Id = Guid.NewGuid();
        registro.AlunoId = alunoId;
        registro.CriadoPorUsuarioId = usuarioId;
        registro.RegistradoEm = DateTime.UtcNow;
        registro.Observacao = observacao;
        registro.FotoUrl = fotoUrl;

        db.RegistrosRotina.Add(registro);
        await db.SaveChangesAsync();

        await db.Entry(registro).Reference(r => r.CriadoPor).LoadAsync();

        return CreatedAtAction(nameof(Listar), new { alunoId }, registro.ToDto());
    }
}
