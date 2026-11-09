using Escola.Api.Auth;
using Escola.Api.Dtos;
using Escola.Api.Dtos.Requests;
using Escola.Api.Servicos;
using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Escola.Infrastructure.Auditoria;
using Escola.Infrastructure.Clientes;
using Escola.Infrastructure.Competicoes;
using Escola.Infrastructure.Data;
using Escola.Infrastructure.Email;
using Escola.Infrastructure.Tempo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Escola.Api.Controllers;

/// <param name="Responsaveis">Quantos responsáveis receberam o e-mail.</param>
/// <param name="Falhas">Quantos e-mails não puderam ser enviados.</param>
public record DivulgacaoConvocacaoDto(JogoDto Jogo, int Responsaveis, int Falhas);

/// <summary>Toda a Equipe agenda, lança resultado e monta a convocação (o técnico precisa fazer isso no dia a dia);
/// só o cadastro de Campeonato é restrito à Gestão. Como no Diário de Classe, não se checa se o Educador é da turma.</summary>
[ApiController]
[Route("api/jogos")]
[Authorize(Roles = GruposDePapeis.Equipe)]
public class JogosController(
    EscolaDbContext db,
    IAuditoriaService auditoria,
    IRelogioEscola relogio,
    IDisciplinaService disciplina,
    IEmailSender email,
    IClienteAtual clienteAtual,
    IConfiguration config,
    ILogger<JogosController> logger) : ControllerBase
{
    /// <summary>Tempo mínimo entre duas divulgações do mesmo jogo (evita e-mail em dobro por duplo clique).</summary>
    private static readonly TimeSpan IntervaloEntreDivulgacoes = TimeSpan.FromMinutes(2);

    [HttpGet]
    public async Task<ActionResult<List<JogoDto>>> Listar([FromQuery] Guid? campeonatoId, [FromQuery] Guid? turmaId)
    {
        var query = ComIncludes();
        if (campeonatoId is { } c) query = query.Where(j => j.CampeonatoId == c);
        if (turmaId is { } t) query = query.Where(j => j.TurmaId == t);

        var jogos = await query.OrderByDescending(j => j.Data).ThenByDescending(j => j.Hora).ToListAsync();

        // Quantidades de suspensos/pendurados só nos jogos agendados de campeonato; um carregamento por campeonato.
        var porCampeonato = new Dictionary<Guid, DisciplinaCampeonato>();
        foreach (var campeonato in jogos.Where(j => j.Status == StatusJogo.Agendado && j.CampeonatoId is not null).Select(j => j.CampeonatoId!.Value).Distinct())
            porCampeonato[campeonato] = await disciplina.CarregarAsync(campeonato);

        var dtos = jogos.Select(j =>
        {
            var dto = j.ToDto();
            if (j.Status != StatusJogo.Agendado || j.CampeonatoId is not { } cid) return dto;

            var alertas = porCampeonato[cid].ParaJogo(j.Id);
            return dto with
            {
                Suspensos = alertas.Count(a => a.Tipo == TipoAlertaDisciplinar.Suspenso),
                Pendurados = alertas.Count(a => a.Tipo == TipoAlertaDisciplinar.Pendurado)
            };
        });
        return Ok(dtos.ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JogoDto>> ObterPorId(Guid id)
    {
        var jogo = await ComIncludes(comAlunos: true).FirstOrDefaultAsync(j => j.Id == id);
        return jogo is null ? NotFound("Jogo não encontrado.") : Ok(await DetalheAsync(jogo));
    }

    [HttpPost]
    public async Task<ActionResult<JogoDto>> Criar(CriarOuEditarJogoRequest request)
    {
        if (!await this.PodeLancarNaTurmaAsync(db, request.TurmaId)) return this.ForaDasSuasTurmas();

        var (erro, turma, campeonato) = await ValidarAsync(request, null);
        if (erro is not null) return BadRequest(erro);

        var jogo = new Jogo
        {
            Id = Guid.NewGuid(),
            RegistradoEm = DateTime.UtcNow
        };
        Aplicar(jogo, request);
        db.Jogos.Add(jogo);

        auditoria.Registrar(nameof(Jogo), jogo.Id, AcaoAuditoria.Criado, this.UsuarioIdAtual(),
            $"{turma!.Nome} x {jogo.Adversario} em {jogo.Data:dd/MM/yyyy} {jogo.Hora:HH:mm}" + (campeonato is null ? " (amistoso)" : $" — {campeonato.Nome}"));
        await db.SaveChangesAsync();

        var criado = await ComIncludes(comAlunos: true).FirstAsync(j => j.Id == jogo.Id);
        return CreatedAtAction(nameof(ObterPorId), new { id = jogo.Id }, await DetalheAsync(criado));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<JogoDto>> Editar(Guid id, CriarOuEditarJogoRequest request)
    {
        var jogo = await ComIncludes(comAlunos: true).FirstOrDefaultAsync(j => j.Id == id);
        if (jogo is null) return NotFound("Jogo não encontrado.");
        if (!await this.PodeLancarNaTurmaAsync(db, jogo.TurmaId)) return this.ForaDasSuasTurmas();
        if (jogo.Status == StatusJogo.Cancelado) return BadRequest("Reabra o jogo cancelado antes de editá-lo.");

        var (erro, turma, campeonato) = await ValidarAsync(request, jogo);
        if (erro is not null) return BadRequest(erro);

        var campeonatoAntes = jogo.Campeonato?.Nome;
        var turmaAntes = jogo.Turma.Nome;
        var adversarioAntes = jogo.Adversario;
        var dataAntes = jogo.Data;
        var horaAntes = jogo.Hora;
        var localAntes = jogo.Local;
        var mandoAntes = jogo.Mando.Rotulo();
        var statusAntes = jogo.Status.Rotulo();
        var placarAntes = Placar(jogo);
        var observacaoAntes = jogo.Observacao;

        Aplicar(jogo, request);

        var detalhe = AuditoriaDetalhe.MontarAlteracoes(
            ("Campeonato", campeonatoAntes, campeonato?.Nome),
            ("Turma", turmaAntes, turma!.Nome),
            ("Adversário", adversarioAntes, jogo.Adversario),
            ("Data", dataAntes, jogo.Data),
            ("Hora", horaAntes, jogo.Hora),
            ("Local", localAntes, jogo.Local),
            ("Mando de campo", mandoAntes, jogo.Mando.Rotulo()),
            ("Situação", statusAntes, jogo.Status.Rotulo()),
            ("Placar", placarAntes, Placar(jogo)),
            ("Observação", observacaoAntes, jogo.Observacao));

        auditoria.Registrar(nameof(Jogo), jogo.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(), detalhe);
        await db.SaveChangesAsync();

        var editado = await ComIncludes(comAlunos: true).FirstAsync(j => j.Id == id);
        return Ok(await DetalheAsync(editado));
    }

    [HttpPost("{id:guid}/cancelar")]
    public async Task<ActionResult<JogoDto>> Cancelar(Guid id)
    {
        var jogo = await ComIncludes(comAlunos: true).FirstOrDefaultAsync(j => j.Id == id);
        if (jogo is null) return NotFound("Jogo não encontrado.");
        if (!await this.PodeLancarNaTurmaAsync(db, jogo.TurmaId)) return this.ForaDasSuasTurmas();
        if (jogo.Status == StatusJogo.Cancelado) return BadRequest("O jogo já está cancelado.");
        if (jogo.Status == StatusJogo.Realizado)
            return BadRequest("Um jogo realizado não pode ser cancelado — volte-o para Agendado (editando) antes, se foi lançado por engano.");

        jogo.Status = StatusJogo.Cancelado;
        auditoria.Registrar(nameof(Jogo), jogo.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(), $"Cancelado: {jogo.Turma.Nome} x {jogo.Adversario}");
        await db.SaveChangesAsync();

        return Ok(await DetalheAsync(jogo));
    }

    [HttpPost("{id:guid}/reabrir")]
    public async Task<ActionResult<JogoDto>> Reabrir(Guid id)
    {
        var jogo = await ComIncludes(comAlunos: true).FirstOrDefaultAsync(j => j.Id == id);
        if (jogo is null) return NotFound("Jogo não encontrado.");
        if (!await this.PodeLancarNaTurmaAsync(db, jogo.TurmaId)) return this.ForaDasSuasTurmas();
        if (jogo.Status != StatusJogo.Cancelado) return BadRequest("Só um jogo cancelado pode ser reaberto.");

        jogo.Status = StatusJogo.Agendado;
        auditoria.Registrar(nameof(Jogo), jogo.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(), $"Reaberto: {jogo.Turma.Nome} x {jogo.Adversario}");
        await db.SaveChangesAsync();

        return Ok(await DetalheAsync(jogo));
    }

    /// <summary>Substitui a convocação inteira (quem entra, quem sai e a participação de cada um na súmula).</summary>
    [HttpPut("{id:guid}/convocacao")]
    public async Task<ActionResult<JogoDto>> SalvarConvocacao(Guid id, SalvarConvocacaoRequest request)
    {
        var jogo = await ComIncludes(comAlunos: true).FirstOrDefaultAsync(j => j.Id == id);
        if (jogo is null) return NotFound("Jogo não encontrado.");
        if (!await this.PodeLancarNaTurmaAsync(db, jogo.TurmaId)) return this.ForaDasSuasTurmas();
        if (jogo.Status == StatusJogo.Cancelado) return BadRequest("Reabra o jogo cancelado antes de alterar a convocação.");

        var itens = request.Convocados ?? [];
        if (itens.GroupBy(i => i.AlunoId).Any(g => g.Count() > 1)) return BadRequest("Há um atleta repetido na convocação.");
        if (itens.Any(i => i.Gols is < 0 or > 99)) return BadRequest("Gols deve ser um número entre 0 e 99.");
        if (itens.Any(i => i.CartoesAmarelos is < 0 or > 2)) return BadRequest("Cartões amarelos deve ser 0, 1 ou 2.");

        var temEstatistica = itens.Any(i => i.Gols > 0 || i.CartoesAmarelos > 0 || i.CartaoVermelho);
        if (temEstatistica && jogo.Status != StatusJogo.Realizado)
            return BadRequest("Gols e cartões só podem ser lançados em um jogo realizado.");

        var golsAtletas = itens.Sum(i => i.Gols);
        if (jogo.Status == StatusJogo.Realizado && golsAtletas > jogo.GolsPro)
            return BadRequest($"Os gols dos atletas ({golsAtletas}) passam do placar ({jogo.GolsPro} gols pró).");

        var ids = itens.Select(i => i.AlunoId).ToList();
        var alunos = await db.Alunos.Where(a => ids.Contains(a.Id)).ToDictionaryAsync(a => a.Id);
        if (alunos.Count != ids.Count) return BadRequest("Há um atleta inválido na convocação.");

        var existentes = jogo.Convocados.ToDictionary(c => c.AlunoId);
        if (itens.Any(i => !existentes.ContainsKey(i.AlunoId) && !alunos[i.AlunoId].Ativo))
            return BadRequest("Não é possível convocar um atleta inativo.");

        var linhas = new List<string>();

        foreach (var removido in existentes.Values.Where(e => !ids.Contains(e.AlunoId)).ToList())
        {
            linhas.Add($"Removido da convocação: {removido.Aluno.Nome}");
            db.JogoAtletas.Remove(removido);
        }

        foreach (var item in itens)
        {
            var nome = alunos[item.AlunoId].Nome;
            if (existentes.TryGetValue(item.AlunoId, out var atual))
            {
                var mudancas = AuditoriaDetalhe.MontarAlteracoes(
                    ("Gols", atual.Gols, item.Gols),
                    ("Cartões amarelos", atual.CartoesAmarelos, item.CartoesAmarelos),
                    ("Cartão vermelho", atual.CartaoVermelho, item.CartaoVermelho));
                if (mudancas is not null) linhas.AddRange(mudancas.Split('\n').Select(l => $"{nome}: {l}"));

                atual.Gols = item.Gols;
                atual.CartoesAmarelos = item.CartoesAmarelos;
                atual.CartaoVermelho = item.CartaoVermelho;
            }
            else
            {
                linhas.Add($"Convocado: {nome}");
                db.JogoAtletas.Add(new JogoAtleta
                {
                    Id = Guid.NewGuid(),
                    JogoId = jogo.Id,
                    AlunoId = item.AlunoId,
                    Gols = item.Gols,
                    CartoesAmarelos = item.CartoesAmarelos,
                    CartaoVermelho = item.CartaoVermelho
                });
            }
        }

        if (linhas.Count > 0)
        {
            jogo.ConvocacaoAlteradaEm = DateTime.UtcNow;
            auditoria.Registrar(nameof(Jogo), jogo.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(), string.Join("\n", linhas));
        }
        await db.SaveChangesAsync();

        var atualizado = await ComIncludes(comAlunos: true).FirstAsync(j => j.Id == id);
        return Ok(await DetalheAsync(atualizado));
    }

    /// <summary>Avisa por e-mail os responsáveis dos atletas da turma (convocados e não convocados) da convocação deste jogo. Cada
    /// responsável recebe só a situação dos próprios filhos. O técnico decide o momento (não é automático a cada edição) e pode repetir
    /// se a lista mudar depois. O Portal da Família já mostra a convocação desde que ela é salva.</summary>
    [HttpPost("{id:guid}/convocacao/divulgar")]
    public async Task<ActionResult<DivulgacaoConvocacaoDto>> DivulgarConvocacao(Guid id, CancellationToken ct)
    {
        var jogo = await ComIncludes(comAlunos: true).FirstOrDefaultAsync(j => j.Id == id, ct);
        if (jogo is null) return NotFound("Jogo não encontrado.");
        if (!await this.PodeLancarNaTurmaAsync(db, jogo.TurmaId)) return this.ForaDasSuasTurmas();
        if (jogo.Status != StatusJogo.Agendado) return BadRequest("Só dá para divulgar a convocação de um jogo agendado.");
        if (jogo.Convocados.Count == 0) return BadRequest("Convoque ao menos um atleta e salve a convocação antes de divulgar.");
        if (!email.Configurado)
            return BadRequest("O envio de e-mail não está configurado neste ambiente. Os responsáveis continuam vendo a convocação no Portal da Família.");

        var agora = DateTime.UtcNow;
        if (jogo.ConvocacaoDivulgadaEm is { } ultima && agora - ultima < IntervaloEntreDivulgacoes)
            return BadRequest("A convocação acabou de ser divulgada. Aguarde um pouco antes de divulgar de novo.");

        // Atletas considerados: os ativos da turma do jogo + os convocados (mesmo de outra turma, jogando "por cima").
        var atletas = (await db.Alunos.Where(a => a.Ativo && a.TurmaId == jogo.TurmaId).Select(a => new { a.Id, a.Nome }).ToListAsync(ct))
            .ToDictionary(a => a.Id, a => a.Nome);
        foreach (var c in jogo.Convocados) atletas[c.AlunoId] = c.Aluno.Nome;
        var convocados = jogo.Convocados.ToDictionary(c => c.AlunoId);
        var ids = atletas.Keys.ToList();

        var vinculos = await db.AlunoResponsaveis
            .Where(ar => ids.Contains(ar.AlunoId) && ar.Responsavel.Email != "")
            .Select(ar => new { ar.AlunoId, ar.Responsavel.Nome, ar.Responsavel.Email })
            .ToListAsync(ct);
        if (vinculos.Count == 0) return BadRequest("Nenhum responsável com e-mail cadastrado para esses atletas.");

        var clube = await db.Clientes.AsNoTracking().Where(c => c.Id == clienteAtual.Id).Select(c => c.Nome).FirstAsync(ct);
        var urlBase = config["App:UrlBase"]?.TrimEnd('/');
        var urlPortal = string.IsNullOrWhiteSpace(urlBase) ? null : $"{urlBase}/portal";

        var enviados = 0;
        var falhas = 0;
        // Um e-mail por endereço: um responsável com dois filhos recebe uma mensagem só, com os dois.
        foreach (var grupo in vinculos.GroupBy(v => v.Email.Trim().ToLowerInvariant()))
        {
            var responsavel = grupo.First();
            var situacoes = grupo.Select(v => v.AlunoId).Distinct()
                .Select(alunoId => convocados.TryGetValue(alunoId, out var c)
                    ? new SituacaoNoJogo(atletas[alunoId], "convocado", true)
                    : new SituacaoNoJogo(atletas[alunoId], "não convocado para este jogo", false))
                .OrderBy(s => s.Atleta)
                .ToList();
            var (assunto, corpo) = EmailConvocacao.Montar(clube, responsavel.Nome, jogo, situacoes, urlPortal);
            try
            {
                await email.EnviarAsync(responsavel.Email, responsavel.Nome, assunto, corpo, ct);
                enviados++;
            }
            catch (Exception ex)
            {
                falhas++;
                logger.LogWarning(ex, "Não foi possível enviar a convocação do jogo {Jogo} para um responsável.", jogo.Id);
            }
        }

        if (enviados == 0) return BadRequest("Não foi possível enviar os e-mails agora. Tente de novo em alguns instantes.");

        jogo.ConvocacaoDivulgadaEm = agora;
        auditoria.Registrar(nameof(Jogo), jogo.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(),
            $"Convocação divulgada por e-mail: {enviados} {(enviados == 1 ? "responsável avisado" : "responsáveis avisados")}{(falhas > 0 ? $", {falhas} com falha" : string.Empty)}");
        await db.SaveChangesAsync(ct);

        return Ok(new DivulgacaoConvocacaoDto(await DetalheAsync(jogo), enviados, falhas));
    }

    private async Task<(string? Erro, Turma? Turma, Campeonato? Campeonato)> ValidarAsync(CriarOuEditarJogoRequest request, Jogo? existente)
    {
        if (string.IsNullOrWhiteSpace(request.Adversario)) return ("Informe o adversário.", null, null);
        if (request.Adversario.Trim().Length > 150) return ("O nome do adversário pode ter no máximo 150 caracteres.", null, null);
        if (request.Data == default) return ("Informe a data do jogo.", null, null);
        if (request.Local is { Length: > 150 }) return ("O local pode ter no máximo 150 caracteres.", null, null);
        if (request.Observacao is { Length: > 500 }) return ("A observação pode ter no máximo 500 caracteres.", null, null);
        if (!Enum.IsDefined(request.Mando)) return ("Mando de campo inválido.", null, null);
        if (!Enum.IsDefined(request.Status)) return ("Situação do jogo inválida.", null, null);
        if (request.Status == StatusJogo.Cancelado) return ("Para cancelar um jogo use a ação Cancelar.", null, null);

        var turma = await db.Turmas.FirstOrDefaultAsync(t => t.Id == request.TurmaId);
        if (turma is null) return ("Turma inválida.", null, null);
        if (!turma.Ativa && existente?.TurmaId != turma.Id) return ("A turma está inativa.", null, null);

        Campeonato? campeonato = null;
        if (request.CampeonatoId is { } campeonatoId)
        {
            campeonato = await db.Campeonatos.FirstOrDefaultAsync(c => c.Id == campeonatoId);
            if (campeonato is null) return ("Campeonato inválido.", null, null);
            if (!campeonato.Ativo && existente?.CampeonatoId != campeonato.Id) return ("O campeonato está inativo.", null, null);
        }

        if (request.Status == StatusJogo.Realizado)
        {
            if (request.Data > await relogio.HojeAsync()) return ("Um jogo só pode ser marcado como realizado na data do jogo ou depois dela.", null, null);
            if (request.GolsPro is null || request.GolsContra is null) return ("Informe o placar do jogo realizado.", null, null);
            if (request.GolsPro is < 0 or > 99 || request.GolsContra is < 0 or > 99) return ("O placar deve ter gols entre 0 e 99.", null, null);
        }
        else if (request.GolsPro is not null || request.GolsContra is not null)
        {
            return ("O placar só pode ser informado em um jogo realizado.", null, null);
        }

        if (existente is not null)
        {
            if (request.Status == StatusJogo.Realizado)
            {
                var golsAtletas = existente.Convocados.Sum(c => c.Gols);
                if (golsAtletas > request.GolsPro)
                    return ($"O placar ({request.GolsPro} gols pró) é menor que a soma dos gols dos atletas ({golsAtletas}) — ajuste a convocação antes.", null, null);
            }
            else if (existente.Convocados.Any(c => c.Gols > 0 || c.CartoesAmarelos > 0 || c.CartaoVermelho))
            {
                return ("Este jogo já tem gols ou cartões lançados na convocação; zere-os antes de voltá-lo para Agendado.", null, null);
            }
        }

        return (null, turma, campeonato);
    }

    private static void Aplicar(Jogo jogo, CriarOuEditarJogoRequest request)
    {
        jogo.CampeonatoId = request.CampeonatoId;
        jogo.TurmaId = request.TurmaId;
        jogo.Adversario = request.Adversario.Trim();
        jogo.Data = request.Data;
        jogo.Hora = request.Hora;
        jogo.Local = string.IsNullOrWhiteSpace(request.Local) ? null : request.Local.Trim();
        jogo.Mando = request.Mando;
        jogo.Status = request.Status;
        jogo.GolsPro = request.Status == StatusJogo.Realizado ? request.GolsPro : null;
        jogo.GolsContra = request.Status == StatusJogo.Realizado ? request.GolsContra : null;
        jogo.Observacao = string.IsNullOrWhiteSpace(request.Observacao) ? null : request.Observacao.Trim();
    }

    /// <summary>Detalhe com convocados e, num jogo agendado de campeonato, os alertas de suspensos/pendurados.</summary>
    private async Task<JogoDto> DetalheAsync(Jogo jogo)
    {
        var dto = jogo.ToDto(comConvocados: true);
        if (jogo.Status != StatusJogo.Agendado || jogo.CampeonatoId is not { } campeonatoId) return dto;

        var alertas = (await disciplina.CarregarAsync(campeonatoId)).ParaJogo(jogo.Id);
        return dto with
        {
            Alertas = alertas.Select(a => a.ToDto()).ToList(),
            Suspensos = alertas.Count(a => a.Tipo == TipoAlertaDisciplinar.Suspenso),
            Pendurados = alertas.Count(a => a.Tipo == TipoAlertaDisciplinar.Pendurado)
        };
    }

    private static string? Placar(Jogo jogo) => jogo.GolsPro is { } pro && jogo.GolsContra is { } contra ? $"{pro} x {contra}" : null;

    private IQueryable<Jogo> ComIncludes(bool comAlunos = false)
    {
        var query = db.Jogos.Include(j => j.Campeonato).Include(j => j.Turma);
        return comAlunos
            ? query.Include(j => j.Convocados).ThenInclude(c => c.Aluno)
            : query.Include(j => j.Convocados);
    }
}
