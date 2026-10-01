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
[Route("api/fornecedores")]
[Authorize(Roles = GruposDePapeis.Equipe)]
public class FornecedoresController(EscolaDbContext db, IAuditoriaService auditoria) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<FornecedorDto>>> Listar()
    {
        var fornecedores = await db.Fornecedores.OrderBy(f => f.Nome).ToListAsync();
        return Ok(fornecedores.Select(f => f.ToDto()));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FornecedorDto>> ObterPorId(Guid id)
    {
        var fornecedor = await db.Fornecedores.FirstOrDefaultAsync(f => f.Id == id);
        return fornecedor is null ? NotFound("Fornecedor não encontrado.") : Ok(fornecedor.ToDto());
    }

    [HttpPost]
    [Authorize(Roles = GruposDePapeis.Financeiro)]
    public async Task<ActionResult<FornecedorDto>> Criar(CriarOuEditarFornecedorRequest request)
    {
        var erro = Validar(request);
        if (erro is not null) return BadRequest(erro);

        var fornecedor = new Fornecedor
        {
            Id = Guid.NewGuid(),
            Nome = request.Nome.Trim(),
            Documento = string.IsNullOrWhiteSpace(request.Documento) ? null : request.Documento.Trim(),
            Telefone = string.IsNullOrWhiteSpace(request.Telefone) ? null : request.Telefone.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim()
        };
        db.Fornecedores.Add(fornecedor);
        auditoria.Registrar(nameof(Fornecedor), fornecedor.Id, AcaoAuditoria.Criado, this.UsuarioIdAtual(), fornecedor.Nome);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(ObterPorId), new { id = fornecedor.Id }, fornecedor.ToDto());
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = GruposDePapeis.Financeiro)]
    public async Task<ActionResult<FornecedorDto>> Editar(Guid id, CriarOuEditarFornecedorRequest request)
    {
        var erro = Validar(request);
        if (erro is not null) return BadRequest(erro);

        var fornecedor = await db.Fornecedores.FirstOrDefaultAsync(f => f.Id == id);
        if (fornecedor is null) return NotFound("Fornecedor não encontrado.");

        var nomeAntes = fornecedor.Nome;
        var documentoAntes = fornecedor.Documento;
        var telefoneAntes = fornecedor.Telefone;
        var emailAntes = fornecedor.Email;

        fornecedor.Nome = request.Nome.Trim();
        fornecedor.Documento = string.IsNullOrWhiteSpace(request.Documento) ? null : request.Documento.Trim();
        fornecedor.Telefone = string.IsNullOrWhiteSpace(request.Telefone) ? null : request.Telefone.Trim();
        fornecedor.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();

        var detalhe = AuditoriaDetalhe.MontarAlteracoes(
            ("Nome", nomeAntes, fornecedor.Nome),
            ("Documento", documentoAntes, fornecedor.Documento),
            ("Telefone", telefoneAntes, fornecedor.Telefone),
            ("E-mail", emailAntes, fornecedor.Email));

        auditoria.Registrar(nameof(Fornecedor), fornecedor.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(), detalhe);
        await db.SaveChangesAsync();

        return Ok(fornecedor.ToDto());
    }

    [HttpPost("{id:guid}/desativar")]
    [Authorize(Roles = GruposDePapeis.Financeiro)]
    public async Task<ActionResult<FornecedorDto>> Desativar(Guid id)
    {
        var fornecedor = await db.Fornecedores.FirstOrDefaultAsync(f => f.Id == id);
        if (fornecedor is null) return NotFound("Fornecedor não encontrado.");

        fornecedor.Ativo = false;
        auditoria.Registrar(nameof(Fornecedor), fornecedor.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(), $"Desativado: {fornecedor.Nome}");
        await db.SaveChangesAsync();

        return Ok(fornecedor.ToDto());
    }

    [HttpPost("{id:guid}/ativar")]
    [Authorize(Roles = GruposDePapeis.Financeiro)]
    public async Task<ActionResult<FornecedorDto>> Ativar(Guid id)
    {
        var fornecedor = await db.Fornecedores.FirstOrDefaultAsync(f => f.Id == id);
        if (fornecedor is null) return NotFound("Fornecedor não encontrado.");

        fornecedor.Ativo = true;
        auditoria.Registrar(nameof(Fornecedor), fornecedor.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(), $"Reativado: {fornecedor.Nome}");
        await db.SaveChangesAsync();

        return Ok(fornecedor.ToDto());
    }

    private static string? Validar(CriarOuEditarFornecedorRequest request) =>
        string.IsNullOrWhiteSpace(request.Nome) ? "Nome é obrigatório." : null;
}
