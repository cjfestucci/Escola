using Escola.Api.Auth;
using Escola.Api.Dtos;
using Escola.Api.Dtos.Requests;
using Escola.Api.Servicos;
using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Escola.Infrastructure.Auditoria;
using Escola.Infrastructure.Auth;
using Escola.Infrastructure.Data;
using Escola.Infrastructure.Financeiro;
using Escola.Infrastructure.Tempo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Escola.Api.Controllers;

[ApiController]
[Route("api/alunos")]
[Authorize]
public class AlunosController(
    EscolaDbContext db, IAuditoriaService auditoria, IRelogioEscola relogio, IBloqueioAlunoService bloqueio, IConviteMatriculaService conviteMatricula)
    : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = GruposDePapeis.Equipe)]
    public async Task<ActionResult<List<AlunoDto>>> Listar([FromQuery] Guid? turmaId)
    {
        var query = db.Alunos.Include(a => a.Turma).AsQueryable();

        if (turmaId is not null)
            query = query.Where(a => a.TurmaId == turmaId);

        var alunos = await query.OrderBy(a => a.Nome).ToListAsync();
        var ids = alunos.Select(a => a.Id).ToList();
        var bloqueados = await bloqueio.ObterBloqueadosAsync(ids);
        var atestados = await db.FichasSaude
            .Where(f => ids.Contains(f.AlunoId) && f.AtestadoValidoAte != null)
            .ToDictionaryAsync(f => f.AlunoId, f => f.AtestadoValidoAte);
        return Ok(alunos.Select(a => a.ToDto(bloqueados.Contains(a.Id), atestados.GetValueOrDefault(a.Id))));
    }

    /// <summary>Equipe pode ver qualquer aluno; um Responsável só o(s) próprio(s) filho(s).</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{GruposDePapeis.Equipe},{GruposDePapeis.Responsavel}")]
    public async Task<ActionResult<AlunoDetalheDto>> ObterPorId(Guid id)
    {
        if (User.IsInRole("Responsavel"))
        {
            var responsavelId = this.ResponsavelIdAtual();
            var ehFilho = await db.AlunoResponsaveis.AnyAsync(ar => ar.AlunoId == id && ar.ResponsavelId == responsavelId);
            if (!ehFilho) return Forbid();
        }

        var aluno = await ComIncludes().FirstOrDefaultAsync(a => a.Id == id);
        return aluno is null ? NotFound("Aluno não encontrado.") : Ok(await DetalheAsync(aluno));
    }

    [HttpPost]
    [Authorize(Roles = GruposDePapeis.Gestao)]
    public async Task<ActionResult<AlunoDetalheDto>> Criar(CriarOuEditarAlunoRequest request)
    {
        var erro = await ValidarAsync(request);
        if (erro is not null) return BadRequest(erro);

        var aluno = new Aluno
        {
            Id = Guid.NewGuid(),
            Nome = request.Nome.Trim(),
            DataNascimento = request.DataNascimento,
            FotoUrl = request.FotoUrl,
            TurmaId = request.TurmaId,
            Posicao = request.Posicao,
            DescontoMensalidadePercentual = request.DescontoMensalidadePercentual,
            MotivoDesconto = string.IsNullOrWhiteSpace(request.MotivoDesconto) ? null : request.MotivoDesconto.Trim()
        };
        db.Alunos.Add(aluno);

        await SincronizarResponsaveisAsync(aluno, request.Responsaveis);
        auditoria.Registrar(nameof(Aluno), aluno.Id, AcaoAuditoria.Criado, this.UsuarioIdAtual(), $"{aluno.Nome} (matrícula aguardando o aceite do responsável)");
        await db.SaveChangesAsync();

        // Depois de salvar: as contas novas precisam existir pro link de convite.
        var convites = await conviteMatricula.EnviarAsync(aluno.Id);
        var criado = await ComIncludes().FirstAsync(a => a.Id == aluno.Id);
        return CreatedAtAction(nameof(ObterPorId), new { id = aluno.Id }, await DetalheAsync(criado) with { Convites = convites });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = GruposDePapeis.Gestao)]
    public async Task<ActionResult<AlunoDetalheDto>> Editar(Guid id, CriarOuEditarAlunoRequest request)
    {
        var erro = await ValidarAsync(request);
        if (erro is not null) return BadRequest(erro);

        var aluno = await ComIncludes().FirstOrDefaultAsync(a => a.Id == id);
        if (aluno is null) return NotFound("Aluno não encontrado.");

        var nomeAntes = aluno.Nome;
        var dataNascimentoAntes = aluno.DataNascimento;
        var turmaIdAntes = aluno.TurmaId;
        var turmaNomeAntes = aluno.Turma.Nome;
        var posicaoAntes = aluno.Posicao?.Rotulo();
        var descontoAntes = aluno.DescontoMensalidadePercentual;
        var motivoDescontoAntes = aluno.MotivoDesconto;

        aluno.DescontoMensalidadePercentual = request.DescontoMensalidadePercentual;
        aluno.MotivoDesconto = string.IsNullOrWhiteSpace(request.MotivoDesconto) ? null : request.MotivoDesconto.Trim();
        aluno.Nome = request.Nome.Trim();
        aluno.DataNascimento = request.DataNascimento;
        aluno.FotoUrl = request.FotoUrl;
        aluno.TurmaId = request.TurmaId;
        aluno.Posicao = request.Posicao;

        var responsaveisNovos = await SincronizarResponsaveisAsync(aluno, request.Responsaveis);

        var turmaNomeDepois = request.TurmaId == turmaIdAntes
            ? turmaNomeAntes
            : (await db.Turmas.FindAsync(request.TurmaId))?.Nome ?? "desconhecida";

        var detalhe = AuditoriaDetalhe.MontarAlteracoes(
            ("Nome", nomeAntes, aluno.Nome),
            ("Data de nascimento", dataNascimentoAntes, aluno.DataNascimento),
            ("Turma", turmaNomeAntes, turmaNomeDepois),
            ("Posição", posicaoAntes, aluno.Posicao?.Rotulo()),
            ("Desconto na mensalidade (%)", descontoAntes, aluno.DescontoMensalidadePercentual),
            ("Motivo do desconto", motivoDescontoAntes, aluno.MotivoDesconto));

        auditoria.Registrar(nameof(Aluno), aluno.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(), detalhe);
        await db.SaveChangesAsync();

        // Quem passou a ser responsável agora recebe o e-mail (convite ou aviso) — o termo aparece pra ele no portal.
        var convites = responsaveisNovos.Count > 0 ? await conviteMatricula.EnviarAsync(aluno.Id, responsaveisNovos) : [];
        var editado = await ComIncludes().FirstAsync(a => a.Id == aluno.Id);
        return Ok(await DetalheAsync(editado) with { Convites = convites });
    }

    [HttpPost("{id:guid}/desativar")]
    [Authorize(Roles = GruposDePapeis.Gestao)]
    public async Task<ActionResult<AlunoDetalheDto>> Desativar(Guid id)
    {
        var aluno = await db.Alunos.FirstOrDefaultAsync(a => a.Id == id);
        if (aluno is null) return NotFound("Aluno não encontrado.");

        aluno.Ativo = false;
        auditoria.Registrar(nameof(Aluno), aluno.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(), $"Desativado: {aluno.Nome}");
        await db.SaveChangesAsync();

        var atualizado = await ComIncludes().FirstAsync(a => a.Id == id);
        return Ok(await DetalheAsync(atualizado));
    }

    [HttpPost("{id:guid}/ativar")]
    [Authorize(Roles = GruposDePapeis.Gestao)]
    public async Task<ActionResult<AlunoDetalheDto>> Ativar(Guid id)
    {
        var aluno = await db.Alunos.FirstOrDefaultAsync(a => a.Id == id);
        if (aluno is null) return NotFound("Aluno não encontrado.");

        aluno.Ativo = true;
        auditoria.Registrar(nameof(Aluno), aluno.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(), $"Reativado: {aluno.Nome}");
        await db.SaveChangesAsync();

        var atualizado = await ComIncludes().FirstAsync(a => a.Id == id);
        return Ok(await DetalheAsync(atualizado));
    }

    /// <summary>Manda de novo o e-mail da matrícula a um responsável: convite (conta ainda não ativada) ou aviso pra entrar no portal.</summary>
    [HttpPost("{id:guid}/responsaveis/{responsavelId:guid}/reenviar-convite")]
    [Authorize(Roles = GruposDePapeis.Gestao)]
    public async Task<ActionResult<ConviteMatriculaDto>> ReenviarConvite(Guid id, Guid responsavelId)
    {
        if (!await db.AlunoResponsaveis.AnyAsync(ar => ar.AlunoId == id && ar.ResponsavelId == responsavelId))
            return NotFound("Responsável não encontrado neste aluno.");

        var resultado = (await conviteMatricula.EnviarAsync(id, [responsavelId])).First();
        auditoria.Registrar(nameof(Aluno), id, AcaoAuditoria.Editado, this.UsuarioIdAtual(),
            $"E-mail da matrícula reenviado para {resultado.Nome} ({resultado.Email}){(resultado.Entregue ? string.Empty : " — não entregue")}");
        await db.SaveChangesAsync();
        return Ok(resultado);
    }

    private async Task<bool> EstaBloqueadoAsync(Guid alunoId) => (await bloqueio.ObterBloqueadosAsync([alunoId])).Contains(alunoId);

    private async Task<AlunoDetalheDto> DetalheAsync(Aluno aluno)
    {
        var responsavelIds = aluno.Responsaveis.Select(r => r.ResponsavelId).ToList();
        var pendentes = (await db.Usuarios
            .Where(u => u.ResponsavelId != null && responsavelIds.Contains(u.ResponsavelId.Value) && u.SenhaHash == SenhaHasher.ConvitePendente)
            .Select(u => u.ResponsavelId!.Value)
            .ToListAsync()).ToHashSet();
        return aluno.ToDetalheDto(await EstaBloqueadoAsync(aluno.Id), pendentes);
    }

    private async Task<string?> ValidarAsync(CriarOuEditarAlunoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
            return "Nome é obrigatório.";

        if (request.DataNascimento == default || request.DataNascimento > await relogio.HojeAsync())
            return "Data de nascimento inválida.";

        if (!await db.Turmas.AnyAsync(t => t.Id == request.TurmaId))
            return "Turma inválida.";

        if (request.Posicao is { } posicao && !Enum.IsDefined(posicao))
            return "Posição inválida.";

        if (request.DescontoMensalidadePercentual is < 0 or > 100)
            return "O desconto na mensalidade deve estar entre 0% e 100%.";

        if (request.Responsaveis.Count == 0)
            return "Informe ao menos um responsável.";

        if (request.Responsaveis.Any(r => string.IsNullOrWhiteSpace(r.Nome) || string.IsNullOrWhiteSpace(r.Email)))
            return "Nome e e-mail são obrigatórios para cada responsável.";

        return null;
    }

    /// <returns>Os responsáveis que passaram a ser vinculados a este aluno agora (recebem o e-mail da matrícula).</returns>
    private async Task<List<Guid>> SincronizarResponsaveisAsync(Aluno aluno, List<ResponsavelInput> inputs)
    {
        var vinculosAtuais = aluno.Responsaveis.ToList();
        var responsavelIdsMantidos = new HashSet<Guid>();
        var novosVinculos = new List<Guid>();

        foreach (var input in inputs)
        {
            var vinculoExistente = input.Id is { } id ? vinculosAtuais.FirstOrDefault(v => v.ResponsavelId == id) : null;

            if (vinculoExistente is not null)
            {
                vinculoExistente.Responsavel.Nome = input.Nome.Trim();
                vinculoExistente.Responsavel.Email = input.Email.Trim();
                vinculoExistente.Responsavel.Telefone = input.Telefone;
                vinculoExistente.ResponsavelFinanceiro = input.ResponsavelFinanceiro;
                responsavelIdsMantidos.Add(vinculoExistente.ResponsavelId);
                continue;
            }

            // Sem Id: pode ser gente nova, ou um responsável que já existe no sistema (ex.: irmão
            // já matriculado com o mesmo responsável). Reaproveita pelo e-mail em vez de duplicar.
            var email = input.Email.Trim();
            var responsavel = await db.Responsaveis.FirstOrDefaultAsync(r => r.Email.ToLower() == email.ToLower());
            if (responsavel is null)
            {
                responsavel = new Responsavel { Id = Guid.NewGuid(), Nome = input.Nome.Trim(), Email = email, Telefone = input.Telefone };
                db.Responsaveis.Add(responsavel);

                // Responsável novo: também cria o login dele (Portal dos Pais), a não ser que o e-mail
                // já pertença a uma conta existente (ex.: alguém da equipe que também é responsável).
                if (!await db.Usuarios.AnyAsync(u => u.Email.ToLower() == email.ToLower()))
                {
                    // Sem senha: a conta nasce com convite pendente e o responsável cria a própria senha pelo link do e-mail
                    // (que também confirma o e-mail). A Gestão ainda pode gerar uma senha pela Matrícula se o e-mail não chegar.
                    db.Usuarios.Add(new Usuario
                    {
                        Id = Guid.NewGuid(), Nome = responsavel.Nome, Email = responsavel.Email,
                        SenhaHash = SenhaHasher.ConvitePendente, Papel = PapelUsuario.Responsavel, ResponsavelId = responsavel.Id
                    });
                }
            }

            if (vinculosAtuais.All(v => v.ResponsavelId != responsavel.Id))
            {
                novosVinculos.Add(responsavel.Id);
                db.AlunoResponsaveis.Add(new AlunoResponsavel
                {
                    AlunoId = aluno.Id,
                    ResponsavelId = responsavel.Id,
                    ResponsavelFinanceiro = input.ResponsavelFinanceiro
                });
            }

            responsavelIdsMantidos.Add(responsavel.Id);
        }

        var vinculosRemover = vinculosAtuais.Where(v => !responsavelIdsMantidos.Contains(v.ResponsavelId)).ToList();
        if (vinculosRemover.Count > 0)
            db.AlunoResponsaveis.RemoveRange(vinculosRemover);

        return novosVinculos;
    }

    private IQueryable<Aluno> ComIncludes() =>
        db.Alunos.Include(a => a.Turma).Include(a => a.Responsaveis).ThenInclude(ar => ar.Responsavel);
}
