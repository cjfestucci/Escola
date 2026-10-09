using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Escola.Api.Auth;
using Escola.Api.Servicos;
using Escola.Domain.Enums;
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

public record PlanoAssinaturaDto(decimal PrecoFixo, decimal PrecoPorAtleta, int DiasTeste, string VersaoTermos);

public record CadastroAssinaturaRequest(
    string? NomeClube, string? CpfCnpj, string? Cidade, int Atletas,
    string? NomeAdmin, string? Email, string? Celular, bool AceiteTermos);

public record CadastroAssinaturaDto(string Situacao, DateOnly? TesteAte, decimal ValorMensal, string? LinkPagamento, bool EmailEnviado);

public record FaturaAssinaturaDto(DateOnly Vencimento, decimal Valor, bool Paga, DateOnly? PagaEm, string? LinkPagamento);

public record MinhaAssinaturaDto(
    string Situacao, string Rotulo, bool Bloqueada, DateOnly? TesteAte, int? DiasRestantesTeste,
    decimal ValorMensal, int AtletasInformados, DateOnly? VencimentoEmAberto, string? LinkPagamento,
    int DiasToleranciaAtraso, bool FaturasDisponiveis, IReadOnlyList<FaturaAssinaturaDto> Faturas);

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
    ILogger<AssinaturasController> logger) : ControllerBase
{
    /// <summary>Cadastros por IP numa janela de 15 minutos — cada cadastro cria um cliente de verdade (e um cliente no gateway).</summary>
    private const int CadastrosPorIp = 5;

    [HttpGet("plano")]
    [AllowAnonymous]
    public ActionResult<PlanoAssinaturaDto> Plano()
    {
        var o = opcoes.Value;
        return Ok(new PlanoAssinaturaDto(o.PrecoFixo, o.PrecoPorAtleta, Math.Max(0, o.DiasTeste), AssinaturaService.VersaoTermosUso));
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
        var documento = SoDigitos(request.CpfCnpj);
        var cidade = request.Cidade?.Trim() ?? string.Empty;
        var nomeAdmin = request.NomeAdmin?.Trim() ?? string.Empty;
        var email = request.Email?.Trim() ?? string.Empty;
        var celular = SoDigitos(request.Celular);

        if (nomeClube.Length < 3 || nomeClube.Length > 200) return BadRequest("Informe o nome do clube (de 3 a 200 caracteres).");
        if (documento.Length == 11 ? !ChavePix.CpfValido(documento) : documento.Length != 14 || !ChavePix.CnpjValido(documento))
            return BadRequest("Informe um CPF ou CNPJ válido.");
        if (cidade.Length < 2 || cidade.Length > 120) return BadRequest("Informe a cidade.");
        if (request.Atletas < 1 || request.Atletas > 5000) return BadRequest("Informe quantos atletas o clube tem (de 1 a 5.000).");
        if (nomeAdmin.Length < 3 || nomeAdmin.Length > 200) return BadRequest("Informe o seu nome.");
        if (!EmailValido(email)) return BadRequest("Informe um e-mail válido.");
        if (celular.Length is not (10 or 11)) return BadRequest("Informe o celular com DDD.");
        if (!request.AceiteTermos) return BadRequest("É preciso aceitar os Termos de Uso e a Política de Privacidade.");

        limitador.RegistrarFalha(chave); // conta todo cadastro aceito pra validação, não só os que falham
        var navegador = Request.Headers.UserAgent.ToString() is { Length: > 0 } ua ? ua[..Math.Min(ua.Length, 500)] : null;

        var resultado = await assinaturas.CadastrarAsync(
            new DadosCadastroAssinatura(nomeClube, documento, cidade, request.Atletas, nomeAdmin, email, celular, ip, navegador), ct);
        logger.LogInformation("Novo cliente pela assinatura do site: {ClienteId} ({Situacao}).", resultado.ClienteId, resultado.Situacao);

        return Ok(new CadastroAssinaturaDto(resultado.Situacao.ToString(), resultado.TesteAte, resultado.ValorMensal,
            resultado.LinkPagamento, resultado.EmailEnviado));
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

    private async Task<MinhaAssinaturaDto?> MontarAsync(CancellationToken ct)
    {
        var a = await db.Assinaturas.AsNoTracking().FirstOrDefaultAsync(ct);
        if (a is null) return null;

        var (faturas, disponivel) = await assinaturas.FaturasAsync(a, ct);
        var hoje = await relogio.HojeAsync();
        int? diasRestantes = a.Situacao == SituacaoAssinatura.EmTeste && a.TesteAte is { } ate ? Math.Max(0, ate.DayNumber - hoje.DayNumber) : null;

        return new MinhaAssinaturaDto(a.Situacao.ToString(), AssinaturaService.Rotulo(a.Situacao), SituacaoAssinaturaCalculo.Bloqueia(a.Situacao),
            a.TesteAte, diasRestantes, a.ValorMensal, a.AtletasInformados, a.VencimentoEmAberto, a.LinkPagamento,
            opcoes.Value.DiasToleranciaAtraso, disponivel,
            faturas.Select(f => new FaturaAssinaturaDto(f.Vencimento, f.Valor, f.Paga, f.PagaEm, f.Paga ? null : f.LinkPagamento)).ToList());
    }

    private static string SoDigitos(string? texto) => new((texto ?? string.Empty).Where(char.IsAsciiDigit).ToArray());

    private static bool EmailValido(string email) =>
        email.Length is > 3 and <= 256 && MailAddress.TryCreate(email, out var endereco) && endereco.Address == email && email.Contains('.');

    private static bool TokenConfere(string recebido, string esperado) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(recebido), Encoding.UTF8.GetBytes(esperado));
}
