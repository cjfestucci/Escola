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
[Route("api/unidades")]
[Authorize(Roles = GruposDePapeis.Equipe)]
public class UnidadesController(EscolaDbContext db, IAuditoriaService auditoria) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<UnidadeDto>>> Listar()
    {
        var unidades = await db.Unidades.OrderBy(u => u.Nome).ToListAsync();
        return Ok(unidades.Select(u => u.ToDto()));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UnidadeDto>> ObterPorId(Guid id)
    {
        var unidade = await db.Unidades.FirstOrDefaultAsync(u => u.Id == id);
        return unidade is null ? NotFound("Unidade não encontrada.") : Ok(unidade.ToDto());
    }

    [HttpPost]
    [Authorize(Roles = GruposDePapeis.Gestao)]
    public async Task<ActionResult<UnidadeDto>> Criar(CriarOuEditarUnidadeRequest request)
    {
        var erro = Validar(request);
        if (erro is not null) return BadRequest(erro);

        var unidade = new Unidade
        {
            Id = Guid.NewGuid(),
            Nome = request.Nome.Trim(),
            Endereco = string.IsNullOrWhiteSpace(request.Endereco) ? null : request.Endereco.Trim(),
            Telefone = string.IsNullOrWhiteSpace(request.Telefone) ? null : request.Telefone.Trim(),
            Ativa = request.Ativa
        };
        db.Unidades.Add(unidade);
        auditoria.Registrar(nameof(Unidade), unidade.Id, AcaoAuditoria.Criado, this.UsuarioIdAtual(), unidade.Nome);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(ObterPorId), new { id = unidade.Id }, unidade.ToDto());
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = GruposDePapeis.Gestao)]
    public async Task<ActionResult<UnidadeDto>> Editar(Guid id, CriarOuEditarUnidadeRequest request)
    {
        var erro = Validar(request);
        if (erro is not null) return BadRequest(erro);

        var unidade = await db.Unidades.FirstOrDefaultAsync(u => u.Id == id);
        if (unidade is null) return NotFound("Unidade não encontrada.");

        var nomeAntes = unidade.Nome;
        var enderecoAntes = unidade.Endereco;
        var telefoneAntes = unidade.Telefone;
        var ativaAntes = unidade.Ativa;

        unidade.Nome = request.Nome.Trim();
        unidade.Endereco = string.IsNullOrWhiteSpace(request.Endereco) ? null : request.Endereco.Trim();
        unidade.Telefone = string.IsNullOrWhiteSpace(request.Telefone) ? null : request.Telefone.Trim();
        unidade.Ativa = request.Ativa;

        var detalhe = AuditoriaDetalhe.MontarAlteracoes(
            ("Nome", nomeAntes, unidade.Nome),
            ("Endereço", enderecoAntes, unidade.Endereco),
            ("Telefone", telefoneAntes, unidade.Telefone),
            ("Ativa", ativaAntes, unidade.Ativa));

        auditoria.Registrar(nameof(Unidade), unidade.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(), detalhe);
        await db.SaveChangesAsync();

        return Ok(unidade.ToDto());
    }

    /// <summary>Em vez de excluir (perderia a unidade de vista em turmas/relatórios antigos e tiraria o
    /// sentido do log de auditoria), desativar/ativar — unidades inativas somem dos seletores de cadastro
    /// mas continuam visíveis aqui e em qualquer turma que já apontava pra elas.</summary>
    [HttpPost("{id:guid}/desativar")]
    [Authorize(Roles = GruposDePapeis.Gestao)]
    public async Task<ActionResult<UnidadeDto>> Desativar(Guid id)
    {
        var unidade = await db.Unidades.FirstOrDefaultAsync(u => u.Id == id);
        if (unidade is null) return NotFound("Unidade não encontrada.");

        unidade.Ativa = false;
        auditoria.Registrar(nameof(Unidade), unidade.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(), $"Desativada: {unidade.Nome}");
        await db.SaveChangesAsync();

        return Ok(unidade.ToDto());
    }

    [HttpPost("{id:guid}/ativar")]
    [Authorize(Roles = GruposDePapeis.Gestao)]
    public async Task<ActionResult<UnidadeDto>> Ativar(Guid id)
    {
        var unidade = await db.Unidades.FirstOrDefaultAsync(u => u.Id == id);
        if (unidade is null) return NotFound("Unidade não encontrada.");

        unidade.Ativa = true;
        auditoria.Registrar(nameof(Unidade), unidade.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(), $"Reativada: {unidade.Nome}");
        await db.SaveChangesAsync();

        return Ok(unidade.ToDto());
    }

    private static string? Validar(CriarOuEditarUnidadeRequest request) =>
        string.IsNullOrWhiteSpace(request.Nome) ? "Nome é obrigatório." : null;
}
