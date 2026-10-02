using Escola.Api.Auth;
using Escola.Api.Dtos;
using Escola.Api.Dtos.Requests;
using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Escola.Infrastructure.Auditoria;
using Escola.Infrastructure.Data;
using Escola.Infrastructure.Estoque;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Escola.Api.Controllers;

[ApiController]
[Route("api/estoque/produtos")]
[Authorize(Roles = GruposDePapeis.Financeiro)]
public class ProdutosController(EscolaDbContext db, IAuditoriaService auditoria) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ProdutoDto>>> Listar()
    {
        var produtos = await db.Produtos.OrderBy(p => p.Nome).ToListAsync();
        var saldos = await db.SaldosAsync();
        return Ok(produtos.Select(p => p.ToDto(saldos.GetValueOrDefault(p.Id))));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProdutoDto>> ObterPorId(Guid id)
    {
        var produto = await db.Produtos.FirstOrDefaultAsync(p => p.Id == id);
        if (produto is null) return NotFound("Produto não encontrado.");

        var saldos = await db.SaldosAsync([id]);
        return Ok(produto.ToDto(saldos.GetValueOrDefault(id)));
    }

    [HttpPost]
    public async Task<ActionResult<ProdutoDto>> Criar(CriarOuEditarProdutoRequest request)
    {
        var erro = await ValidarAsync(request, null);
        if (erro is not null) return BadRequest(erro);

        var produto = new Produto
        {
            Id = Guid.NewGuid(),
            Nome = request.Nome.Trim(),
            Codigo = string.IsNullOrWhiteSpace(request.Codigo) ? null : request.Codigo.Trim(),
            UnidadeMedida = request.UnidadeMedida.Trim(),
            EstoqueMinimo = request.EstoqueMinimo,
            RegistradoEm = DateTime.UtcNow
        };
        db.Produtos.Add(produto);
        auditoria.Registrar(nameof(Produto), produto.Id, AcaoAuditoria.Criado, this.UsuarioIdAtual(), produto.Nome);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(ObterPorId), new { id = produto.Id }, produto.ToDto(0));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProdutoDto>> Editar(Guid id, CriarOuEditarProdutoRequest request)
    {
        var erro = await ValidarAsync(request, id);
        if (erro is not null) return BadRequest(erro);

        var produto = await db.Produtos.FirstOrDefaultAsync(p => p.Id == id);
        if (produto is null) return NotFound("Produto não encontrado.");

        var nomeAntes = produto.Nome;
        var codigoAntes = produto.Codigo;
        var unidadeMedidaAntes = produto.UnidadeMedida;
        var estoqueMinimoAntes = produto.EstoqueMinimo;

        produto.Nome = request.Nome.Trim();
        produto.Codigo = string.IsNullOrWhiteSpace(request.Codigo) ? null : request.Codigo.Trim();
        produto.UnidadeMedida = request.UnidadeMedida.Trim();
        produto.EstoqueMinimo = request.EstoqueMinimo;

        var detalhe = AuditoriaDetalhe.MontarAlteracoes(
            ("Nome", nomeAntes, produto.Nome),
            ("Código", codigoAntes, produto.Codigo),
            ("Unidade de medida", unidadeMedidaAntes, produto.UnidadeMedida),
            ("Estoque mínimo", estoqueMinimoAntes, produto.EstoqueMinimo));

        auditoria.Registrar(nameof(Produto), produto.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(), detalhe);
        await db.SaveChangesAsync();

        var saldos = await db.SaldosAsync([id]);
        return Ok(produto.ToDto(saldos.GetValueOrDefault(id)));
    }

    [HttpPost("{id:guid}/desativar")]
    public async Task<ActionResult<ProdutoDto>> Desativar(Guid id)
    {
        var produto = await db.Produtos.FirstOrDefaultAsync(p => p.Id == id);
        if (produto is null) return NotFound("Produto não encontrado.");

        produto.Ativo = false;
        auditoria.Registrar(nameof(Produto), produto.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(), $"Desativado: {produto.Nome}");
        await db.SaveChangesAsync();

        var saldos = await db.SaldosAsync([id]);
        return Ok(produto.ToDto(saldos.GetValueOrDefault(id)));
    }

    [HttpPost("{id:guid}/ativar")]
    public async Task<ActionResult<ProdutoDto>> Ativar(Guid id)
    {
        var produto = await db.Produtos.FirstOrDefaultAsync(p => p.Id == id);
        if (produto is null) return NotFound("Produto não encontrado.");

        produto.Ativo = true;
        auditoria.Registrar(nameof(Produto), produto.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(), $"Reativado: {produto.Nome}");
        await db.SaveChangesAsync();

        var saldos = await db.SaldosAsync([id]);
        return Ok(produto.ToDto(saldos.GetValueOrDefault(id)));
    }

    private async Task<string?> ValidarAsync(CriarOuEditarProdutoRequest request, Guid? idAtual)
    {
        if (string.IsNullOrWhiteSpace(request.Nome)) return "Nome é obrigatório.";
        if (string.IsNullOrWhiteSpace(request.UnidadeMedida)) return "Informe a unidade de medida.";
        if (request.EstoqueMinimo < 0) return "O estoque mínimo não pode ser negativo.";
        if (decimal.Round(request.EstoqueMinimo, 3) != request.EstoqueMinimo) return "O estoque mínimo aceita no máximo 3 casas decimais.";

        if (!string.IsNullOrWhiteSpace(request.Codigo))
        {
            var codigo = request.Codigo.Trim().ToLower();
            var duplicado = await db.Produtos.AnyAsync(p => p.Codigo != null && p.Codigo.ToLower() == codigo && p.Id != idAtual);
            if (duplicado) return "Já existe um produto com esse código.";
        }

        return null;
    }
}
