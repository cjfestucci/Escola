using Escola.Api.Dtos;
using Escola.Api.Dtos.Requests;
using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Escola.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Escola.Api.Controllers;

[ApiController]
[Route("api/alunos/{alunoId:guid}/rotina")]
public class RotinaController(EscolaDbContext db) : ControllerBase
{
    private const int MaxFotos = 4;

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
            .Include(r => r.Fotos)
            .Where(r => r.AlunoId == alunoId && r.RegistradoEm >= inicio && r.RegistradoEm < fim)
            .OrderByDescending(r => r.RegistradoEm)
            .ToListAsync();

        return Ok(registros.Select(r => r.ToDto()));
    }

    [HttpPost("alimentacao")]
    public Task<ActionResult<RegistroRotinaDto>> Alimentacao(Guid alunoId, CriarRegistroAlimentacaoRequest request) =>
        Criar(alunoId, request.UsuarioId, request.Observacao, request.FotoUrls, new RegistroAlimentacao
        {
            Refeicao = request.Refeicao,
            Status = request.Status
        });

    [HttpPost("sono")]
    public Task<ActionResult<RegistroRotinaDto>> Sono(Guid alunoId, CriarRegistroSonoRequest request) =>
        Criar(alunoId, request.UsuarioId, request.Observacao, request.FotoUrls, new RegistroSono
        {
            HoraInicio = request.HoraInicio,
            HoraFim = request.HoraFim
        });

    [HttpPost("higiene")]
    public Task<ActionResult<RegistroRotinaDto>> Higiene(Guid alunoId, CriarRegistroHigieneRequest request) =>
        Criar(alunoId, request.UsuarioId, request.Observacao, request.FotoUrls, new RegistroHigiene
        {
            Tipo = request.Tipo
        });

    [HttpPost("humor")]
    public Task<ActionResult<RegistroRotinaDto>> Humor(Guid alunoId, CriarRegistroHumorRequest request) =>
        Criar(alunoId, request.UsuarioId, request.Observacao, request.FotoUrls, new RegistroHumor
        {
            Humor = request.Humor
        });

    [HttpPost("momento")]
    public Task<ActionResult<RegistroRotinaDto>> Momento(Guid alunoId, CriarRegistroMomentoRequest request) =>
        Criar(alunoId, request.UsuarioId, request.Observacao, request.FotoUrls, new RegistroMomento());

    [HttpPut("alimentacao/{registroId:guid}")]
    public Task<ActionResult<RegistroRotinaDto>> EditarAlimentacao(Guid alunoId, Guid registroId, CriarRegistroAlimentacaoRequest request) =>
        Editar<RegistroAlimentacao>(alunoId, registroId, request.UsuarioId, request.Observacao, request.FotoUrls, r =>
        {
            r.Refeicao = request.Refeicao;
            r.Status = request.Status;
        });

    [HttpPut("sono/{registroId:guid}")]
    public Task<ActionResult<RegistroRotinaDto>> EditarSono(Guid alunoId, Guid registroId, CriarRegistroSonoRequest request) =>
        Editar<RegistroSono>(alunoId, registroId, request.UsuarioId, request.Observacao, request.FotoUrls, r =>
        {
            r.HoraInicio = request.HoraInicio;
            r.HoraFim = request.HoraFim;
        });

    [HttpPut("higiene/{registroId:guid}")]
    public Task<ActionResult<RegistroRotinaDto>> EditarHigiene(Guid alunoId, Guid registroId, CriarRegistroHigieneRequest request) =>
        Editar<RegistroHigiene>(alunoId, registroId, request.UsuarioId, request.Observacao, request.FotoUrls, r =>
        {
            r.Tipo = request.Tipo;
        });

    [HttpPut("humor/{registroId:guid}")]
    public Task<ActionResult<RegistroRotinaDto>> EditarHumor(Guid alunoId, Guid registroId, CriarRegistroHumorRequest request) =>
        Editar<RegistroHumor>(alunoId, registroId, request.UsuarioId, request.Observacao, request.FotoUrls, r =>
        {
            r.Humor = request.Humor;
        });

    [HttpPut("momento/{registroId:guid}")]
    public Task<ActionResult<RegistroRotinaDto>> EditarMomento(Guid alunoId, Guid registroId, CriarRegistroMomentoRequest request) =>
        Editar<RegistroMomento>(alunoId, registroId, request.UsuarioId, request.Observacao, request.FotoUrls, _ => { });

    [HttpDelete("{registroId:guid}")]
    public async Task<IActionResult> Excluir(Guid alunoId, Guid registroId, [FromQuery] Guid usuarioId)
    {
        if (!await db.Usuarios.AnyAsync(u => u.Id == usuarioId))
            return BadRequest("Usuário (educador) inválido.");

        var registro = await db.RegistrosRotina.FirstOrDefaultAsync(r => r.Id == registroId && r.AlunoId == alunoId);
        if (registro is null)
            return NotFound("Registro não encontrado.");

        db.RegistrosRotina.Remove(registro);
        RegistrarLog(nameof(RegistroRotina), registro.Id, AcaoAuditoria.Excluido, usuarioId, registro.GetType().Name);
        await db.SaveChangesAsync();

        return NoContent();
    }

    private async Task<ActionResult<RegistroRotinaDto>> Criar(
        Guid alunoId, Guid usuarioId, string? observacao, List<string>? fotoUrls, RegistroRotina registro)
    {
        if (fotoUrls is { Count: > MaxFotos })
            return BadRequest($"No máximo {MaxFotos} fotos por registro.");

        if (!await db.Alunos.AnyAsync(a => a.Id == alunoId))
            return NotFound("Aluno não encontrado.");

        if (!await db.Usuarios.AnyAsync(u => u.Id == usuarioId))
            return BadRequest("Usuário (educador) inválido.");

        registro.Id = Guid.NewGuid();
        registro.AlunoId = alunoId;
        registro.CriadoPorUsuarioId = usuarioId;
        registro.RegistradoEm = DateTime.UtcNow;
        registro.Observacao = observacao;
        foreach (var foto in MontarFotos(fotoUrls))
            registro.Fotos.Add(foto);

        db.RegistrosRotina.Add(registro);
        RegistrarLog(nameof(RegistroRotina), registro.Id, AcaoAuditoria.Criado, usuarioId, registro.GetType().Name);
        await db.SaveChangesAsync();

        await db.Entry(registro).Reference(r => r.CriadoPor).LoadAsync();

        return CreatedAtAction(nameof(Listar), new { alunoId }, registro.ToDto());
    }

    private async Task<ActionResult<RegistroRotinaDto>> Editar<T>(
        Guid alunoId, Guid registroId, Guid usuarioId, string? observacao, List<string>? fotoUrls, Action<T> aplicarCampos)
        where T : RegistroRotina
    {
        if (fotoUrls is { Count: > MaxFotos })
            return BadRequest($"No máximo {MaxFotos} fotos por registro.");

        if (!await db.Usuarios.AnyAsync(u => u.Id == usuarioId))
            return BadRequest("Usuário (educador) inválido.");

        var registro = await db.Set<T>()
            .Include(r => r.Fotos)
            .FirstOrDefaultAsync(r => r.Id == registroId && r.AlunoId == alunoId);
        if (registro is null)
            return NotFound("Registro não encontrado.");

        aplicarCampos(registro);
        registro.Observacao = observacao;

        db.Fotos.RemoveRange(registro.Fotos.ToList());
        foreach (var foto in MontarFotos(fotoUrls))
        {
            foto.RegistroRotinaId = registro.Id;
            db.Fotos.Add(foto);
        }

        RegistrarLog(nameof(RegistroRotina), registro.Id, AcaoAuditoria.Editado, usuarioId, registro.GetType().Name);
        await db.SaveChangesAsync();

        await db.Entry(registro).Reference(r => r.CriadoPor).LoadAsync();

        return Ok(registro.ToDto());
    }

    private void RegistrarLog(string entidadeTipo, Guid entidadeId, AcaoAuditoria acao, Guid usuarioId, string? detalhe) =>
        db.LogsAuditoria.Add(new LogAuditoria
        {
            Id = Guid.NewGuid(),
            EntidadeTipo = entidadeTipo,
            EntidadeId = entidadeId,
            Acao = acao,
            UsuarioId = usuarioId,
            Detalhe = detalhe,
            RegistradoEm = DateTime.UtcNow
        });

    private static List<FotoRegistro> MontarFotos(List<string>? urls) =>
        (urls ?? []).Select((url, indice) => new FotoRegistro { Id = Guid.NewGuid(), Url = url, Ordem = indice }).ToList();
}
