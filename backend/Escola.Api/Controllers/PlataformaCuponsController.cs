using Escola.Api.Auth;
using Escola.Domain.Entities;
using Escola.Infrastructure.Assinaturas;
using Escola.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Escola.Api.Controllers;

public record CupomDto(
    Guid Id, string Codigo, int PercentualDesconto, int? Meses, DateOnly? ValidoAte, int? UsosMaximos, int Usos,
    bool Ativo, string? Observacao, string Descricao, DateTime CriadoEm);

public record SalvarCupomRequest(string? Codigo, int PercentualDesconto, int? Meses, DateOnly? ValidoAte, int? UsosMaximos, string? Observacao);

/// <summary>Cupons de desconto da assinatura — do produto, não de um cliente (só o Suporte). Mudar ou desativar um cupom não mexe em quem já
/// assinou com ele (o desconto foi copiado pra assinatura). Sem exclusão: desativa. Sem histórico de auditoria (o log é por cliente e o cupom
/// não é de cliente nenhum): as mudanças vão pro log da aplicação.</summary>
[ApiController]
[Route("api/plataforma/cupons")]
[Authorize(Roles = GruposDePapeis.Suporte)]
public class PlataformaCuponsController(EscolaDbContext db, ILogger<PlataformaCuponsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CupomDto>>> Listar(CancellationToken ct)
    {
        var cupons = await db.Cupons.AsNoTracking().OrderByDescending(c => c.Ativo).ThenBy(c => c.Codigo).ToListAsync(ct);
        var usos = await UsosAsync(ct);
        return Ok(cupons.Select(c => ParaDto(c, usos.GetValueOrDefault(c.Codigo))).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<CupomDto>> Criar(SalvarCupomRequest request, CancellationToken ct)
    {
        var codigo = CupomRegras.Normalizar(request.Codigo);
        if (codigo.Length is < 3 or > 30) return BadRequest("O código precisa ter de 3 a 30 letras, números, - ou _.");
        if (await db.Cupons.AnyAsync(c => c.Codigo == codigo, ct)) return BadRequest("Já existe um cupom com esse código.");
        var erro = Validar(request);
        if (erro is not null) return BadRequest(erro);

        var cupom = new Cupom { Id = Guid.NewGuid(), Codigo = codigo, CriadoEm = DateTime.UtcNow };
        Aplicar(cupom, request);
        db.Cupons.Add(cupom);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Cupom {Codigo} criado pelo Suporte {Usuario}: {Descricao}.", cupom.Codigo, this.UsuarioIdAtual(),
            CupomRegras.Descricao(cupom.PercentualDesconto, cupom.Meses));
        return Ok(ParaDto(cupom, 0));
    }

    /// <summary>O código não muda (quem assinou guarda o código); o resto vale pros próximos cadastros.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CupomDto>> Editar(Guid id, SalvarCupomRequest request, CancellationToken ct)
    {
        var cupom = await db.Cupons.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (cupom is null) return NotFound("Cupom não encontrado.");
        var erro = Validar(request);
        if (erro is not null) return BadRequest(erro);

        Aplicar(cupom, request);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Cupom {Codigo} editado pelo Suporte {Usuario}.", cupom.Codigo, this.UsuarioIdAtual());
        return Ok(ParaDto(cupom, (await UsosAsync(ct)).GetValueOrDefault(cupom.Codigo)));
    }

    [HttpPost("{id:guid}/desativar")]
    public Task<ActionResult<CupomDto>> Desativar(Guid id, CancellationToken ct) => DefinirAtivoAsync(id, false, ct);

    [HttpPost("{id:guid}/ativar")]
    public Task<ActionResult<CupomDto>> Ativar(Guid id, CancellationToken ct) => DefinirAtivoAsync(id, true, ct);

    private async Task<ActionResult<CupomDto>> DefinirAtivoAsync(Guid id, bool ativo, CancellationToken ct)
    {
        var cupom = await db.Cupons.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (cupom is null) return NotFound("Cupom não encontrado.");
        cupom.Ativo = ativo;
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Cupom {Codigo} {Acao} pelo Suporte {Usuario}.", cupom.Codigo, ativo ? "ativado" : "desativado", this.UsuarioIdAtual());
        return Ok(ParaDto(cupom, (await UsosAsync(ct)).GetValueOrDefault(cupom.Codigo)));
    }

    private static string? Validar(SalvarCupomRequest r)
    {
        if (r.PercentualDesconto is < 1 or > 100) return "O desconto precisa ser de 1% a 100%.";
        if (r.Meses is < 1 or > 120) return "Informe de 1 a 120 meses, ou deixe vazio pra valer sempre.";
        if (r.UsosMaximos is < 1) return "O limite de usos precisa ser pelo menos 1, ou vazio pra não ter limite.";
        if (r.Observacao?.Length > 300) return "A observação pode ter no máximo 300 caracteres.";
        return null;
    }

    private static void Aplicar(Cupom c, SalvarCupomRequest r)
    {
        c.PercentualDesconto = r.PercentualDesconto;
        c.Meses = r.Meses;
        c.ValidoAte = r.ValidoAte;
        c.UsosMaximos = r.UsosMaximos;
        c.Observacao = string.IsNullOrWhiteSpace(r.Observacao) ? null : r.Observacao.Trim();
    }

    private Task<Dictionary<string, int>> UsosAsync(CancellationToken ct) =>
        db.Assinaturas.IgnoreQueryFilters().Where(a => a.CupomCodigo != null)
            .GroupBy(a => a.CupomCodigo!)
            .Select(g => new { Codigo = g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.Codigo, x => x.Total, ct);

    private static CupomDto ParaDto(Cupom c, int usos) =>
        new(c.Id, c.Codigo, c.PercentualDesconto, c.Meses, c.ValidoAte, c.UsosMaximos, usos, c.Ativo, c.Observacao,
            CupomRegras.Descricao(c.PercentualDesconto, c.Meses), c.CriadoEm);
}
