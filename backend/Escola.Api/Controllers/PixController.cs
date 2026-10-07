using Escola.Infrastructure.Clientes;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Escola.Api.Auth;
using Escola.Domain.Enums;
using Escola.Infrastructure.Data;
using Escola.Infrastructure.Pagamentos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Escola.Api.Controllers;

public record PixAutomaticoStatusDto(
    bool Configurado,
    string Ambiente,
    bool ChavePixConfigurada,
    bool CertificadoConfigurado,
    bool WebhookHabilitado,
    bool WebhookUrlBaseConfigurada,
    int IntervaloConciliacaoSegundos,
    int CobrancasAguardando,
    int PagasAutomaticamente);

/// <summary>Integração com a API Pix do Banco do Brasil (baixa automática). Status/teste/registro de webhook são pra quem
/// administra; o recebimento do webhook é anônimo de propósito (quem chama é o banco), por isso se protege de outro jeito.</summary>
[ApiController]
[Route("api/pix")]
[Authorize]
public class PixController(
    EscolaDbContext db,
    IProvedorPix provedor,
    IPixAutomaticoService pixAutomatico,
    IOptions<OpcoesPixBb> opcoes,
    ClienteAtual clienteAtual,
    ILogger<PixController> logger) : ControllerBase
{
    [HttpGet("status")]
    [Authorize(Roles = GruposDePapeis.FinanceiroOuSuporte)]
    public async Task<ActionResult<PixAutomaticoStatusDto>> Status()
    {
        var o = opcoes.Value;
        var config = await db.ConfiguracoesFinanceiras.FirstOrDefaultAsync();
        return Ok(new PixAutomaticoStatusDto(
            provedor.Configurado, // só verdadeiro pro cliente dono das credenciais do BB
            o.Producao ? "Produção" : "Homologação",
            !string.IsNullOrWhiteSpace(config?.PixChave),
            !string.IsNullOrWhiteSpace(o.CertificadoPfxCaminho),
            o.WebhookHabilitado,
            !string.IsNullOrWhiteSpace(o.WebhookUrlBase),
            o.IntervaloConciliacaoSegundos,
            await db.CobrancasPix.CountAsync(p => p.Status == StatusCobrancaPix.Ativa),
            await db.CobrancasPix.CountAsync(p => p.Status == StatusCobrancaPix.Concluida)));
    }

    /// <summary>Pede um token ao banco: prova que credenciais, certificado e endereços estão certos, sem criar nem mudar nada.</summary>
    [HttpPost("testar-conexao")]
    [Authorize(Roles = GruposDePapeis.GestaoOuSuporte)]
    public async Task<IActionResult> TestarConexao()
    {
        if (!provedor.Configurado)
            return BadRequest("A integração com o Banco do Brasil não está configurada neste ambiente (Pix:Bb).");

        try
        {
            await provedor.TestarConexaoAsync();
            return Ok();
        }
        catch (PixProvedorException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>Cadastra no banco o endereço que ele deve avisar quando um Pix for pago. Opcional: sem webhook, a conciliação
    /// periódica já dá a baixa (só demora até o próximo ciclo).</summary>
    [HttpPost("webhook/registrar")]
    [Authorize(Roles = GruposDePapeis.GestaoOuSuporte)]
    public async Task<IActionResult> RegistrarWebhook()
    {
        var o = opcoes.Value;
        if (!provedor.Configurado) return BadRequest("A integração com o Banco do Brasil não está configurada neste ambiente (Pix:Bb).");
        if (!o.WebhookHabilitado || string.IsNullOrWhiteSpace(o.WebhookUrlBase))
            return BadRequest("Defina Pix:Bb:WebhookUrlBase (endereço público da API) e Pix:Bb:WebhookSegredo para usar o webhook.");

        var config = await db.ConfiguracoesFinanceiras.FirstOrDefaultAsync();
        if (string.IsNullOrWhiteSpace(config?.PixChave))
            return BadRequest("Configure a chave Pix da escola antes de registrar o webhook.");

        try
        {
            await provedor.RegistrarWebhookAsync(config.PixChave, $"{o.WebhookUrlBase.TrimEnd('/')}/api/pix/webhook/{o.WebhookSegredo}");
            return Ok();
        }
        catch (PixProvedorException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>Aviso do banco de que Pix foram recebidos (o BB acrescenta <c>/pix</c> ao endereço cadastrado). <b>O conteúdo do
    /// aviso nunca é confiado</b>: serve só pra dizer quais cobranças conferir — cada uma é consultada no banco antes da baixa.
    /// Por isso um aviso forjado, no máximo, faz uma consulta à toa. O segredo no caminho só reduz esse ruído.</summary>
    [HttpPost("webhook/{segredo}")]
    [HttpPost("webhook/{segredo}/pix")]
    [AllowAnonymous]
    public async Task<IActionResult> ReceberWebhook(string segredo, [FromBody] JsonElement corpo)
    {
        var o = opcoes.Value;
        if (!o.WebhookHabilitado || !SegredoConfere(segredo, o.WebhookSegredo!))
            return NotFound();

        // Aviso sem login: o cliente é o dono das credenciais do BB (OpcoesPixBb.ClienteId) — só ele tem cobranças no banco.
        clienteAtual.Definir(o.ClienteId!.Value);

        if (corpo.ValueKind == JsonValueKind.Object && corpo.TryGetProperty("pix", out var lista) && lista.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in lista.EnumerateArray())
            {
                var txid = item.TryGetProperty("txid", out var t) ? t.GetString() : null;
                if (string.IsNullOrWhiteSpace(txid)) continue;
                try
                {
                    await pixAutomatico.ConfirmarAsync(txid);
                }
                catch (PixProvedorException ex)
                {
                    // A conciliação periódica tenta de novo; o banco não precisa saber da nossa falha.
                    logger.LogWarning(ex, "Webhook Pix: não foi possível conferir a cobrança {TxId}.", txid);
                }
            }
        }

        return Ok();
    }

    private static bool SegredoConfere(string recebido, string esperado) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(recebido), Encoding.UTF8.GetBytes(esperado));
}
