using System.Globalization;
using Escola.Api.Auth;
using Escola.Api.Dtos;
using Escola.Api.Dtos.Requests;
using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Escola.Infrastructure.Auditoria;
using Escola.Infrastructure.Data;
using Escola.Infrastructure.Estoque;
using Escola.Infrastructure.Tempo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Escola.Api.Controllers;

[ApiController]
[Route("api/estoque/movimentacoes")]
[Authorize(Roles = GruposDePapeis.Financeiro)]
public class MovimentacoesEstoqueController(EscolaDbContext db, IAuditoriaService auditoria, IRelogioEscola relogio) : ControllerBase
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    [HttpGet]
    public async Task<ActionResult<List<MovimentacaoEstoqueDto>>> Listar([FromQuery] Guid? produtoId, [FromQuery] TipoMovimentacaoEstoque? tipo)
    {
        var query = ComIncludes();

        if (produtoId is { } p) query = query.Where(m => m.ProdutoId == p);
        if (tipo is { } t) query = query.Where(m => m.Tipo == t);

        var movimentacoes = await query
            .OrderByDescending(m => m.Data)
            .ThenByDescending(m => m.RegistradoEm)
            .ToListAsync();
        return Ok(movimentacoes.Select(m => m.ToDto()));
    }

    [HttpPost]
    public async Task<ActionResult<MovimentacaoEstoqueDto>> Criar(CriarMovimentacaoEstoqueRequest request)
    {
        if (!Enum.IsDefined(request.Tipo)) return BadRequest("Tipo de movimentação inválido.");
        if (request.Quantidade <= 0) return BadRequest("A quantidade deve ser maior que zero.");
        if (decimal.Round(request.Quantidade, 3) != request.Quantidade) return BadRequest("A quantidade aceita no máximo 3 casas decimais.");
        if (request.Data == default) return BadRequest("Informe a data.");
        if (request.Observacao is { Length: > 300 }) return BadRequest("A observação pode ter no máximo 300 caracteres.");

        if (request.Data > await relogio.HojeAsync()) return BadRequest("A data da movimentação não pode ser futura.");

        var produto = await db.Produtos.FirstOrDefaultAsync(p => p.Id == request.ProdutoId);
        if (produto is null) return BadRequest("Produto inválido.");
        if (!produto.Ativo) return BadRequest("Produto inativo — reative-o antes de lançar uma movimentação.");

        Fornecedor? fornecedor = null;
        if (request.FornecedorId is { } fornecedorId)
        {
            if (request.Tipo != TipoMovimentacaoEstoque.Entrada) return BadRequest("O fornecedor só se aplica a entradas.");

            fornecedor = await db.Fornecedores.FirstOrDefaultAsync(f => f.Id == fornecedorId);
            if (fornecedor is null) return BadRequest("Fornecedor inválido.");
        }

        var saldoAntes = (await db.SaldosAsync([produto.Id])).GetValueOrDefault(produto.Id);
        if (request.Tipo == TipoMovimentacaoEstoque.Saida && request.Quantidade > saldoAntes)
            return BadRequest($"Saldo insuficiente: há {Formatar(saldoAntes)} {produto.UnidadeMedida} em estoque.");

        var movimentacao = new MovimentacaoEstoque
        {
            Id = Guid.NewGuid(),
            ProdutoId = produto.Id,
            Tipo = request.Tipo,
            Quantidade = request.Quantidade,
            Data = request.Data,
            FornecedorId = fornecedor?.Id,
            Observacao = string.IsNullOrWhiteSpace(request.Observacao) ? null : request.Observacao.Trim(),
            UsuarioId = this.UsuarioIdAtual(),
            RegistradoEm = DateTime.UtcNow
        };
        db.MovimentacoesEstoque.Add(movimentacao);

        // Registrada na trilha do próprio Produto (EntidadeTipo = Produto) pra aparecer no "Ver histórico" dele,
        // já que a movimentação em si é imutável e não tem tela/linha própria pra ancorar um histórico.
        var saldoDepois = request.Tipo == TipoMovimentacaoEstoque.Entrada ? saldoAntes + request.Quantidade : saldoAntes - request.Quantidade;
        var rotuloTipo = request.Tipo == TipoMovimentacaoEstoque.Entrada ? "Entrada" : "Saída";
        var detalhe = $"{rotuloTipo} de {Formatar(request.Quantidade)} {produto.UnidadeMedida}"
            + (fornecedor is null ? "" : $" (fornecedor {fornecedor.Nome})")
            + $" em {request.Data:dd/MM/yyyy}"
            + $"\nSaldo: {Formatar(saldoAntes)} → {Formatar(saldoDepois)} {produto.UnidadeMedida}"
            + (movimentacao.Observacao is null ? "" : $"\nObservação: {movimentacao.Observacao}");
        auditoria.Registrar(nameof(Produto), produto.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(), detalhe);

        await db.SaveChangesAsync();

        var criada = await ComIncludes().FirstAsync(m => m.Id == movimentacao.Id);
        return CreatedAtAction(nameof(Listar), criada.ToDto());
    }

    private static string Formatar(decimal valor) => valor.ToString("0.###", PtBr);

    private IQueryable<MovimentacaoEstoque> ComIncludes() =>
        db.MovimentacoesEstoque.Include(m => m.Produto).Include(m => m.Fornecedor).Include(m => m.Usuario);
}
