using Escola.Api.Auth;
using Escola.Api.Dtos;
using Escola.Api.Dtos.Requests;
using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Escola.Infrastructure.Auditoria;
using Escola.Infrastructure.Competicoes;
using Escola.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Escola.Api.Controllers;

[ApiController]
[Route("api/campeonatos")]
[Authorize(Roles = GruposDePapeis.Equipe)]
public class CampeonatosController(EscolaDbContext db, IAuditoriaService auditoria, IDisciplinaService disciplina) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CampeonatoDto>>> Listar()
    {
        var campeonatos = await db.Campeonatos.Include(c => c.Jogos).OrderByDescending(c => c.DataInicio).ThenBy(c => c.Nome).ToListAsync();
        return Ok(campeonatos.Select(c => c.ToDto()));
    }

    /// <summary>Detalhe com o desempenho do NOSSO time (por turma) e os artilheiros — só contam jogos realizados.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CampeonatoDto>> ObterPorId(Guid id)
    {
        var campeonato = await db.Campeonatos.Include(c => c.Jogos).FirstOrDefaultAsync(c => c.Id == id);
        if (campeonato is null) return NotFound("Campeonato não encontrado.");

        var realizados = await db.Jogos
            .Include(j => j.Turma)
            .Where(j => j.CampeonatoId == id && j.Status == StatusJogo.Realizado)
            .ToListAsync();

        var desempenho = realizados
            .GroupBy(j => new { j.TurmaId, j.Turma.Nome })
            .Select(g =>
            {
                var vitorias = g.Count(j => j.GolsPro > j.GolsContra);
                var empates = g.Count(j => j.GolsPro == j.GolsContra);
                return new DesempenhoDto(
                    g.Key.TurmaId, g.Key.Nome, g.Count(), vitorias, empates, g.Count(j => j.GolsPro < j.GolsContra),
                    g.Sum(j => j.GolsPro ?? 0), g.Sum(j => j.GolsContra ?? 0), vitorias * 3 + empates);
            })
            .OrderByDescending(d => d.Pontos).ThenBy(d => d.TurmaNome)
            .ToList();

        var comGols = await db.JogoAtletas
            .Include(ja => ja.Aluno)
            .Where(ja => ja.Jogo.CampeonatoId == id && ja.Jogo.Status == StatusJogo.Realizado && ja.Gols > 0)
            .ToListAsync();

        var artilheiros = comGols
            .GroupBy(ja => new { ja.AlunoId, ja.Aluno.Nome })
            .Select(g => new ArtilheiroDto(g.Key.AlunoId, g.Key.Nome, g.Sum(ja => ja.Gols)))
            .OrderByDescending(a => a.Gols).ThenBy(a => a.AlunoNome)
            .Take(10)
            .ToList();

        var alertas = (await disciplina.CarregarAsync(id)).Atual().Select(a => a.ToDto()).ToList();

        return Ok(campeonato.ToDto() with { Desempenho = desempenho, Artilheiros = artilheiros, Disciplina = alertas });
    }

    [HttpPost]
    [Authorize(Roles = GruposDePapeis.Gestao)]
    public async Task<ActionResult<CampeonatoDto>> Criar(CriarOuEditarCampeonatoRequest request)
    {
        var erro = Validar(request);
        if (erro is not null) return BadRequest(erro);

        var campeonato = new Campeonato
        {
            Id = Guid.NewGuid(),
            Nome = request.Nome.Trim(),
            DataInicio = request.DataInicio,
            DataFim = request.DataFim,
            Observacao = string.IsNullOrWhiteSpace(request.Observacao) ? null : request.Observacao.Trim(),
            AmarelosParaSuspensao = request.AmarelosParaSuspensao,
            RegistradoEm = DateTime.UtcNow
        };
        db.Campeonatos.Add(campeonato);
        auditoria.Registrar(nameof(Campeonato), campeonato.Id, AcaoAuditoria.Criado, this.UsuarioIdAtual(), campeonato.Nome);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(ObterPorId), new { id = campeonato.Id }, campeonato.ToDto());
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = GruposDePapeis.Gestao)]
    public async Task<ActionResult<CampeonatoDto>> Editar(Guid id, CriarOuEditarCampeonatoRequest request)
    {
        var erro = Validar(request);
        if (erro is not null) return BadRequest(erro);

        var campeonato = await db.Campeonatos.Include(c => c.Jogos).FirstOrDefaultAsync(c => c.Id == id);
        if (campeonato is null) return NotFound("Campeonato não encontrado.");

        var nomeAntes = campeonato.Nome;
        var inicioAntes = campeonato.DataInicio;
        var fimAntes = campeonato.DataFim;
        var observacaoAntes = campeonato.Observacao;
        var amarelosAntes = campeonato.AmarelosParaSuspensao;

        campeonato.Nome = request.Nome.Trim();
        campeonato.DataInicio = request.DataInicio;
        campeonato.DataFim = request.DataFim;
        campeonato.Observacao = string.IsNullOrWhiteSpace(request.Observacao) ? null : request.Observacao.Trim();
        campeonato.AmarelosParaSuspensao = request.AmarelosParaSuspensao;

        var detalhe = AuditoriaDetalhe.MontarAlteracoes(
            ("Nome", nomeAntes, campeonato.Nome),
            ("Data de início", inicioAntes, campeonato.DataInicio),
            ("Data de término", fimAntes, campeonato.DataFim),
            ("Observação", observacaoAntes, campeonato.Observacao),
            ("Amarelos para suspensão", amarelosAntes, campeonato.AmarelosParaSuspensao));

        auditoria.Registrar(nameof(Campeonato), campeonato.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(), detalhe);
        await db.SaveChangesAsync();

        return Ok(campeonato.ToDto());
    }

    [HttpPost("{id:guid}/desativar")]
    [Authorize(Roles = GruposDePapeis.Gestao)]
    public async Task<ActionResult<CampeonatoDto>> Desativar(Guid id) => await AlterarStatusAsync(id, false, "Desativado");

    [HttpPost("{id:guid}/ativar")]
    [Authorize(Roles = GruposDePapeis.Gestao)]
    public async Task<ActionResult<CampeonatoDto>> Ativar(Guid id) => await AlterarStatusAsync(id, true, "Reativado");

    private async Task<ActionResult<CampeonatoDto>> AlterarStatusAsync(Guid id, bool ativo, string rotulo)
    {
        var campeonato = await db.Campeonatos.Include(c => c.Jogos).FirstOrDefaultAsync(c => c.Id == id);
        if (campeonato is null) return NotFound("Campeonato não encontrado.");

        campeonato.Ativo = ativo;
        auditoria.Registrar(nameof(Campeonato), campeonato.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(), $"{rotulo}: {campeonato.Nome}");
        await db.SaveChangesAsync();

        return Ok(campeonato.ToDto());
    }

    private static string? Validar(CriarOuEditarCampeonatoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nome)) return "Nome é obrigatório.";
        if (request.Nome.Trim().Length > 150) return "O nome pode ter no máximo 150 caracteres.";
        if (request.DataInicio == default) return "Informe a data de início.";
        if (request.DataFim is { } fim && fim < request.DataInicio) return "A data de término não pode ser anterior à de início.";
        if (request.Observacao is { Length: > 500 }) return "A observação pode ter no máximo 500 caracteres.";
        if (request.AmarelosParaSuspensao is < 1 or > 10)
            return "Os cartões amarelos para suspensão devem estar entre 1 e 10 (ou fique em branco para não controlar).";
        return null;
    }
}
