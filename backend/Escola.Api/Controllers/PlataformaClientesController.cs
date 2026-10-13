using Escola.Api.Auth;
using Escola.Api.Servicos;
using Escola.Domain.Enums;
using Escola.Infrastructure.Assinaturas;
using Escola.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Escola.Api.Controllers;

/// <param name="Situacao">Nula = cliente sem assinatura (criado à mão).</param>
/// <param name="UltimoAcessoEm">Último login/uso da equipe ou das famílias (não conta o Suporte).</param>
public record ClienteResumoDto(
    Guid Id, string Nome, SegmentoCliente Segmento, bool Ativo, DateTime CriadoEm,
    string? Situacao, string? RotuloSituacao, decimal? ValorMensal, DateOnly? TesteAte, DateOnly? VencimentoEmAberto,
    DateOnly? AcessoAte, DateOnly? DataExclusao, string? EmailCobranca,
    int AtletasAtivos, int ContasEquipe, DateTime? UltimoAcessoEm);

/// <summary>Visão de todos os clientes do produto, pra equipe do produto (Suporte) acompanhar assinaturas e uso. Só <b>contagens</b> e dados
/// da assinatura — nada de aluno, saúde ou financeiro do clube (o privilégio mínimo do Suporte continua). É a única consulta do app que
/// atravessa clientes de propósito (<c>IgnoreQueryFilters</c>); pra agir num cliente, o Suporte entra nele escolhendo a escola no login.</summary>
[ApiController]
[Route("api/plataforma/clientes")]
[Authorize(Roles = GruposDePapeis.Suporte)]
public class PlataformaClientesController(EscolaDbContext db, IConfiguration config, IOptions<OpcoesAssinatura> opcoes) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ClienteResumoDto>>> Listar(CancellationToken ct)
    {
        var clientes = await db.Clientes.AsNoTracking().OrderBy(c => c.Nome).ToListAsync(ct);

        var assinaturas = await db.Assinaturas.IgnoreQueryFilters().AsNoTracking()
            .Select(a => new { ClienteId = EF.Property<Guid>(a, EscolaDbContext.ColunaCliente), Assinatura = a })
            .ToDictionaryAsync(x => x.ClienteId, x => x.Assinatura, ct);

        var atletas = await db.Alunos.IgnoreQueryFilters().Where(a => a.Ativo)
            .GroupBy(a => EF.Property<Guid>(a, EscolaDbContext.ColunaCliente))
            .Select(g => new { ClienteId = g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.ClienteId, x => x.Total, ct);

        var equipe = await db.Usuarios.IgnoreQueryFilters()
            .Where(u => u.Ativo && u.Papel != PapelUsuario.Responsavel && u.Papel != PapelUsuario.Suporte)
            .GroupBy(u => EF.Property<Guid>(u, EscolaDbContext.ColunaCliente))
            .Select(g => new { ClienteId = g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.ClienteId, x => x.Total, ct);

        var emailSuporte = (config["Suporte:Email"] ?? string.Empty).ToLower();
        var ultimoAcesso = await db.RegistrosAcesso.IgnoreQueryFilters()
            .Where(r => r.Email.ToLower() != emailSuporte)
            .GroupBy(r => EF.Property<Guid>(r, EscolaDbContext.ColunaCliente))
            .Select(g => new { ClienteId = g.Key, Ultimo = g.Max(r => r.OcorridoEm) })
            .ToDictionaryAsync(x => x.ClienteId, x => (DateTime?)x.Ultimo, ct);

        var dias = opcoes.Value.DiasRetencaoAposSuspensao;
        return Ok(clientes.Select(c =>
        {
            var a = assinaturas.GetValueOrDefault(c.Id);
            return new ClienteResumoDto(
                c.Id, c.Nome, c.Segmento, c.Ativo, c.CriadoEm,
                a?.Situacao.ToString(), a is null ? null : AssinaturaService.Rotulo(a.Situacao), a?.ValorMensal, a?.TesteAte,
                a?.VencimentoEmAberto, a?.AcessoAte,
                a is null ? null : SituacaoAssinaturaCalculo.DataExclusao(a.Situacao, a.AcessoAte, a.SuspensaEm, a.JaPagou, dias),
                a?.EmailCobranca,
                atletas.GetValueOrDefault(c.Id), equipe.GetValueOrDefault(c.Id), ultimoAcesso.GetValueOrDefault(c.Id));
        }).ToList());
    }
}
