using Escola.Api.Auth;
using Escola.Api.Dtos;
using Escola.Api.Dtos.Requests;
using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Escola.Infrastructure.Auditoria;
using Escola.Infrastructure.Data;
using Escola.Infrastructure.Frequencia;
using Escola.Infrastructure.Tempo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Escola.Api.Controllers;

/// <summary>Chamada (presença) por turma e dia, e a frequência derivada dela. Quem faz a chamada é a Equipe inteira, sem
/// checar se o Educador é da turma (mesma lacuna aceita de Diário/Rotina). O Responsável só lê a frequência do próprio filho.</summary>
[ApiController]
[Route("api")]
[Authorize]
public class PresencaController(EscolaDbContext db, IAuditoriaService auditoria, IRelogioEscola relogio) : ControllerBase
{
    /// <summary>Janela máxima olhada pra "faltas seguidas" — evita varrer o histórico inteiro de um aluno.</summary>
    private const int DiasParaFaltasSeguidas = 90;

    [HttpGet("turmas/{turmaId:guid}/presenca")]
    [Authorize(Roles = GruposDePapeis.Equipe)]
    public async Task<ActionResult<ChamadaDto>> ObterChamada(Guid turmaId, [FromQuery] DateOnly data)
    {
        if (!await db.Turmas.AnyAsync(t => t.Id == turmaId)) return NotFound("Turma não encontrada.");
        return Ok(await MontarChamadaAsync(turmaId, data));
    }

    /// <summary>Salva a chamada do dia (substitui a lista inteira). Cada mudança vira uma linha no histórico do dia da turma.</summary>
    [HttpPut("turmas/{turmaId:guid}/presenca")]
    [Authorize(Roles = GruposDePapeis.Equipe)]
    public async Task<ActionResult<ChamadaDto>> SalvarChamada(Guid turmaId, SalvarChamadaRequest request)
    {
        if (!await db.Turmas.AnyAsync(t => t.Id == turmaId)) return NotFound("Turma não encontrada.");

        if (request.Data == default || request.Data > await relogio.HojeAsync())
            return BadRequest("A chamada não pode ser de uma data futura.");

        if (request.Itens.Any(i => !Enum.IsDefined(i.Status)))
            return BadRequest("Situação de presença inválida.");

        if (request.Itens.GroupBy(i => i.AlunoId).Any(g => g.Count() > 1))
            return BadRequest("Aluno repetido na chamada.");

        var alunosDaTurma = await db.Alunos
            .Where(a => a.TurmaId == turmaId && a.Ativo)
            .ToDictionaryAsync(a => a.Id, a => a.Nome);

        var existentes = await db.RegistrosPresenca
            .Where(r => r.TurmaId == turmaId && r.Data == request.Data)
            .Include(r => r.Aluno)
            .ToListAsync();
        var existentesPorAluno = existentes.ToDictionary(r => r.AlunoId);

        var estranho = request.Itens.FirstOrDefault(i => !alunosDaTurma.ContainsKey(i.AlunoId) && !existentesPorAluno.ContainsKey(i.AlunoId));
        if (estranho is not null) return BadRequest("A chamada tem um aluno que não é (ou não está ativo) nesta turma.");

        // O índice único é (aluno, dia): se o aluno já tem presença hoje em OUTRA turma, melhor avisar do que estourar no banco.
        var idsDoRequest = request.Itens.Select(i => i.AlunoId).ToList();
        var emOutraTurma = await db.RegistrosPresenca
            .Where(r => r.Data == request.Data && r.TurmaId != turmaId && idsDoRequest.Contains(r.AlunoId))
            .Select(r => r.Aluno.Nome)
            .FirstOrDefaultAsync();
        if (emOutraTurma is not null)
            return BadRequest($"{emOutraTurma} já tem presença registrada em outra turma nesse dia.");

        var usuarioId = this.UsuarioIdAtual();
        var linhas = new List<string>();
        var primeiraChamada = existentes.Count == 0;
        var idsMantidos = new HashSet<Guid>();

        foreach (var item in request.Itens)
        {
            idsMantidos.Add(item.AlunoId);
            var nome = alunosDaTurma.TryGetValue(item.AlunoId, out var n) ? n : existentesPorAluno[item.AlunoId].Aluno.Nome;

            if (existentesPorAluno.TryGetValue(item.AlunoId, out var registro))
            {
                if (registro.Status == item.Status) continue;
                linhas.Add($"{nome}: {registro.Status.Rotulo()} → {item.Status.Rotulo()}");
                registro.Status = item.Status;
                registro.UsuarioId = usuarioId;
                registro.RegistradoEm = DateTime.UtcNow;
            }
            else
            {
                db.RegistrosPresenca.Add(new RegistroPresenca
                {
                    Id = Guid.NewGuid(),
                    TurmaId = turmaId,
                    AlunoId = item.AlunoId,
                    Data = request.Data,
                    Status = item.Status,
                    UsuarioId = usuarioId,
                    RegistradoEm = DateTime.UtcNow
                });
                if (!primeiraChamada) linhas.Add($"{nome}: {item.Status.Rotulo()} (incluído na chamada)");
            }
        }

        foreach (var removido in existentes.Where(r => !idsMantidos.Contains(r.AlunoId)))
        {
            linhas.Add($"{removido.Aluno.Nome}: removido da chamada (era {removido.Status.Rotulo()})");
            db.RegistrosPresenca.Remove(removido);
        }

        if (primeiraChamada)
        {
            if (request.Itens.Count == 0) return BadRequest("Informe a presença de ao menos um aluno.");
            var res = FrequenciaCalculo.Resumir(request.Itens.Select(i => i.Status));
            linhas.Insert(0, $"Chamada registrada: {res.Presencas} {(res.Presencas == 1 ? "presente" : "presentes")}, {res.Faltas} {(res.Faltas == 1 ? "falta" : "faltas")}, {res.Justificadas} {(res.Justificadas == 1 ? "justificada" : "justificadas")}");
        }

        // Salvar sem mudar nada não vira entrada de histórico (só poluiria a trilha).
        if (linhas.Count > 0)
        {
            auditoria.Registrar("Presenca", turmaId, primeiraChamada ? AcaoAuditoria.Criado : AcaoAuditoria.Editado, usuarioId,
                string.Join('\n', linhas), turmaId, request.Data);
        }

        await db.SaveChangesAsync();
        return Ok(await MontarChamadaAsync(turmaId, request.Data));
    }

    /// <summary>Frequência de todos os alunos ativos da turma num período — alimenta a tela de chamada, a convocação e o alerta de
    /// faltosos. O período é <c>de</c>/<c>ate</c> (datas) ou, sem elas, os últimos <c>dias</c>. "Faltas seguidas" não depende do período:
    /// é sempre sobre as chamadas mais recentes.</summary>
    [HttpGet("turmas/{turmaId:guid}/frequencia")]
    [Authorize(Roles = GruposDePapeis.Equipe)]
    public async Task<ActionResult<List<FrequenciaAlunoDto>>> FrequenciaDaTurma(
        Guid turmaId, [FromQuery] int dias = FrequenciaCalculo.JanelaPadraoDias, [FromQuery] DateOnly? de = null, [FromQuery] DateOnly? ate = null)
    {
        if (!await db.Turmas.AnyAsync(t => t.Id == turmaId)) return NotFound("Turma não encontrada.");

        var hoje = await relogio.HojeAsync();
        var (inicio, fim, erro) = ResolverPeriodo(dias, de, ate, hoje);
        if (erro is not null) return BadRequest(erro);

        var alunos = await db.Alunos.Where(a => a.TurmaId == turmaId && a.Ativo).OrderBy(a => a.Nome)
            .Select(a => new { a.Id, a.Nome }).ToListAsync();
        var registros = await RegistrosRecentesAsync(alunos.Select(a => a.Id).ToList(), inicio, hoje);

        return Ok(alunos.Select(a => Resumo(a.Id, a.Nome, registros.GetValueOrDefault(a.Id) ?? [], inicio, fim)).ToList());
    }

    /// <summary>Frequência de um aluno num período (<c>de</c>/<c>ate</c> ou os últimos <c>dias</c>) e as chamadas desse período.
    /// Equipe vê qualquer aluno; um Responsável só o(s) próprio(s) filho(s).</summary>
    [HttpGet("alunos/{alunoId:guid}/frequencia")]
    [Authorize(Roles = $"{GruposDePapeis.Equipe},{GruposDePapeis.Responsavel}")]
    public async Task<ActionResult<FrequenciaDoAlunoDto>> FrequenciaDoAluno(
        Guid alunoId, [FromQuery] int dias = FrequenciaCalculo.JanelaPadraoDias, [FromQuery] DateOnly? de = null, [FromQuery] DateOnly? ate = null)
    {
        var aluno = await db.Alunos.Where(a => a.Id == alunoId).Select(a => new { a.Id, a.Nome }).FirstOrDefaultAsync();
        if (aluno is null) return NotFound("Aluno não encontrado.");

        if (User.IsInRole("Responsavel"))
        {
            var responsavelId = this.ResponsavelIdAtual();
            var ehFilho = await db.AlunoResponsaveis.AnyAsync(ar => ar.AlunoId == alunoId && ar.ResponsavelId == responsavelId);
            if (!ehFilho) return Forbid();
        }

        var hoje = await relogio.HojeAsync();
        var (inicio, fim, erro) = ResolverPeriodo(dias, de, ate, hoje);
        if (erro is not null) return BadRequest(erro);

        var registros = (await RegistrosRecentesAsync([alunoId], inicio, hoje)).GetValueOrDefault(alunoId) ?? [];
        var resumo = Resumo(aluno.Id, aluno.Nome, registros, inicio, fim);

        var doPeriodo = registros.Where(r => r.Data >= inicio && r.Data <= fim)
            .OrderByDescending(r => r.Data).Take(MaxChamadasListadas)
            .Select(r => new PresencaRecenteDto(r.Data, r.Status)).ToList();
        return Ok(new FrequenciaDoAlunoDto(resumo, fim.DayNumber - inicio.DayNumber + 1, doPeriodo, inicio, fim));
    }

    /// <summary>Alunos ativos (de turma ativa) com pelo menos <paramref name="minimo"/> faltas seguidas — alerta do Dashboard.</summary>
    [HttpGet("presenca/faltosos")]
    [Authorize(Roles = GruposDePapeis.Equipe)]
    public async Task<ActionResult<List<FaltosoDto>>> Faltosos([FromQuery] int minimo = FrequenciaCalculo.FaltasSeguidasParaAlerta)
    {
        minimo = Math.Clamp(minimo, 1, 30);
        var alunos = await db.Alunos.Where(a => a.Ativo && a.Turma.Ativa)
            .Select(a => new { a.Id, a.Nome, a.TurmaId, TurmaNome = a.Turma.Nome }).ToListAsync();
        var hoje = await relogio.HojeAsync();
        var registros = await RegistrosRecentesAsync(alunos.Select(a => a.Id).ToList(), hoje, hoje);

        var faltosos = alunos
            .Select(a => new FaltosoDto(a.Id, a.Nome, a.TurmaId, a.TurmaNome,
                FrequenciaCalculo.FaltasSeguidas((registros.GetValueOrDefault(a.Id) ?? []).Select(r => (r.Data, r.Status)))))
            .Where(f => f.FaltasSeguidas >= minimo)
            .OrderByDescending(f => f.FaltasSeguidas).ThenBy(f => f.AlunoNome)
            .ToList();
        return Ok(faltosos);
    }

    // ----- helpers -----

    /// <summary>Quantas chamadas do período são devolvidas na lista do aluno (o resumo considera todas).</summary>
    private const int MaxChamadasListadas = 60;

    /// <summary>Maior período aceito (dias corridos).</summary>
    private const int MaxDiasPeriodo = 366;

    /// <summary>Período a considerar: <c>de</c>/<c>ate</c> quando informados (<c>ate</c> nunca passa de hoje); senão os últimos <c>dias</c>.
    /// Só <c>ate</c> = os <c>dias</c> que terminam nele. Devolve o erro (em pt-BR) quando o período não faz sentido.</summary>
    private static (DateOnly Inicio, DateOnly Fim, string? Erro) ResolverPeriodo(int dias, DateOnly? de, DateOnly? ate, DateOnly hoje)
    {
        dias = Math.Clamp(dias, 1, 365);
        var fim = ate is { } a ? (a > hoje ? hoje : a) : hoje;
        var inicio = de ?? fim.AddDays(-(dias - 1));

        if (inicio > fim) return (inicio, fim, "A data inicial não pode ser depois da data final.");
        if (fim.DayNumber - inicio.DayNumber + 1 > MaxDiasPeriodo) return (inicio, fim, $"O período pode ter no máximo {MaxDiasPeriodo} dias.");
        return (inicio, fim, null);
    }

    private record RegistroLeve(DateOnly Data, StatusPresenca Status);

    /// <summary>Registros dos últimos 90 dias (o bastante pro percentual de 30 dias e pras faltas seguidas), por aluno.</summary>
    private async Task<Dictionary<Guid, List<RegistroLeve>>> RegistrosRecentesAsync(List<Guid> alunoIds, DateOnly inicioPeriodo, DateOnly hoje)
    {
        // Busca o bastante pro período pedido E pras faltas seguidas (que olham os últimos 90 dias, seja qual for o período).
        var desde = new[] { inicioPeriodo, hoje.AddDays(-DiasParaFaltasSeguidas) }.Min();
        var linhas = await db.RegistrosPresenca
            .Where(r => r.Data >= desde && alunoIds.Contains(r.AlunoId))
            .Select(r => new { r.AlunoId, r.Data, r.Status })
            .ToListAsync();
        return linhas.GroupBy(l => l.AlunoId).ToDictionary(g => g.Key, g => g.Select(l => new RegistroLeve(l.Data, l.Status)).ToList());
    }

    private static FrequenciaAlunoDto Resumo(Guid alunoId, string nome, List<RegistroLeve> registros, DateOnly inicio, DateOnly fim)
    {
        var res = FrequenciaCalculo.Resumir(registros.Where(r => r.Data >= inicio && r.Data <= fim).Select(r => r.Status));
        // Sempre sobre as chamadas mais recentes (não sobre o período escolhido): é um alerta de "agora".
        var seguidas = FrequenciaCalculo.FaltasSeguidas(registros.Select(r => (r.Data, r.Status)));
        return new FrequenciaAlunoDto(alunoId, nome, res.Presencas, res.Faltas, res.Justificadas, res.Percentual, seguidas);
    }

    private async Task<ChamadaDto> MontarChamadaAsync(Guid turmaId, DateOnly data)
    {
        var registros = await db.RegistrosPresenca.Where(r => r.TurmaId == turmaId && r.Data == data).ToListAsync();
        var porAluno = registros.ToDictionary(r => r.AlunoId);

        // Alunos ativos da turma + quem já tem registro no dia (ex.: foi desativado depois da chamada).
        var alunos = await db.Alunos
            .Where(a => a.TurmaId == turmaId && (a.Ativo || registros.Select(r => r.AlunoId).Contains(a.Id)))
            .OrderBy(a => a.Nome)
            .Select(a => new { a.Id, a.Nome, a.FotoUrl })
            .ToListAsync();

        var itens = alunos.Select(a => new ChamadaItemDto(a.Id, a.Nome, a.FotoUrl, porAluno.TryGetValue(a.Id, out var r) ? r.Status : null)).ToList();
        return new ChamadaDto(
            turmaId, data, registros.Count > 0, itens,
            registros.Count(r => r.Status == StatusPresenca.Presente),
            registros.Count(r => r.Status == StatusPresenca.Falta),
            registros.Count(r => r.Status == StatusPresenca.Justificada));
    }
}
