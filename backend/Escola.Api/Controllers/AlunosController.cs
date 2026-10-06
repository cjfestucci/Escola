using Escola.Api.Auth;
using Escola.Api.Dtos;
using Escola.Api.Dtos.Requests;
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
public class AlunosController(EscolaDbContext db, IAuditoriaService auditoria, IRelogioEscola relogio, IBloqueioAlunoService bloqueio) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = GruposDePapeis.Equipe)]
    public async Task<ActionResult<List<AlunoDto>>> Listar([FromQuery] Guid? turmaId)
    {
        var query = db.Alunos.Include(a => a.Turma).AsQueryable();

        if (turmaId is not null)
            query = query.Where(a => a.TurmaId == turmaId);

        var alunos = await query.OrderBy(a => a.Nome).ToListAsync();
        var bloqueados = await bloqueio.ObterBloqueadosAsync(alunos.Select(a => a.Id).ToList());
        return Ok(alunos.Select(a => a.ToDto(bloqueados.Contains(a.Id))));
    }

    /// <summary>Equipe pode ver qualquer aluno; um Responsável só o(s) próprio(s) filho(s).</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{GruposDePapeis.Equipe},{GruposDePapeis.Responsavel}")]
    public async Task<ActionResult<AlunoDetalheDto>> ObterPorId(Guid id)
    {
        if (User.IsInRole("Responsavel"))
        {
            var responsavelId = User.FindFirst("responsavelId")?.Value;
            var ehFilho = await db.AlunoResponsaveis.AnyAsync(ar => ar.AlunoId == id && ar.ResponsavelId.ToString() == responsavelId);
            if (!ehFilho) return Forbid();
        }

        var aluno = await ComIncludes().FirstOrDefaultAsync(a => a.Id == id);
        return aluno is null ? NotFound("Aluno não encontrado.") : Ok(aluno.ToDetalheDto(await EstaBloqueadoAsync(id)));
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

        var senhasGeradas = await SincronizarResponsaveisAsync(aluno, request.Responsaveis);
        auditoria.Registrar(nameof(Aluno), aluno.Id, AcaoAuditoria.Criado, this.UsuarioIdAtual(), aluno.Nome);
        await db.SaveChangesAsync();

        var criado = await ComIncludes().FirstAsync(a => a.Id == aluno.Id);
        return CreatedAtAction(nameof(ObterPorId), new { id = aluno.Id }, criado.ToDetalheDto() with { SenhasGeradas = senhasGeradas });    }

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

        var senhasGeradas = await SincronizarResponsaveisAsync(aluno, request.Responsaveis);

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

        var editado = await ComIncludes().FirstAsync(a => a.Id == aluno.Id);
        return Ok(editado.ToDetalheDto(await EstaBloqueadoAsync(id)) with { SenhasGeradas = senhasGeradas });
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
        return Ok(atualizado.ToDetalheDto(await EstaBloqueadoAsync(id)));
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
        return Ok(atualizado.ToDetalheDto(await EstaBloqueadoAsync(id)));
    }

    private async Task<bool> EstaBloqueadoAsync(Guid alunoId) => (await bloqueio.ObterBloqueadosAsync([alunoId])).Contains(alunoId);

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

    private async Task<List<SenhaGeradaDto>> SincronizarResponsaveisAsync(Aluno aluno, List<ResponsavelInput> inputs)
    {
        var vinculosAtuais = aluno.Responsaveis.ToList();
        var responsavelIdsMantidos = new HashSet<Guid>();
        var senhasGeradas = new List<SenhaGeradaDto>();

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
                    var senha = GeradorSenhaTemporaria.Gerar();
                    db.Usuarios.Add(new Usuario
                    {
                        Id = Guid.NewGuid(), Nome = responsavel.Nome, Email = responsavel.Email,
                        SenhaHash = SenhaHasher.Hash(senha), Papel = PapelUsuario.Responsavel, ResponsavelId = responsavel.Id
                    });
                    senhasGeradas.Add(new SenhaGeradaDto(responsavel.Nome, responsavel.Email, senha));
                }
            }

            if (vinculosAtuais.All(v => v.ResponsavelId != responsavel.Id))
            {
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

        return senhasGeradas;
    }

    private IQueryable<Aluno> ComIncludes() =>
        db.Alunos.Include(a => a.Turma).Include(a => a.Responsaveis).ThenInclude(ar => ar.Responsavel);
}
