using Escola.Api.Auth;
using Escola.Api.Dtos;
using Escola.Api.Dtos.Requests;
using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Escola.Infrastructure.Auditoria;
using Escola.Infrastructure.Data;
using Escola.Infrastructure.Tempo;
using Escola.Infrastructure.Email;
using Escola.Infrastructure.Pagamentos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Escola.Api.Controllers;

[ApiController]
[Route("api/financeiro")]
[Authorize]
public class FinanceiroController(EscolaDbContext db, IEmailSender emailSender, IAuditoriaService auditoria, IRelogioEscola relogio) : ControllerBase
{
    /// <summary>Visão administrativa: todas as cobranças da escola, com filtros opcionais.</summary>
    [HttpGet("cobrancas")]
    [Authorize(Roles = GruposDePapeis.Financeiro)]
    public async Task<ActionResult<List<CobrancaDto>>> Listar(
        [FromQuery] Guid? turmaId, [FromQuery] Guid? alunoId, [FromQuery] bool? paga)
    {
        var query = ComIncludes();

        if (turmaId is { } t) query = query.Where(c => c.Aluno.TurmaId == t);
        if (alunoId is { } a) query = query.Where(c => c.AlunoId == a);
        if (paga is { } p) query = query.Where(c => c.Paga == p);

        var cobrancas = await query.OrderBy(c => c.Vencimento).ToListAsync();
        return Ok(cobrancas.Select(c => c.ToDto()));
    }

    [HttpPost("cobrancas")]
    [Authorize(Roles = GruposDePapeis.Financeiro)]
    public async Task<ActionResult<CobrancaDto>> Criar(CriarCobrancaRequest request)
    {
        var erro = Validar(request.Descricao, request.Valor);
        if (erro is not null) return BadRequest(erro);

        if (!await db.Alunos.AnyAsync(a => a.Id == request.AlunoId))
            return BadRequest("Aluno inválido.");

        var cobranca = new Cobranca
        {
            Id = Guid.NewGuid(),
            AlunoId = request.AlunoId,
            Descricao = request.Descricao.Trim(),
            Valor = request.Valor,
            Vencimento = request.Vencimento,
            RegistradoEm = DateTime.UtcNow
        };
        db.Cobrancas.Add(cobranca);
        auditoria.Registrar(nameof(Cobranca), cobranca.Id, AcaoAuditoria.Criado, this.UsuarioIdAtual(), cobranca.Descricao);
        await db.SaveChangesAsync();

        var criada = await ComIncludes().FirstAsync(c => c.Id == cobranca.Id);
        return CreatedAtAction(nameof(Listar), criada.ToDto());
    }

    [HttpPut("cobrancas/{id:guid}")]
    [Authorize(Roles = GruposDePapeis.Financeiro)]
    public async Task<ActionResult<CobrancaDto>> Editar(Guid id, EditarCobrancaRequest request)
    {
        var erro = Validar(request.Descricao, request.Valor);
        if (erro is not null) return BadRequest(erro);

        var cobranca = await db.Cobrancas.FirstOrDefaultAsync(c => c.Id == id);
        if (cobranca is null) return NotFound("Cobrança não encontrada.");

        if (cobranca.Cancelada)
            return BadRequest("Não é possível editar uma cobrança cancelada.");

        var descricaoAntes = cobranca.Descricao;
        var valorAntes = cobranca.Valor;
        var vencimentoAntes = cobranca.Vencimento;

        cobranca.Descricao = request.Descricao.Trim();
        cobranca.Valor = request.Valor;
        cobranca.Vencimento = request.Vencimento;

        var detalhe = AuditoriaDetalhe.MontarAlteracoes(
            ("Descrição", descricaoAntes, cobranca.Descricao),
            ("Valor", valorAntes, cobranca.Valor),
            ("Vencimento", vencimentoAntes, cobranca.Vencimento));

        auditoria.Registrar(nameof(Cobranca), cobranca.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(), detalhe);
        await db.SaveChangesAsync();

        var editada = await ComIncludes().FirstAsync(c => c.Id == id);
        return Ok(editada.ToDto());
    }

    [HttpPost("cobrancas/{id:guid}/marcar-paga")]
    [Authorize(Roles = GruposDePapeis.Financeiro)]
    public async Task<ActionResult<CobrancaDto>> MarcarPaga(Guid id)
    {
        var cobranca = await db.Cobrancas.FirstOrDefaultAsync(c => c.Id == id);
        if (cobranca is null) return NotFound("Cobrança não encontrada.");

        if (cobranca.Cancelada)
            return BadRequest("Não é possível marcar como paga uma cobrança cancelada.");

        cobranca.Paga = true;
        cobranca.PagoEm = await relogio.HojeAsync();
        auditoria.Registrar(nameof(Cobranca), cobranca.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(), $"Marcada como paga: {cobranca.Descricao}");
        await db.SaveChangesAsync();

        var atualizada = await ComIncludes().FirstAsync(c => c.Id == id);
        return Ok(atualizada.ToDto());
    }

    [HttpPost("cobrancas/{id:guid}/desmarcar-paga")]
    [Authorize(Roles = GruposDePapeis.Financeiro)]
    public async Task<ActionResult<CobrancaDto>> DesmarcarPaga(Guid id)
    {
        var cobranca = await db.Cobrancas.FirstOrDefaultAsync(c => c.Id == id);
        if (cobranca is null) return NotFound("Cobrança não encontrada.");

        cobranca.Paga = false;
        cobranca.PagoEm = null;
        auditoria.Registrar(nameof(Cobranca), cobranca.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(), $"Marcada como pendente: {cobranca.Descricao}");
        await db.SaveChangesAsync();

        var atualizada = await ComIncludes().FirstAsync(c => c.Id == id);
        return Ok(atualizada.ToDto());
    }

    /// <summary>Em vez de excluir (perderia o rastro de quem cancelou e o próprio registro do valor que
    /// deixou de ser cobrado), marca como Cancelada — some dos totais em aberto mas continua visível na
    /// lista e no histórico. Só cabe numa cobrança ainda não paga; se já foi paga, desmarcar antes.</summary>
    [HttpPost("cobrancas/{id:guid}/cancelar")]
    [Authorize(Roles = GruposDePapeis.Financeiro)]
    public async Task<ActionResult<CobrancaDto>> Cancelar(Guid id)
    {
        var cobranca = await db.Cobrancas.FirstOrDefaultAsync(c => c.Id == id);
        if (cobranca is null) return NotFound("Cobrança não encontrada.");

        if (cobranca.Paga)
            return BadRequest("Não é possível cancelar uma cobrança já paga — desmarque o pagamento antes.");

        cobranca.Cancelada = true;
        cobranca.CanceladaEm = await relogio.HojeAsync();
        auditoria.Registrar(nameof(Cobranca), cobranca.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(), $"Cancelada: {cobranca.Descricao}");
        await db.SaveChangesAsync();

        var atualizada = await ComIncludes().FirstAsync(c => c.Id == id);
        return Ok(atualizada.ToDto());
    }

    [HttpPost("cobrancas/{id:guid}/reabrir")]
    [Authorize(Roles = GruposDePapeis.Financeiro)]
    public async Task<ActionResult<CobrancaDto>> Reabrir(Guid id)
    {
        var cobranca = await db.Cobrancas.FirstOrDefaultAsync(c => c.Id == id);
        if (cobranca is null) return NotFound("Cobrança não encontrada.");

        cobranca.Cancelada = false;
        cobranca.CanceladaEm = null;
        auditoria.Registrar(nameof(Cobranca), cobranca.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(), $"Reaberta: {cobranca.Descricao}");
        await db.SaveChangesAsync();

        var atualizada = await ComIncludes().FirstAsync(c => c.Id == id);
        return Ok(atualizada.ToDto());
    }

    /// <summary>Código Pix "copia e cola" pra pagar uma cobrança específica — gerado localmente
    /// (padrão BR Code do Bacen), sem depender de nenhuma conta em processadora de pagamento.
    /// Equipe vê qualquer cobrança; um Responsável só a(s) do(s) próprio(s) filho(s).</summary>
    [HttpGet("cobrancas/{id:guid}/pix")]
    [Authorize(Roles = $"{GruposDePapeis.Equipe},{GruposDePapeis.Responsavel}")]
    public async Task<ActionResult<PixCobrancaDto>> ObterPix(Guid id)
    {
        var cobranca = await db.Cobrancas.FirstOrDefaultAsync(c => c.Id == id);
        if (cobranca is null) return NotFound("Cobrança não encontrada.");

        if (User.IsInRole("Responsavel"))
        {
            var responsavelId = User.FindFirst("responsavelId")?.Value;
            var ehFilho = await db.AlunoResponsaveis.AnyAsync(ar => ar.AlunoId == cobranca.AlunoId && ar.ResponsavelId.ToString() == responsavelId);
            if (!ehFilho) return Forbid();
        }

        var config = await db.ConfiguracoesFinanceiras.FirstOrDefaultAsync();
        if (config is null || string.IsNullOrWhiteSpace(config.PixChave) || string.IsNullOrWhiteSpace(config.PixNomeRecebedor) || string.IsNullOrWhiteSpace(config.PixCidade))
            return BadRequest("A chave Pix da escola ainda não foi configurada.");

        var codigo = PixBrCode.Gerar(config.PixChave, config.PixNomeRecebedor, config.PixCidade, cobranca.Valor, cobranca.Id.ToString("N"));
        return Ok(new PixCobrancaDto(codigo));
    }

    /// <summary>Envia por e-mail, ao(s) responsável(is) do aluno, os detalhes da cobrança e o código Pix pra pagamento.</summary>
    [HttpPost("cobrancas/{id:guid}/enviar-email")]
    [Authorize(Roles = GruposDePapeis.Financeiro)]
    public async Task<IActionResult> EnviarEmail(Guid id)
    {
        if (!emailSender.Configurado)
            return BadRequest("Envio de e-mail não configurado — defina as credenciais de SMTP para habilitar esse recurso.");

        var cobranca = await ComIncludes().FirstOrDefaultAsync(c => c.Id == id);
        if (cobranca is null) return NotFound("Cobrança não encontrada.");

        var destinatarios = await db.AlunoResponsaveis
            .Where(ar => ar.AlunoId == cobranca.AlunoId)
            .Select(ar => ar.Responsavel)
            .Where(r => r.Email != "")
            .ToListAsync();

        if (destinatarios.Count == 0)
            return BadRequest("Nenhum responsável com e-mail cadastrado para esse aluno.");

        var config = await db.ConfiguracoesFinanceiras.FirstOrDefaultAsync();
        string? codigoPix = null;
        if (config is not null && !string.IsNullOrWhiteSpace(config.PixChave) && !string.IsNullOrWhiteSpace(config.PixNomeRecebedor) && !string.IsNullOrWhiteSpace(config.PixCidade))
            codigoPix = PixBrCode.Gerar(config.PixChave, config.PixNomeRecebedor, config.PixCidade, cobranca.Valor, cobranca.Id.ToString("N"));

        var corpo = MontarCorpoEmail(cobranca, codigoPix);

        foreach (var responsavel in destinatarios)
            await emailSender.EnviarAsync(responsavel.Email, responsavel.Nome, $"Cobrança — {cobranca.Descricao}", corpo);

        return Ok();
    }

    /// <summary>Configurações financeiras da escola (recebimento Pix e política de bloqueio por atraso) —
    /// GET liberado pra Financeiro (precisa do Pix pra gerar os códigos), PUT restrito à Gestão
    /// (é identidade financeira e política da escola, não é operacional do dia a dia).</summary>
    [HttpGet("configuracao")]
    [Authorize(Roles = GruposDePapeis.Financeiro)]
    public async Task<ActionResult<ConfiguracaoFinanceiraDto>> ObterConfiguracao()
    {
        var config = await db.ConfiguracoesFinanceiras.FirstOrDefaultAsync();
        return Ok((config ?? new ConfiguracaoFinanceira()).ToDto());
    }

    [HttpPut("configuracao")]
    [Authorize(Roles = GruposDePapeis.Gestao)]
    public async Task<ActionResult<ConfiguracaoFinanceiraDto>> EditarConfiguracao(EditarConfiguracaoFinanceiraRequest request)
    {
        string? chave = null;
        TipoChavePix? tipo = null;
        if (!string.IsNullOrWhiteSpace(request.PixChave))
        {
            if (request.PixTipoChave is not { } tipoInformado || !Enum.IsDefined(tipoInformado))
                return BadRequest("Selecione o tipo da chave Pix.");

            chave = ChavePix.Normalizar(tipoInformado, request.PixChave, out var erroChave);
            if (chave is null) return BadRequest(erroChave);
            tipo = tipoInformado;
        }

        if (request.DiasParaBloqueio is < 1 or > 365)
            return BadRequest("A quantidade de dias para bloqueio deve estar entre 1 e 365 (ou fique em branco para não bloquear).");

        var config = await db.ConfiguracoesFinanceiras.FirstOrDefaultAsync();
        var criando = config is null;
        if (config is null)
        {
            config = new ConfiguracaoFinanceira { Id = Guid.NewGuid() };
            db.ConfiguracoesFinanceiras.Add(config);
        }

        var tipoAntes = config.PixTipoChave?.ToString();
        var chaveAntes = config.PixChave;
        var nomeAntes = config.PixNomeRecebedor;
        var cidadeAntes = config.PixCidade;
        var diasBloqueioAntes = config.DiasParaBloqueio;

        config.PixChave = chave;
        config.PixTipoChave = tipo;
        config.PixNomeRecebedor = string.IsNullOrWhiteSpace(request.PixNomeRecebedor) ? null : request.PixNomeRecebedor.Trim();
        config.PixCidade = string.IsNullOrWhiteSpace(request.PixCidade) ? null : request.PixCidade.Trim();
        config.DiasParaBloqueio = request.DiasParaBloqueio;
        config.AtualizadoEm = DateTime.UtcNow;

        var detalhe = AuditoriaDetalhe.MontarAlteracoes(
            ("Tipo da chave Pix", tipoAntes, config.PixTipoChave?.ToString()),
            ("Chave Pix", chaveAntes, config.PixChave),
            ("Nome do recebedor", nomeAntes, config.PixNomeRecebedor),
            ("Cidade", cidadeAntes, config.PixCidade),
            ("Dias para bloqueio", diasBloqueioAntes, config.DiasParaBloqueio));

        // Salvar sem mudar nada não vira entrada de histórico (só poluiria a trilha).
        if (criando || detalhe is not null)
            auditoria.Registrar(nameof(ConfiguracaoFinanceira), config.Id, criando ? AcaoAuditoria.Criado : AcaoAuditoria.Editado, this.UsuarioIdAtual(), detalhe);
        await db.SaveChangesAsync();

        return Ok(config.ToDto());
    }

    /// <summary>Visão do Portal dos Pais: cobranças de um aluno específico. Equipe vê qualquer aluno;
    /// um Responsável só o(s) próprio(s) filho(s).</summary>
    [HttpGet("/api/alunos/{alunoId:guid}/cobrancas")]
    [Authorize(Roles = $"{GruposDePapeis.Equipe},{GruposDePapeis.Responsavel}")]
    public async Task<ActionResult<List<CobrancaDto>>> ListarDoAluno(Guid alunoId)
    {
        if (!await db.Alunos.AnyAsync(a => a.Id == alunoId))
            return NotFound("Aluno não encontrado.");

        if (User.IsInRole("Responsavel"))
        {
            var responsavelId = User.FindFirst("responsavelId")?.Value;
            var ehFilho = await db.AlunoResponsaveis.AnyAsync(ar => ar.AlunoId == alunoId && ar.ResponsavelId.ToString() == responsavelId);
            if (!ehFilho) return Forbid();
        }

        var cobrancas = await ComIncludes()
            .Where(c => c.AlunoId == alunoId)
            .OrderByDescending(c => c.Vencimento)
            .ToListAsync();

        return Ok(cobrancas.Select(c => c.ToDto()));
    }

    private static string? Validar(string descricao, decimal valor)
    {
        if (string.IsNullOrWhiteSpace(descricao))
            return "Informe uma descrição.";

        if (valor <= 0)
            return "O valor deve ser maior que zero.";

        return null;
    }

    private IQueryable<Cobranca> ComIncludes() =>
        db.Cobrancas.Include(c => c.Aluno).ThenInclude(a => a.Turma);

    private static string MontarCorpoEmail(Cobranca cobranca, string? codigoPix)
    {
        var pixHtml = codigoPix is null
            ? "<p>Entre em contato com a escola para combinar o pagamento.</p>"
            : $"""
               <p>Pague com Pix copiando o código abaixo no app do seu banco:</p>
               <p style="font-family: monospace; word-break: break-all; background: #f3f4f6; padding: 12px; border-radius: 6px;">{codigoPix}</p>
               """;

        return $"""
                <p>Olá,</p>
                <p>Você tem uma cobrança referente a <strong>{cobranca.Descricao}</strong>, aluno(a) <strong>{cobranca.Aluno.Nome}</strong>.</p>
                <p>Valor: <strong>R$ {cobranca.Valor:F2}</strong><br/>
                Vencimento: <strong>{cobranca.Vencimento:dd/MM/yyyy}</strong></p>
                {pixHtml}
                """;
    }
}
