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
[Route("api/financeiro/contas-receber")]
[Authorize(Roles = GruposDePapeis.Financeiro)]
public class ContasReceberController(EscolaDbContext db, IAuditoriaService auditoria, IRelogioEscola relogio) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ContaReceberDto>>> Listar([FromQuery] bool? recebida)
    {
        var query = db.ContasReceber.AsQueryable();

        if (recebida is { } r) query = query.Where(c => c.Recebida == r);

        var contas = await query.OrderBy(c => c.Vencimento).ToListAsync();
        return Ok(contas.Select(c => c.ToDto()));
    }

    [HttpPost]
    public async Task<ActionResult<ContaReceberDto>> Criar(CriarOuEditarContaReceberRequest request)
    {
        var erro = Validar(request.Descricao, request.Valor);
        if (erro is not null) return BadRequest(erro);

        var conta = new ContaReceber
        {
            Id = Guid.NewGuid(),
            Descricao = request.Descricao.Trim(),
            Origem = string.IsNullOrWhiteSpace(request.Origem) ? null : request.Origem.Trim(),
            Valor = request.Valor,
            Vencimento = request.Vencimento,
            RegistradoEm = DateTime.UtcNow
        };
        db.ContasReceber.Add(conta);
        auditoria.Registrar(nameof(ContaReceber), conta.Id, AcaoAuditoria.Criado, this.UsuarioIdAtual(), conta.Descricao);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(Listar), conta.ToDto());
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ContaReceberDto>> Editar(Guid id, CriarOuEditarContaReceberRequest request)
    {
        var erro = Validar(request.Descricao, request.Valor);
        if (erro is not null) return BadRequest(erro);

        var conta = await db.ContasReceber.FirstOrDefaultAsync(c => c.Id == id);
        if (conta is null) return NotFound("Conta a receber não encontrada.");

        if (conta.Cancelada)
            return BadRequest("Não é possível editar uma conta cancelada.");

        var descricaoAntes = conta.Descricao;
        var origemAntes = conta.Origem;
        var valorAntes = conta.Valor;
        var vencimentoAntes = conta.Vencimento;

        conta.Descricao = request.Descricao.Trim();
        conta.Origem = string.IsNullOrWhiteSpace(request.Origem) ? null : request.Origem.Trim();
        conta.Valor = request.Valor;
        conta.Vencimento = request.Vencimento;

        var detalhe = AuditoriaDetalhe.MontarAlteracoes(
            ("Descrição", descricaoAntes, conta.Descricao),
            ("Origem", origemAntes, conta.Origem),
            ("Valor", valorAntes, conta.Valor),
            ("Vencimento", vencimentoAntes, conta.Vencimento));

        auditoria.Registrar(nameof(ContaReceber), conta.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(), detalhe);
        await db.SaveChangesAsync();

        return Ok(conta.ToDto());
    }

    [HttpPost("{id:guid}/marcar-recebida")]
    public async Task<ActionResult<ContaReceberDto>> MarcarRecebida(Guid id)
    {
        var conta = await db.ContasReceber.FirstOrDefaultAsync(c => c.Id == id);
        if (conta is null) return NotFound("Conta a receber não encontrada.");

        if (conta.Cancelada)
            return BadRequest("Não é possível marcar como recebida uma conta cancelada.");

        conta.Recebida = true;
        conta.RecebidoEm = await relogio.HojeAsync();
        auditoria.Registrar(nameof(ContaReceber), conta.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(), $"Marcada como recebida: {conta.Descricao}");
        await db.SaveChangesAsync();

        return Ok(conta.ToDto());
    }

    [HttpPost("{id:guid}/desmarcar-recebida")]
    public async Task<ActionResult<ContaReceberDto>> DesmarcarRecebida(Guid id)
    {
        var conta = await db.ContasReceber.FirstOrDefaultAsync(c => c.Id == id);
        if (conta is null) return NotFound("Conta a receber não encontrada.");

        conta.Recebida = false;
        conta.RecebidoEm = null;
        auditoria.Registrar(nameof(ContaReceber), conta.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(), $"Marcada como pendente: {conta.Descricao}");
        await db.SaveChangesAsync();

        return Ok(conta.ToDto());
    }

    [HttpPost("{id:guid}/cancelar")]
    public async Task<ActionResult<ContaReceberDto>> Cancelar(Guid id)
    {
        var conta = await db.ContasReceber.FirstOrDefaultAsync(c => c.Id == id);
        if (conta is null) return NotFound("Conta a receber não encontrada.");

        if (conta.Recebida)
            return BadRequest("Não é possível cancelar uma conta já recebida — desmarque o recebimento antes.");

        conta.Cancelada = true;
        conta.CanceladaEm = await relogio.HojeAsync();
        auditoria.Registrar(nameof(ContaReceber), conta.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(), $"Cancelada: {conta.Descricao}");
        await db.SaveChangesAsync();

        return Ok(conta.ToDto());
    }

    [HttpPost("{id:guid}/reabrir")]
    public async Task<ActionResult<ContaReceberDto>> Reabrir(Guid id)
    {
        var conta = await db.ContasReceber.FirstOrDefaultAsync(c => c.Id == id);
        if (conta is null) return NotFound("Conta a receber não encontrada.");

        conta.Cancelada = false;
        conta.CanceladaEm = null;
        auditoria.Registrar(nameof(ContaReceber), conta.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(), $"Reaberta: {conta.Descricao}");
        await db.SaveChangesAsync();

        return Ok(conta.ToDto());
    }

    private static string? Validar(string descricao, decimal valor)
    {
        if (string.IsNullOrWhiteSpace(descricao))
            return "Informe uma descrição.";

        if (valor <= 0)
            return "O valor deve ser maior que zero.";

        return null;
    }
}
