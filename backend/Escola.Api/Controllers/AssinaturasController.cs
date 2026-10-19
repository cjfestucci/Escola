using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Escola.Api.Auth;
using Escola.Api.Servicos;
using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Escola.Infrastructure.Documentos;
using Escola.Infrastructure.Assinaturas;
using Escola.Infrastructure.Auth;
using Escola.Infrastructure.Clientes;
using Escola.Infrastructure.Data;
using Escola.Infrastructure.Pagamentos;
using Escola.Infrastructure.Tempo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Escola.Api.Controllers;

/// <param name="Restantes">Quantos cadastros ainda podem usar o cupom (nulo = sem limite) — o site mostra "próximos N assinantes".</param>
public record CupomPublicoDto(string Codigo, int PercentualDesconto, int? Meses, string Descricao, int? MesesGratis = null, int? Restantes = null);

/// <param name="PrecoFixo">Valor mínimo, que já cobre <c>AtletasIncluidos</c> atletas.</param>
/// <param name="PrecoPorAtleta">Valor de cada atleta acima dos incluídos.</param>
/// <param name="ChaveCaptcha">Chave pública do Turnstile pra tela mostrar o captcha; nula = cadastro sem captcha.</param>
public record PlanoAssinaturaDto(decimal PrecoFixo, decimal PrecoPorAtleta, int DiasTeste, string VersaoTermos, int AtletasIncluidos, string? ChaveCaptcha = null);

public record CadastroAssinaturaRequest(
    string? NomeClube, string? CpfCnpj, string? Cidade, int Atletas,
    string? NomeAdmin, string? Email, string? Celular, bool AceiteTermos, string? Cupom = null, string? TokenCaptcha = null);

public record CadastroAssinaturaDto(string Situacao, DateOnly? TesteAte, decimal ValorMensal, string? LinkPagamento, bool EmailEnviado);

public record FaturaAssinaturaDto(DateOnly Vencimento, decimal Valor, bool Paga, DateOnly? PagaEm, string? LinkPagamento);

public record MinhaAssinaturaDto(
    string Situacao, string Rotulo, bool Bloqueada, DateOnly? TesteAte, int? DiasRestantesTeste,
    decimal ValorMensal, int AtletasInformados, int? AtletasCobrados, DateOnly? VencimentoEmAberto, string? LinkPagamento,
    int DiasToleranciaAtraso, bool FaturasDisponiveis, IReadOnlyList<FaturaAssinaturaDto> Faturas,
    DateOnly? AcessoAte, DateTime? CanceladaEm, DateOnly? DataExclusao, string? CupomCodigo = null, string? CupomDescricao = null);

/// <summary>Assinatura do produto pelo site: plano e cadastro (anônimos — é como o clube vira cliente), webhook do gateway (anônimo,
/// com token) e a tela da assinatura do próprio clube (Admin ou Suporte).</summary>
[ApiController]
[Route("api/assinaturas")]
[Authorize]
public class AssinaturasController(
    EscolaDbContext db,
    IAssinaturaService assinaturas,
    IOptions<OpcoesAssinatura> opcoes,
    LimitadorTentativasLogin limitador,
    ClienteAtual clienteAtual,
    IRelogioEscola relogio,
    VerificadorCaptcha captcha,
    Escola.Infrastructure.Email.IEmailSender email,
    IConfiguration config,
    ILogger<AssinaturasController> logger) : ControllerBase
{
    /// <summary>Cadastros por IP numa janela de 15 minutos — cada cadastro cria um cliente de verdade (e um cliente no gateway).</summary>
    private const int CadastrosPorIp = 5;

    [HttpGet("plano")]
    [AllowAnonymous]
    public ActionResult<PlanoAssinaturaDto> Plano()
    {
        var o = opcoes.Value;
        return Ok(new PlanoAssinaturaDto(o.PrecoFixo, o.PrecoPorAtleta, Math.Max(0, o.DiasTeste), AssinaturaService.VersaoTermosUso, Math.Max(0, o.AtletasIncluidos), captcha.ChavePublica));
    }

    /// <summary>Confere um cupom na tela de cadastro (antes de assinar) e diz o desconto. Com freio por IP: não serve pra adivinhar cupons.</summary>
    [HttpGet("cupom/{codigo}")]
    [AllowAnonymous]
    public async Task<ActionResult<CupomPublicoDto>> ConferirCupom(string codigo, CancellationToken ct)
    {
        var chave = $"cupom:{HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconhecido"}";
        if (limitador.EstaBloqueado(chave, ConsultasDeCupomPorIp, out var restante))
            return StatusCode(StatusCodes.Status429TooManyRequests,
                $"Muitas tentativas. Tente novamente em {Math.Max(1, (int)Math.Ceiling(restante.TotalMinutes))} minutos.");

        var (cupom, motivo, usos) = await CupomValidoAsync(codigo, ct);
        if (motivo is not null)
        {
            limitador.RegistrarFalha(chave); // só os errados contam
            return NotFound(motivo);
        }
        return Ok(new CupomPublicoDto(cupom!.Codigo, cupom.PercentualDesconto, cupom.Meses,
            CupomRegras.Descricao(cupom.PercentualDesconto, cupom.Meses, cupom.MesesGratis), cupom.MesesGratis,
            cupom.UsosMaximos is { } maximo ? Math.Max(0, maximo - usos) : null));
    }

    private const int ConsultasDeCupomPorIp = 10;

    private async Task<(Cupom? Cupom, string? Motivo, int Usos)> CupomValidoAsync(string codigo, CancellationToken ct)
    {
        var normalizado = CupomRegras.Normalizar(codigo);
        var cupom = await db.Cupons.AsNoTracking().FirstOrDefaultAsync(c => c.Codigo == normalizado, ct);
        var usos = cupom is null ? 0 : await db.Assinaturas.IgnoreQueryFilters().CountAsync(a => a.CupomCodigo == cupom.Codigo, ct);
        // Sem login não há escola: o "hoje" é o do fuso padrão do produto.
        var motivo = CupomRegras.MotivoInvalido(cupom, await relogio.HojeAsync(), usos);
        return (motivo is null ? cupom : null, motivo, usos);
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<CadastroAssinaturaDto>> Cadastrar(CadastroAssinaturaRequest request, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var chave = $"assinatura:{ip ?? "desconhecido"}";
        if (limitador.EstaBloqueado(chave, CadastrosPorIp, out var restante))
            return StatusCode(StatusCodes.Status429TooManyRequests,
                $"Muitos cadastros seguidos. Tente novamente em {Math.Max(1, (int)Math.Ceiling(restante.TotalMinutes))} minutos.");

        var nomeClube = request.NomeClube?.Trim() ?? string.Empty;
        var documento = DocumentoBr.Normalizar(request.CpfCnpj); // CNPJ alfanumérico: mantém as letras
        var cidade = request.Cidade?.Trim() ?? string.Empty;
        var nomeAdmin = request.NomeAdmin?.Trim() ?? string.Empty;
        var email = request.Email?.Trim() ?? string.Empty;
        var celular = SoDigitos(request.Celular);

        if (nomeClube.Length < 3 || nomeClube.Length > 200) return BadRequest("Informe o nome do clube (de 3 a 200 caracteres).");
        if (!DocumentoBr.CpfOuCnpjValido(documento))
            return BadRequest("Informe um CPF ou CNPJ válido.");
        if (cidade.Length < 2 || cidade.Length > 120) return BadRequest("Informe a cidade.");
        if (request.Atletas < 1 || request.Atletas > 5000) return BadRequest("Informe quantos atletas o clube tem (de 1 a 5.000).");
        if (nomeAdmin.Length < 3 || nomeAdmin.Length > 200) return BadRequest("Informe o seu nome.");
        if (!EmailValido(email)) return BadRequest("Informe um e-mail válido.");
        if (celular.Length is not (10 or 11)) return BadRequest("Informe o celular com DDD.");
        if (!request.AceiteTermos) return BadRequest("É preciso aceitar os Termos de Uso e a Política de Privacidade.");

        Cupom? cupom = null;
        if (!string.IsNullOrWhiteSpace(request.Cupom))
        {
            (cupom, var motivo, _) = await CupomValidoAsync(request.Cupom, ct);
            if (motivo is not null) return BadRequest(motivo);
        }

        // Captcha (Turnstile) por último, depois dos campos: o token vale uma vez só, e um erro de digitação não gasta a verificação à toa.
        if (!await captcha.ValidarAsync(request.TokenCaptcha, ip, ct))
            return BadRequest("Não foi possível confirmar que você não é um robô. Faça a verificação de novo e tente outra vez.");

        limitador.RegistrarFalha(chave); // conta todo cadastro aceito pra validação, não só os que falham
        var navegador = Request.Headers.UserAgent.ToString() is { Length: > 0 } ua ? ua[..Math.Min(ua.Length, 500)] : null;

        var resultado = await assinaturas.CadastrarAsync(
            new DadosCadastroAssinatura(nomeClube, documento, cidade, request.Atletas, nomeAdmin, email, celular, ip, navegador, cupom), ct);
        logger.LogInformation("Novo cliente pela assinatura do site: {ClienteId} ({Situacao}).", resultado.ClienteId, resultado.Situacao);
        await AvisarNovoCadastroAsync(new DadosCadastroAssinatura(nomeClube, documento, cidade, request.Atletas, nomeAdmin, email, celular, ip,
            navegador, cupom), resultado, ct);

        return Ok(new CadastroAssinaturaDto(resultado.Situacao.ToString(), resultado.TesteAte, resultado.ValorMensal,
            resultado.LinkPagamento, resultado.EmailEnviado));
    }

    /// <summary>Avisa a equipe do produto que um clube se cadastrou pelo site (<c>Assinatura:EmailAvisoCadastro</c>; vazio = sem aviso).
    /// Falhar aqui nunca desfaz o cadastro: o clube já foi criado, o aviso é só pra gente.</summary>
    private async Task AvisarNovoCadastroAsync(DadosCadastroAssinatura dados, ResultadoCadastroAssinatura resultado, CancellationToken ct)
    {
        // Um ou mais endereços, separados por vírgula ou ponto e vírgula.
        var destinos = (config["Assinatura:EmailAvisoCadastro"] ?? string.Empty)
            .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (destinos.Length == 0) return;

        static string H(string? texto) => System.Net.WebUtility.HtmlEncode(texto ?? string.Empty);
        var situacao = resultado.TesteAte is { } fimTeste
            ? $"Em teste grátis até {fimTeste:dd/MM/yyyy}"
            : resultado.Situacao == SituacaoAssinatura.AguardandoPagamento ? "Aguardando o 1º pagamento" : resultado.Situacao.ToString();
        var cupom = dados.Cupom is { } c ? $"{H(c.Codigo)} ({H(CupomRegras.Descricao(c.PercentualDesconto, c.Meses, c.MesesGratis))})" : "nenhum";
        var corpo = $"""
            <p><strong>Novo clube cadastrado pelo site</strong></p>
            <p style="margin:0 0 16px;padding:14px 16px;background:#F3F5F7;border-radius:8px;">
            Clube: <strong>{H(dados.NomeClube)}</strong><br>
            CPF/CNPJ: {H(dados.CpfCnpj)}<br>
            Cidade: {H(dados.Cidade)}<br>
            Atletas informados: {dados.Atletas}<br>
            Mensalidade: {resultado.ValorMensal.ToString("C", new System.Globalization.CultureInfo("pt-BR"))}<br>
            Situação: {H(situacao)}<br>
            Cupom: {cupom}
            </p>
            <p>Administrador: {H(dados.NomeAdmin)}<br>
            E-mail: {H(dados.Email)}<br>
            Celular: {H(dados.Celular)}</p>
            <p style="font-size:13px;color:#555">Convite de acesso {(resultado.EmailEnviado ? "enviado" : "<strong>não enviado</strong> (confira o SMTP)")} ao administrador.
            Id do cliente: {resultado.ClienteId}. Acompanhe em Plataforma → Clientes.</p>
            """;
        foreach (var destino in destinos)
        {
            try
            {
                await email.EnviarAsync(destino, $"Equipe {Escola.Infrastructure.MarcaProduto.Nome}", $"Novo clube cadastrado: {dados.NomeClube}", corpo, ct,
                    responderPara: dados.Email);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Clube {ClienteId} cadastrado, mas o aviso por e-mail para {Destino} falhou.", resultado.ClienteId, destino);
            }
        }
    }

    /// <summary>Aviso do Asaas (conta raiz) sobre uma fatura da assinatura. <b>O corpo nunca é confiado</b>: só diz qual assinatura
    /// conferir, e as faturas são lidas no Asaas. Responde 200 até pra assinatura desconhecida (erro repetido pausa a fila do Asaas).</summary>
    [HttpPost("asaas/webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook([FromBody] JsonElement corpo, CancellationToken ct)
    {
        var token = opcoes.Value.WebhookToken;
        if (string.IsNullOrWhiteSpace(token)) return NotFound();
        if (!TokenConfere(Request.Headers["asaas-access-token"].ToString(), token)) return Unauthorized();

        var idAssinatura = corpo.ValueKind == JsonValueKind.Object && corpo.TryGetProperty("payment", out var pagamento)
            && pagamento.ValueKind == JsonValueKind.Object && pagamento.TryGetProperty("subscription", out var s) && s.ValueKind == JsonValueKind.String
            ? s.GetString() : null;
        if (string.IsNullOrWhiteSpace(idAssinatura)) return Ok();

        var cliente = await db.Assinaturas.IgnoreQueryFilters()
            .Where(a => a.IdAssinaturaGateway == idAssinatura)
            .Select(a => EF.Property<Guid>(a, EscolaDbContext.ColunaCliente))
            .FirstOrDefaultAsync(ct);
        if (cliente == Guid.Empty) return Ok();

        try
        {
            await assinaturas.AtualizarAsync(cliente, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Webhook da assinatura: não foi possível atualizar o cliente {Cliente}.", cliente);
        }
        return Ok();
    }

    /// <summary>A assinatura do clube logado. 404 pra cliente sem assinatura (provisionado à mão).</summary>
    [HttpGet("minha")]
    [Authorize(Roles = GruposDePapeis.AdminOuSuporte)]
    public async Task<ActionResult<MinhaAssinaturaDto>> Minha(CancellationToken ct)
    {
        var dto = await MontarAsync(ct);
        return dto is null ? NotFound("Este clube não tem assinatura pelo site.") : Ok(dto);
    }

    /// <summary>Confere agora no gateway (ex.: o Admin acabou de pagar e não quer esperar).</summary>
    [HttpPost("minha/atualizar")]
    [Authorize(Roles = GruposDePapeis.AdminOuSuporte)]
    public async Task<ActionResult<MinhaAssinaturaDto>> Atualizar(CancellationToken ct)
    {
        if (await assinaturas.AtualizarAsync(clienteAtual.Id, ct) is null) return NotFound("Este clube não tem assinatura pelo site.");
        return Ok(await MontarAsync(ct));
    }

    /// <summary>Cancela a assinatura. O acesso segue até o fim do período pago (ou do teste); sem multa, sem fidelidade.</summary>
    [HttpPost("minha/cancelar")]
    [Authorize(Roles = GruposDePapeis.AdminOuSuporte)]
    public async Task<ActionResult<MinhaAssinaturaDto>> Cancelar(CancellationToken ct)
    {
        var (_, erro) = await assinaturas.CancelarAsync(clienteAtual.Id, this.UsuarioIdAtual(), ct);
        if (erro is not null) return BadRequest(erro);
        return Ok(await MontarAsync(ct));
    }

    /// <summary>Desfaz o cancelamento. Dentro do período pago nada é cobrado agora; depois dele, a fatura nova vence hoje.</summary>
    [HttpPost("minha/reativar")]
    [Authorize(Roles = GruposDePapeis.AdminOuSuporte)]
    public async Task<ActionResult<MinhaAssinaturaDto>> Reativar(CancellationToken ct)
    {
        var erro = await assinaturas.ReativarAsync(clienteAtual.Id, this.UsuarioIdAtual(), ct);
        if (erro is not null) return BadRequest(erro);
        return Ok(await MontarAsync(ct));
    }

    private async Task<MinhaAssinaturaDto?> MontarAsync(CancellationToken ct)
    {
        var a = await db.Assinaturas.AsNoTracking().FirstOrDefaultAsync(ct);
        if (a is null) return null;

        var (faturas, disponivel) = await assinaturas.FaturasAsync(a, ct);
        var hoje = await relogio.HojeAsync();
        int? diasRestantes = a.Situacao == SituacaoAssinatura.EmTeste && a.TesteAte is { } ate ? Math.Max(0, ate.DayNumber - hoje.DayNumber) : null;

        return new MinhaAssinaturaDto(a.Situacao.ToString(), AssinaturaService.Rotulo(a.Situacao), SituacaoAssinaturaCalculo.Bloqueia(a.Situacao),
            a.TesteAte, diasRestantes, a.ValorMensal, a.AtletasInformados, a.AtletasCobrados, a.VencimentoEmAberto, a.LinkPagamento,
            opcoes.Value.DiasToleranciaAtraso, disponivel,
            faturas.Select(f => new FaturaAssinaturaDto(f.Vencimento, f.Valor, f.Paga, f.PagaEm, f.Paga || a.AcessoAte is not null ? null : f.LinkPagamento)).ToList(),
            a.AcessoAte, a.CanceladaEm,
            SituacaoAssinaturaCalculo.DataExclusao(a.Situacao, a.AcessoAte, a.SuspensaEm, a.JaPagou, opcoes.Value.DiasRetencaoAposSuspensao),
            a.CupomCodigo, a.CupomPercentual is { } pct ? CupomRegras.Descricao(pct, a.CupomMeses, a.CupomMesesGratis) : null);
    }

    private static string SoDigitos(string? texto) => new((texto ?? string.Empty).Where(char.IsAsciiDigit).ToArray());

    private static bool EmailValido(string email) =>
        email.Length is > 3 and <= 256 && MailAddress.TryCreate(email, out var endereco) && endereco.Address == email && email.Contains('.');

    private static bool TokenConfere(string recebido, string esperado) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(recebido), Encoding.UTF8.GetBytes(esperado));
}
