using Escola.Api.Auth;
using Escola.Api.Dtos;
using Escola.Api.Dtos.Requests;
using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Escola.Infrastructure.Auditoria;
using Escola.Infrastructure.Data;
using Escola.Infrastructure.Tempo;
using Escola.Infrastructure.Email;
using Escola.Infrastructure.Financeiro;
using Escola.Infrastructure.Pagamentos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Escola.Api.Controllers;

[ApiController]
[Route("api/financeiro")]
[Authorize]
public class FinanceiroController(EscolaDbContext db, IEmailSender emailSender, IAuditoriaService auditoria, IRelogioEscola relogio, IPixAutomaticoService pixAutomatico) : ControllerBase
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
        var (politica, hoje) = await ContextoEncargosAsync();
        return Ok(cobrancas.Select(c => c.ToDto(politica, hoje)));
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

        return CreatedAtAction(nameof(Listar), await ComoDtoAsync(cobranca.Id));
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

        return Ok(await ComoDtoAsync(id));
    }

    [HttpPost("cobrancas/{id:guid}/marcar-paga")]
    [Authorize(Roles = GruposDePapeis.Financeiro)]
    public async Task<ActionResult<CobrancaDto>> MarcarPaga(Guid id)
    {
        var cobranca = await db.Cobrancas.FirstOrDefaultAsync(c => c.Id == id);
        if (cobranca is null) return NotFound("Cobrança não encontrada.");

        if (cobranca.Cancelada)
            return BadRequest("Não é possível marcar como paga uma cobrança cancelada.");

        // O valor recebido já inclui multa/juros do dia: depois de paga, o encargo para de crescer e fica registrado.
        var (politica, hoje) = await ContextoEncargosAsync();
        var encargos = EncargosCobranca.Calcular(cobranca.Valor, cobranca.Vencimento, hoje, paga: false, cancelada: false, politica);
        cobranca.Paga = true;
        cobranca.PagoEm = hoje;
        cobranca.ValorPago = cobranca.Valor + encargos.Total;
        var detalhePagamento = encargos.Total > 0
            ? $"Marcada como paga: {cobranca.Descricao} (R$ {cobranca.ValorPago:N2}, com R$ {encargos.Total:N2} de multa/juros)"
            : $"Marcada como paga: {cobranca.Descricao}";
        auditoria.Registrar(nameof(Cobranca), cobranca.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(), detalhePagamento);
        await db.SaveChangesAsync();

        return Ok(await ComoDtoAsync(id));
    }

    [HttpPost("cobrancas/{id:guid}/desmarcar-paga")]
    [Authorize(Roles = GruposDePapeis.Financeiro)]
    public async Task<ActionResult<CobrancaDto>> DesmarcarPaga(Guid id)
    {
        var cobranca = await db.Cobrancas.FirstOrDefaultAsync(c => c.Id == id);
        if (cobranca is null) return NotFound("Cobrança não encontrada.");

        cobranca.Paga = false;
        cobranca.PagoEm = null;
        cobranca.ValorPago = null;
        auditoria.Registrar(nameof(Cobranca), cobranca.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(), $"Marcada como pendente: {cobranca.Descricao}");
        await db.SaveChangesAsync();

        return Ok(await ComoDtoAsync(id));
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

        return Ok(await ComoDtoAsync(id));
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

        return Ok(await ComoDtoAsync(id));
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
            var responsavelId = this.ResponsavelIdAtual();
            var ehFilho = await db.AlunoResponsaveis.AnyAsync(ar => ar.AlunoId == cobranca.AlunoId && ar.ResponsavelId == responsavelId);
            if (!ehFilho) return Forbid();
        }

        var config = await db.ConfiguracoesFinanceiras.FirstOrDefaultAsync();
        if (config is null || string.IsNullOrWhiteSpace(config.PixChave) || string.IsNullOrWhiteSpace(config.PixNomeRecebedor) || string.IsNullOrWhiteSpace(config.PixCidade))
            return BadRequest("A chave Pix da escola ainda não foi configurada.");

        // Cobra o valor atualizado (com multa/juros, se a escola configurou e a cobrança está atrasada).
        var valor = cobranca.Valor + EncargosCobranca.Calcular(cobranca.Valor, cobranca.Vencimento, await relogio.HojeAsync(), cobranca.Paga, cobranca.Cancelada, config.ToPolitica()).Total;
        var (codigo, automatico) = await GerarCodigoPixAsync(cobranca, config, valor);
        return Ok(new PixCobrancaDto(codigo, automatico));
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
        var encargos = EncargosCobranca.Calcular(cobranca.Valor, cobranca.Vencimento, await relogio.HojeAsync(), cobranca.Paga, cobranca.Cancelada, config.ToPolitica());
        var valorAtualizado = cobranca.Valor + encargos.Total;
        string? codigoPix = null;
        if (config is not null && !string.IsNullOrWhiteSpace(config.PixChave) && !string.IsNullOrWhiteSpace(config.PixNomeRecebedor) && !string.IsNullOrWhiteSpace(config.PixCidade))
            codigoPix = (await GerarCodigoPixAsync(cobranca, config, valorAtualizado)).Codigo;

        var corpo = MontarCorpoEmail(cobranca, codigoPix, valorAtualizado, encargos.Total);

        foreach (var responsavel in destinatarios)
            await emailSender.EnviarAsync(responsavel.Email, responsavel.Nome, $"Cobrança — {cobranca.Descricao}", corpo);

        return Ok();
    }

    /// <summary>Configurações financeiras da escola (recebimento Pix e política de bloqueio por atraso) —
    /// GET liberado pra Financeiro (precisa do Pix pra gerar os códigos), PUT restrito à Gestão
    /// (é identidade financeira e política da escola, não é operacional do dia a dia).</summary>
    [HttpGet("configuracao")]
    [Authorize(Roles = GruposDePapeis.FinanceiroOuSuporte)]
    public async Task<ActionResult<ConfiguracaoFinanceiraDto>> ObterConfiguracao()
    {
        var config = await db.ConfiguracoesFinanceiras.FirstOrDefaultAsync();
        return Ok((config ?? new ConfiguracaoFinanceira()).ToDto());
    }

    [HttpPut("configuracao")]
    [Authorize(Roles = GruposDePapeis.GestaoOuSuporte)]
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

        if (request.DiaVencimentoMensalidade is < 1 or > 31)
            return BadRequest("O dia de vencimento das mensalidades deve estar entre 1 e 31.");
        if (request.MultaAtrasoPercentual is < 0 or > 20)
            return BadRequest("A multa por atraso deve estar entre 0% e 20%.");
        if (request.JurosMensaisPercentual is < 0 or > 10)
            return BadRequest("Os juros mensais devem estar entre 0% e 10%.");

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
        var diaVencimentoAntes = config.DiaVencimentoMensalidade;
        var multaAntes = config.MultaAtrasoPercentual;
        var jurosAntes = config.JurosMensaisPercentual;

        config.PixChave = chave;
        config.PixTipoChave = tipo;
        config.PixNomeRecebedor = string.IsNullOrWhiteSpace(request.PixNomeRecebedor) ? null : request.PixNomeRecebedor.Trim();
        config.PixCidade = string.IsNullOrWhiteSpace(request.PixCidade) ? null : request.PixCidade.Trim();
        config.DiasParaBloqueio = request.DiasParaBloqueio;
        // Zero e vazio significam a mesma coisa ("não cobra"): guarda nulo pra não haver dois jeitos de dizer isso.
        config.DiaVencimentoMensalidade = request.DiaVencimentoMensalidade ?? config.DiaVencimentoMensalidade;
        config.MultaAtrasoPercentual = request.MultaAtrasoPercentual is > 0 ? request.MultaAtrasoPercentual : null;
        config.JurosMensaisPercentual = request.JurosMensaisPercentual is > 0 ? request.JurosMensaisPercentual : null;
        config.AtualizadoEm = DateTime.UtcNow;

        var detalhe = AuditoriaDetalhe.MontarAlteracoes(
            ("Tipo da chave Pix", tipoAntes, config.PixTipoChave?.ToString()),
            ("Chave Pix", chaveAntes, config.PixChave),
            ("Nome do recebedor", nomeAntes, config.PixNomeRecebedor),
            ("Cidade", cidadeAntes, config.PixCidade),
            ("Dias para bloqueio", diasBloqueioAntes, config.DiasParaBloqueio),
            ("Dia de vencimento das mensalidades", diaVencimentoAntes, config.DiaVencimentoMensalidade),
            ("Multa por atraso (%)", multaAntes, config.MultaAtrasoPercentual),
            ("Juros mensais por atraso (%)", jurosAntes, config.JurosMensaisPercentual));

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
            var responsavelId = this.ResponsavelIdAtual();
            var ehFilho = await db.AlunoResponsaveis.AnyAsync(ar => ar.AlunoId == alunoId && ar.ResponsavelId == responsavelId);
            if (!ehFilho) return Forbid();
        }

        var cobrancas = await ComIncludes()
            .Where(c => c.AlunoId == alunoId)
            .OrderByDescending(c => c.Vencimento)
            .ToListAsync();

        var (politica, hoje) = await ContextoEncargosAsync();
        return Ok(cobrancas.Select(c => c.ToDto(politica, hoje)));
    }

    // ----- Mensalidades em lote -----

    private static readonly string[] NomesMeses =
        ["Janeiro", "Fevereiro", "Março", "Abril", "Maio", "Junho", "Julho", "Agosto", "Setembro", "Outubro", "Novembro", "Dezembro"];

    /// <summary>Mostra o que seria gerado, sem gravar nada — pra conferir antes de confirmar.</summary>
    [HttpPost("mensalidades/previa")]
    [Authorize(Roles = GruposDePapeis.Financeiro)]
    public async Task<ActionResult<PreviaMensalidadesDto>> PreviaMensalidades(GerarMensalidadesRequest request)
    {
        var (previa, erro) = await MontarPreviaAsync(request);
        return erro is not null ? BadRequest(erro) : Ok(previa);
    }

    /// <summary>Gera as mensalidades do mês pra todo aluno ativo (de turma ativa e com valor definido) que ainda não tem
    /// a daquele mês — rodar duas vezes não duplica nada. Valor = mensalidade da turma menos o desconto do aluno.</summary>
    [HttpPost("mensalidades/gerar")]
    [Authorize(Roles = GruposDePapeis.Financeiro)]
    public async Task<ActionResult<GeracaoMensalidadesDto>> GerarMensalidades(GerarMensalidadesRequest request)
    {
        var (previa, erro) = await MontarPreviaAsync(request);
        if (erro is not null) return BadRequest(erro);

        var usuarioId = this.UsuarioIdAtual();
        var competencia = new DateOnly(request.Ano, request.Mes, 1);
        var geradas = 0;
        decimal total = 0;

        foreach (var item in previa!.Itens.Where(i => i.Situacao == SituacaoMensalidade.Gerar))
        {
            var cobranca = new Cobranca
            {
                Id = Guid.NewGuid(),
                AlunoId = item.AlunoId,
                Descricao = previa.Descricao,
                Valor = item.Valor,
                Vencimento = previa.Vencimento,
                Competencia = competencia,
                RegistradoEm = DateTime.UtcNow
            };
            db.Cobrancas.Add(cobranca);

            var desconto = item.DescontoPercentual > 0 ? $" (desconto de {item.DescontoPercentual:0.##}%)" : string.Empty;
            auditoria.Registrar(nameof(Cobranca), cobranca.Id, AcaoAuditoria.Criado, usuarioId, $"Gerada em lote: {cobranca.Descricao}{desconto}");
            geradas++;
            total += item.Valor;
        }

        await db.SaveChangesAsync();
        return Ok(new GeracaoMensalidadesDto(geradas, previa.JaExistem, previa.SemValor, previa.Isentos, total));
    }

    private async Task<(PreviaMensalidadesDto? Previa, string? Erro)> MontarPreviaAsync(GerarMensalidadesRequest request)
    {
        if (request.Mes is < 1 or > 12 || request.Ano is < 2000 or > 2100)
            return (null, "Informe um mês e um ano válidos.");

        var config = await db.ConfiguracoesFinanceiras.FirstOrDefaultAsync();
        var diaConfigurado = config?.DiaVencimentoMensalidade ?? 10;
        var competencia = new DateOnly(request.Ano, request.Mes, 1);
        var ultimoDia = DateTime.DaysInMonth(request.Ano, request.Mes);
        var fimDoMes = new DateOnly(request.Ano, request.Mes, ultimoDia);
        var vencimento = new DateOnly(request.Ano, request.Mes, Math.Min(diaConfigurado, ultimoDia));
        var descricao = $"Mensalidade - {NomesMeses[request.Mes - 1]}/{request.Ano}";

        var alunosQuery = db.Alunos.Include(a => a.Turma).Where(a => a.Ativo && a.Turma.Ativa);
        if (request.TurmaId is { } turmaId) alunosQuery = alunosQuery.Where(a => a.TurmaId == turmaId);
        var alunos = await alunosQuery.OrderBy(a => a.Turma.Nome).ThenBy(a => a.Nome).ToListAsync();

        // "Já existe" também enxerga mensalidades lançadas à mão antes desta função (sem Competencia): mesma
        // descrição de mensalidade com vencimento dentro do mês.
        var ids = alunos.Select(a => a.Id).ToList();
        var jaTem = (await db.Cobrancas
            .Where(c => !c.Cancelada && ids.Contains(c.AlunoId)
                && (c.Competencia == competencia
                    || (c.Competencia == null && c.Descricao.StartsWith("Mensalidade") && c.Vencimento >= competencia && c.Vencimento <= fimDoMes)))
            .Select(c => c.AlunoId)
            .Distinct()
            .ToListAsync()).ToHashSet();

        var itens = alunos.Select(a =>
        {
            var baseValor = a.Turma.ValorMensalidade ?? 0m;
            var valor = Math.Round(baseValor * (1 - a.DescontoMensalidadePercentual / 100m), 2, MidpointRounding.AwayFromZero);
            var situacao = jaTem.Contains(a.Id) ? SituacaoMensalidade.JaExiste
                : baseValor <= 0 ? SituacaoMensalidade.SemValor
                : valor <= 0 ? SituacaoMensalidade.Isento
                : SituacaoMensalidade.Gerar;
            return new MensalidadeItemDto(a.Id, a.Nome, a.Turma.Nome, baseValor, a.DescontoMensalidadePercentual, valor, situacao);
        }).ToList();

        var previa = new PreviaMensalidadesDto(
            request.Ano, request.Mes, descricao, vencimento, itens,
            itens.Count(i => i.Situacao == SituacaoMensalidade.Gerar),
            itens.Count(i => i.Situacao == SituacaoMensalidade.JaExiste),
            itens.Count(i => i.Situacao == SituacaoMensalidade.SemValor),
            itens.Count(i => i.Situacao == SituacaoMensalidade.Isento),
            itens.Where(i => i.Situacao == SituacaoMensalidade.Gerar).Sum(i => i.Valor));
        return (previa, null);
    }

    /// <summary>Pix dinâmico do banco (com baixa automática) quando a integração está ligada e a cobrança ainda está em aberto;
    /// caso contrário — integração desligada, cobrança já paga/cancelada ou banco fora do ar — o Pix estático de sempre.</summary>
    private async Task<(string Codigo, bool Automatico)> GerarCodigoPixAsync(Cobranca cobranca, ConfiguracaoFinanceira config, decimal valor)
    {
        if (!cobranca.Paga && !cobranca.Cancelada)
        {
            var dinamico = await pixAutomatico.ObterCopiaEColaAsync(cobranca, valor, config.PixChave!);
            if (dinamico is not null) return (dinamico, true);
        }

        return (PixBrCode.Gerar(config.PixChave!, config.PixNomeRecebedor!, config.PixCidade!, valor, cobranca.Id.ToString("N")), false);
    }

    private async Task<(EncargosCobranca.Politica Politica, DateOnly Hoje)> ContextoEncargosAsync()
    {
        var config = await db.ConfiguracoesFinanceiras.FirstOrDefaultAsync();
        return (config.ToPolitica(), await relogio.HojeAsync());
    }

    private async Task<CobrancaDto> ComoDtoAsync(Guid id)
    {
        var cobranca = await ComIncludes().FirstAsync(c => c.Id == id);
        var (politica, hoje) = await ContextoEncargosAsync();
        return cobranca.ToDto(politica, hoje);
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

    private static string MontarCorpoEmail(Cobranca cobranca, string? codigoPix, decimal valorAtualizado, decimal encargos)
    {
        var encargosHtml = encargos > 0
            ? $"<br/>Em atraso — valor atualizado com multa/juros: <strong>R$ {valorAtualizado:N2}</strong>"
            : string.Empty;

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
                Vencimento: <strong>{cobranca.Vencimento:dd/MM/yyyy}</strong>{encargosHtml}</p>
                {pixHtml}
                """;
    }
}
