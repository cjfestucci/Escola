using Escola.Api.Auth;
using Escola.Api.Servicos;
using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Escola.Infrastructure.Auditoria;
using Escola.Infrastructure.Clientes;
using Escola.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Escola.Api.Controllers;

public record TermoAlunoPendenteDto(Guid AlunoId, string AlunoNome, bool MatriculaPendente);
public record TermoPendenteDto(string Versao, string Titulo, IReadOnlyList<string> Paragrafos, IReadOnlyList<TermoAlunoPendenteDto> Alunos);
public record AceitarTermoRequest(string Versao, List<Guid> AlunoIds);
public record TermoAceiteDto(Guid Id, string ResponsavelNome, string Versao, DateTime AceitoEm, string? Ip, string TextoAceito);

/// <summary>Termo de matrícula/LGPD: o responsável vê e aceita no portal; a equipe consulta os aceites de cada aluno.</summary>
[ApiController]
[Authorize]
public class TermosController(EscolaDbContext db, IAuditoriaService auditoria, IClienteAtual clienteAtual) : ControllerBase
{
    /// <summary>O termo da versão atual e os filhos (ativos) do responsável logado que ainda não têm aceite dessa versão. Lista vazia = nada
    /// a aceitar, o portal segue normal.</summary>
    [HttpGet("api/portal/termo")]
    [Authorize(Roles = GruposDePapeis.Responsavel)]
    public async Task<ActionResult<TermoPendenteDto>> Pendente()
    {
        var responsavelId = this.ResponsavelIdAtual();
        var pendentes = await PendentesAsync(responsavelId);
        var paragrafos = pendentes.Count == 0 ? [] : await ParagrafosAsync(responsavelId, pendentes.Select(p => p.AlunoNome).ToList());
        return Ok(new TermoPendenteDto(TermoMatricula.Versao, TermoMatricula.Titulo, paragrafos, pendentes));
    }

    [HttpPost("api/portal/termo/aceitar")]
    [Authorize(Roles = GruposDePapeis.Responsavel)]
    public async Task<IActionResult> Aceitar(AceitarTermoRequest request)
    {
        if (request.Versao != TermoMatricula.Versao)
            return BadRequest("O termo foi atualizado. Recarregue a página para ler a versão nova.");

        var responsavelId = this.ResponsavelIdAtual();
        var pendentes = await PendentesAsync(responsavelId);
        if (pendentes.Count == 0) return NoContent();

        // O texto guardado é o mesmo que foi mostrado (com os nomes de todos os filhos pendentes); por isso a lista tem que bater.
        if (!pendentes.Select(p => p.AlunoId).ToHashSet().SetEquals(request.AlunoIds ?? []))
            return BadRequest("A lista de matrículas mudou. Recarregue a página e leia o termo de novo.");

        var responsavel = await db.Responsaveis.FirstAsync(r => r.Id == responsavelId);
        var texto = TermoMatricula.TextoCompleto(await ParagrafosAsync(responsavelId, pendentes.Select(p => p.AlunoNome).ToList()));
        var agora = DateTime.UtcNow;
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var navegador = Request.Headers.UserAgent.ToString() is { Length: > 0 } ua ? ua[..Math.Min(ua.Length, 512)] : null;
        var usuarioId = this.UsuarioIdAtual();

        var ids = pendentes.Select(p => p.AlunoId).ToList();
        var alunos = await db.Alunos.Where(a => ids.Contains(a.Id)).ToListAsync();
        foreach (var aluno in alunos)
        {
            db.TermosAceite.Add(new TermoAceite
            {
                Id = Guid.NewGuid(), AlunoId = aluno.Id, ResponsavelId = responsavelId, UsuarioId = usuarioId,
                Versao = TermoMatricula.Versao, TextoAceito = texto, AceitoEm = agora, Ip = ip, NavegadorUserAgent = navegador
            });

            string detalhe;
            if (aluno.MatriculaConfirmadaEm is null)
            {
                aluno.MatriculaConfirmadaEm = agora;
                detalhe = $"Matrícula confirmada: termo aceito por {responsavel.Nome} (versão {TermoMatricula.Versao})";
            }
            else
            {
                detalhe = $"Termo de matrícula aceito por {responsavel.Nome} (versão {TermoMatricula.Versao})";
            }
            auditoria.Registrar(nameof(Aluno), aluno.Id, AcaoAuditoria.Editado, usuarioId, detalhe);
        }

        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Os aceites do termo de um aluno (quem, quando, de onde e o texto exato) — prova do consentimento, pra equipe.</summary>
    [HttpGet("api/alunos/{alunoId:guid}/termos")]
    [Authorize(Roles = GruposDePapeis.Equipe)]
    public async Task<ActionResult<List<TermoAceiteDto>>> DoAluno(Guid alunoId)
    {
        if (!await db.Alunos.AnyAsync(a => a.Id == alunoId)) return NotFound("Aluno não encontrado.");

        var aceites = await db.TermosAceite
            .Where(t => t.AlunoId == alunoId)
            .OrderByDescending(t => t.AceitoEm)
            .Select(t => new TermoAceiteDto(t.Id, t.Responsavel.Nome, t.Versao, t.AceitoEm, t.Ip, t.TextoAceito))
            .ToListAsync();
        return Ok(aceites);
    }

    private async Task<List<TermoAlunoPendenteDto>> PendentesAsync(Guid responsavelId)
    {
        var aceitos = db.TermosAceite.Where(t => t.ResponsavelId == responsavelId && t.Versao == TermoMatricula.Versao).Select(t => t.AlunoId);
        return await db.AlunoResponsaveis
            .Where(ar => ar.ResponsavelId == responsavelId && ar.Aluno.Ativo && !aceitos.Contains(ar.AlunoId))
            .OrderBy(ar => ar.Aluno.Nome)
            .Select(ar => new TermoAlunoPendenteDto(ar.AlunoId, ar.Aluno.Nome, ar.Aluno.MatriculaConfirmadaEm == null))
            .ToListAsync();
    }

    private async Task<IReadOnlyList<string>> ParagrafosAsync(Guid responsavelId, List<string> nomesAlunos)
    {
        var cliente = await db.Clientes.Where(c => c.Id == clienteAtual.Id).Select(c => new { c.Nome, c.Segmento }).FirstAsync();
        var nomeResponsavel = await db.Responsaveis.Where(r => r.Id == responsavelId).Select(r => r.Nome).FirstAsync();
        return TermoMatricula.Paragrafos(cliente.Nome, nomeResponsavel, nomesAlunos, cliente.Segmento);
    }
}
