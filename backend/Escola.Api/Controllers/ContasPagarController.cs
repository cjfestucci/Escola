using Escola.Api.Auth;
using Escola.Api.Dtos;
using Escola.Api.Dtos.Requests;
using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Escola.Infrastructure.Auditoria;
using Escola.Infrastructure.Data;
using Escola.Infrastructure.Tempo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Escola.Api.Controllers;

[ApiController]
[Route("api/financeiro/contas-pagar")]
[Authorize(Roles = GruposDePapeis.Financeiro)]
public class ContasPagarController(EscolaDbContext db, IAuditoriaService auditoria, IRelogioEscola relogio) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ContaPagarDto>>> Listar([FromQuery] Guid? fornecedorId, [FromQuery] bool? paga)
    {
        var query = ComIncludes();

        if (fornecedorId is { } f) query = query.Where(c => c.FornecedorId == f);
        if (paga is { } p) query = query.Where(c => c.Paga == p);

        var contas = await query.OrderBy(c => c.Vencimento).ToListAsync();
        return Ok(contas.Select(c => c.ToDto()));
    }

    [HttpPost]
    public async Task<ActionResult<ContaPagarDto>> Criar(CriarContaPagarRequest request)
    {
        var erro = Validar(request.Descricao, request.Valor);
        if (erro is not null) return BadRequest(erro);

        if (!await db.Fornecedores.AnyAsync(f => f.Id == request.FornecedorId))
            return BadRequest("Fornecedor inválido.");

        var conta = new ContaPagar
        {
            Id = Guid.NewGuid(),
            FornecedorId = request.FornecedorId,
            Descricao = request.Descricao.Trim(),
            Valor = request.Valor,
            Vencimento = request.Vencimento,
            RegistradoEm = DateTime.UtcNow
        };
        db.ContasPagar.Add(conta);
        auditoria.Registrar(nameof(ContaPagar), conta.Id, AcaoAuditoria.Criado, this.UsuarioIdAtual(), conta.Descricao);
        await db.SaveChangesAsync();

        var criada = await ComIncludes().FirstAsync(c => c.Id == conta.Id);
        return CreatedAtAction(nameof(Listar), criada.ToDto());
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ContaPagarDto>> Editar(Guid id, EditarContaPagarRequest request)
    {
        var erro = Validar(request.Descricao, request.Valor);
        if (erro is not null) return BadRequest(erro);

        if (!await db.Fornecedores.AnyAsync(f => f.Id == request.FornecedorId))
            return BadRequest("Fornecedor inválido.");

        var conta = await ComIncludes().FirstOrDefaultAsync(c => c.Id == id);
        if (conta is null) return NotFound("Conta a pagar não encontrada.");

        if (conta.Cancelada)
            return BadRequest("Não é possível editar uma conta cancelada.");

        var fornecedorIdAntes = conta.FornecedorId;
        var fornecedorNomeAntes = conta.Fornecedor.Nome;
        var descricaoAntes = conta.Descricao;
        var valorAntes = conta.Valor;
        var vencimentoAntes = conta.Vencimento;

        conta.FornecedorId = request.FornecedorId;
        conta.Descricao = request.Descricao.Trim();
        conta.Valor = request.Valor;
        conta.Vencimento = request.Vencimento;

        var fornecedorNomeDepois = request.FornecedorId == fornecedorIdAntes
            ? fornecedorNomeAntes
            : (await db.Fornecedores.FindAsync(request.FornecedorId))?.Nome ?? "desconhecido";

        var detalhe = AuditoriaDetalhe.MontarAlteracoes(
            ("Fornecedor", fornecedorNomeAntes, fornecedorNomeDepois),
            ("Descrição", descricaoAntes, conta.Descricao),
            ("Valor", valorAntes, conta.Valor),
            ("Vencimento", vencimentoAntes, conta.Vencimento));

        auditoria.Registrar(nameof(ContaPagar), conta.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(), detalhe);
        await db.SaveChangesAsync();

        var editada = await ComIncludes().FirstAsync(c => c.Id == id);
        return Ok(editada.ToDto());
    }

    [HttpPost("{id:guid}/marcar-paga")]
    public async Task<ActionResult<ContaPagarDto>> MarcarPaga(Guid id)
    {
        var conta = await db.ContasPagar.FirstOrDefaultAsync(c => c.Id == id);
        if (conta is null) return NotFound("Conta a pagar não encontrada.");

        if (conta.Cancelada)
            return BadRequest("Não é possível marcar como paga uma conta cancelada.");

        conta.Paga = true;
        conta.PagoEm = await relogio.HojeAsync();
        auditoria.Registrar(nameof(ContaPagar), conta.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(), $"Marcada como paga: {conta.Descricao}");
        await db.SaveChangesAsync();

        var atualizada = await ComIncludes().FirstAsync(c => c.Id == id);
        return Ok(atualizada.ToDto());
    }

    [HttpPost("{id:guid}/desmarcar-paga")]
    public async Task<ActionResult<ContaPagarDto>> DesmarcarPaga(Guid id)
    {
        var conta = await db.ContasPagar.FirstOrDefaultAsync(c => c.Id == id);
        if (conta is null) return NotFound("Conta a pagar não encontrada.");

        conta.Paga = false;
        conta.PagoEm = null;
        auditoria.Registrar(nameof(ContaPagar), conta.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(), $"Marcada como pendente: {conta.Descricao}");
        await db.SaveChangesAsync();

        var atualizada = await ComIncludes().FirstAsync(c => c.Id == id);
        return Ok(atualizada.ToDto());
    }

    [HttpPost("{id:guid}/cancelar")]
    public async Task<ActionResult<ContaPagarDto>> Cancelar(Guid id)
    {
        var conta = await db.ContasPagar.FirstOrDefaultAsync(c => c.Id == id);
        if (conta is null) return NotFound("Conta a pagar não encontrada.");

        if (conta.Paga)
            return BadRequest("Não é possível cancelar uma conta já paga — desmarque o pagamento antes.");

        conta.Cancelada = true;
        conta.CanceladaEm = await relogio.HojeAsync();
        auditoria.Registrar(nameof(ContaPagar), conta.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(), $"Cancelada: {conta.Descricao}");
        await db.SaveChangesAsync();

        var atualizada = await ComIncludes().FirstAsync(c => c.Id == id);
        return Ok(atualizada.ToDto());
    }

    [HttpPost("{id:guid}/reabrir")]
    public async Task<ActionResult<ContaPagarDto>> Reabrir(Guid id)
    {
        var conta = await db.ContasPagar.FirstOrDefaultAsync(c => c.Id == id);
        if (conta is null) return NotFound("Conta a pagar não encontrada.");

        conta.Cancelada = false;
        conta.CanceladaEm = null;
        auditoria.Registrar(nameof(ContaPagar), conta.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(), $"Reaberta: {conta.Descricao}");
        await db.SaveChangesAsync();

        var atualizada = await ComIncludes().FirstAsync(c => c.Id == id);
        return Ok(atualizada.ToDto());
    }

    private static string? Validar(string descricao, decimal valor)
    {
        if (string.IsNullOrWhiteSpace(descricao))
            return "Informe uma descrição.";

        if (valor <= 0)
            return "O valor deve ser maior que zero.";

        return null;
    }

    private IQueryable<ContaPagar> ComIncludes() =>
        db.ContasPagar.Include(c => c.Fornecedor);
}
