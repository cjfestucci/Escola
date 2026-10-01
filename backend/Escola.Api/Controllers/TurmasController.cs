using Escola.Api.Auth;
using Escola.Api.Dtos;
using Escola.Api.Dtos.Requests;
using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Escola.Infrastructure.Auditoria;
using Escola.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Escola.Api.Controllers;

[ApiController]
[Route("api/turmas")]
[Authorize(Roles = GruposDePapeis.Equipe)]
public class TurmasController(EscolaDbContext db, IAuditoriaService auditoria) : ControllerBase
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
            HorarioSaida = request.HorarioSaida,
            UnidadeId = request.UnidadeId
        };
        db.Turmas.Add(turma);

        if (request.ProfessorId is { } professorId)
            db.TurmaEducadores.Add(new TurmaEducador { TurmaId = turma.Id, UsuarioId = professorId });

        auditoria.Registrar(nameof(Turma), turma.Id, AcaoAuditoria.Criado, this.UsuarioIdAtual(), turma.Nome);
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

        var turma = await db.Turmas.Include(t => t.Educadores).ThenInclude(te => te.Usuario).Include(t => t.Unidade).FirstOrDefaultAsync(t => t.Id == id);
        if (turma is null) return NotFound("Turma não encontrada.");

        var nomeAntes = turma.Nome;
        var periodoAntes = turma.Periodo;
        var horarioEntradaAntes = turma.HorarioEntrada;
        var horarioSaidaAntes = turma.HorarioSaida;
        var unidadeIdAntes = turma.UnidadeId;
        var unidadeNomeAntes = turma.Unidade.Nome;
        var professorVinculoAntes = turma.Educadores.FirstOrDefault();
        var professorIdAntes = professorVinculoAntes?.UsuarioId;
        var professorNomeAntes = professorVinculoAntes?.Usuario.Nome ?? "Nenhum";

        turma.Nome = request.Nome.Trim();
        turma.Periodo = request.Periodo;
        turma.HorarioEntrada = request.HorarioEntrada;
        turma.HorarioSaida = request.HorarioSaida;
        turma.UnidadeId = request.UnidadeId;

        db.TurmaEducadores.RemoveRange(turma.Educadores.ToList());
        if (request.ProfessorId is { } professorId)
            db.TurmaEducadores.Add(new TurmaEducador { TurmaId = turma.Id, UsuarioId = professorId });

        var unidadeNomeDepois = request.UnidadeId == unidadeIdAntes
            ? unidadeNomeAntes
            : (await db.Unidades.FindAsync(request.UnidadeId))?.Nome ?? "desconhecida";
        var professorNomeDepois = request.ProfessorId == professorIdAntes
            ? professorNomeAntes
            : request.ProfessorId is { } pid ? (await db.Usuarios.FindAsync(pid))?.Nome ?? "desconhecido" : "Nenhum";

        var detalhe = AuditoriaDetalhe.MontarAlteracoes(
            ("Nome", nomeAntes, turma.Nome),
            ("Período", periodoAntes.ToString(), turma.Periodo.ToString()),
            ("Horário de entrada", horarioEntradaAntes, turma.HorarioEntrada),
            ("Horário de saída", horarioSaidaAntes, turma.HorarioSaida),
            ("Unidade", unidadeNomeAntes, unidadeNomeDepois),
            ("Professor", professorNomeAntes, professorNomeDepois));

        auditoria.Registrar(nameof(Turma), turma.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(), detalhe);
        await db.SaveChangesAsync();

        var editada = await ComIncludes().FirstAsync(t => t.Id == turma.Id);
        return Ok(editada.ToDto());
    }

    [HttpPost("{id:guid}/desativar")]
    [Authorize(Roles = GruposDePapeis.Gestao)]
    public async Task<ActionResult<TurmaDto>> Desativar(Guid id)
    {
        var turma = await db.Turmas.FirstOrDefaultAsync(t => t.Id == id);
        if (turma is null) return NotFound("Turma não encontrada.");

        turma.Ativa = false;
        auditoria.Registrar(nameof(Turma), turma.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(), $"Desativada: {turma.Nome}");
        await db.SaveChangesAsync();

        var atualizada = await ComIncludes().FirstAsync(t => t.Id == id);
        return Ok(atualizada.ToDto());
    }

    [HttpPost("{id:guid}/ativar")]
    [Authorize(Roles = GruposDePapeis.Gestao)]
    public async Task<ActionResult<TurmaDto>> Ativar(Guid id)
    {
        var turma = await db.Turmas.FirstOrDefaultAsync(t => t.Id == id);
        if (turma is null) return NotFound("Turma não encontrada.");

        turma.Ativa = true;
        auditoria.Registrar(nameof(Turma), turma.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(), $"Reativada: {turma.Nome}");
        await db.SaveChangesAsync();

        var atualizada = await ComIncludes().FirstAsync(t => t.Id == id);
        return Ok(atualizada.ToDto());
    }

    private async Task<string?> ValidarAsync(CriarOuEditarTurmaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
            return "Nome é obrigatório.";

        if (request.HorarioSaida <= request.HorarioEntrada)
            return "Horário de saída deve ser depois do horário de entrada.";

        if (request.ProfessorId is { } professorId && !await db.Usuarios.AnyAsync(u => u.Id == professorId))
            return "Professor inválido.";

        if (!await db.Unidades.AnyAsync(u => u.Id == request.UnidadeId))
            return "Unidade inválida.";

        return null;
    }

    private IQueryable<Turma> ComIncludes() =>
        db.Turmas.Include(t => t.Alunos).Include(t => t.Educadores).ThenInclude(te => te.Usuario).Include(t => t.Unidade);
}
