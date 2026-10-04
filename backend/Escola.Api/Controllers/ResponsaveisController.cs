using Escola.Api.Auth;
using Escola.Api.Dtos;
using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Escola.Infrastructure.Auditoria;
using Escola.Infrastructure.Auth;
using Escola.Infrastructure.Data;
using Escola.Infrastructure.Financeiro;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Escola.Api.Controllers;

[ApiController]
[Route("api/responsaveis")]
[Authorize]
public class ResponsaveisController(EscolaDbContext db, IAuditoriaService auditoria, IBloqueioAlunoService bloqueio) : ControllerBase
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
                ar.Aluno.TurmaId, ar.Aluno.Turma.Nome, ar.Aluno.Ativo, ar.Aluno.Posicao, false))
            .ToListAsync();

        var bloqueados = await bloqueio.ObterBloqueadosAsync(alunos.Select(a => a.Id).ToList());
        return Ok(alunos.Select(a => a with { Bloqueado = bloqueados.Contains(a.Id) }).ToList());
    }

    /// <summary>Gera uma nova senha temporária pro login do Portal dos Pais desse responsável.</summary>
    [HttpPost("{id:guid}/redefinir-senha")]
    [Authorize(Roles = GruposDePapeis.Gestao)]
    public async Task<ActionResult<SenhaGeradaDto>> RedefinirSenha(Guid id)
    {
        var responsavel = await db.Responsaveis.FirstOrDefaultAsync(r => r.Id == id);
        if (responsavel is null) return NotFound("Responsável não encontrado.");

        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.ResponsavelId == id && u.Papel == PapelUsuario.Responsavel);
        if (usuario is null) return NotFound("Esse responsável ainda não tem login no Portal dos Pais.");

        var senha = GeradorSenhaTemporaria.Gerar();
        usuario.SenhaHash = SenhaHasher.Hash(senha);
        auditoria.Registrar(nameof(Responsavel), responsavel.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(), $"Senha redefinida: {responsavel.Nome}");
        await db.SaveChangesAsync();

        return Ok(new SenhaGeradaDto(responsavel.Nome, responsavel.Email, senha));
    }
}
