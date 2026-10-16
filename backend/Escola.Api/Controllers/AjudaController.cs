using System.Net;
using Escola.Api.Auth;
using Escola.Domain.Enums;
using Escola.Infrastructure;
using Escola.Infrastructure.Auth;
using Escola.Infrastructure.Clientes;
using Escola.Infrastructure.Data;
using Escola.Infrastructure.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Escola.Api.Controllers;

public record FaleConoscoRequest(string? Assunto, string? Mensagem, string? Pagina);

public record ContatoUnidadeDto(Guid Id, string Nome, string? Endereco, string? Telefone, string? WhatsApp, string? Email);
public record MensagemClubeRequest(Guid UnidadeId, string? Assunto, string? Mensagem);
/// <param name="Atletas">Atletas ativos do responsável logado, "Nome (Categoria)" — pra mensagem inicial do WhatsApp dizer de quem se trata.</param>
public record ContatoClubeDto(string NomeClube, List<ContatoUnidadeDto> Unidades, List<string> Atletas);

/// <summary>Ajuda: o Fale conosco da equipe do clube manda um e-mail pro suporte do produto (<c>Ajuda:EmailSuporte</c>), com o contexto
/// de quem escreveu (clube, conta, perfil, tela) e o Reply-To na pessoa — o suporte responde direto da caixa de entrada. Quem escreveu
/// recebe uma cópia. Os pais não falam com o produto: o portal mostra o contato do próprio clube (<see cref="ContatoClubeController"/>).</summary>
[ApiController]
[Route("api/ajuda")]
[Authorize(Roles = GruposDePapeis.Equipe)]
public class AjudaController(
    EscolaDbContext db,
    IClienteAtual clienteAtual,
    IEmailSender email,
    LimitadorTentativasLogin limitador,
    IConfiguration config,
    ILogger<AjudaController> logger) : ControllerBase
{
    /// <summary>Assuntos aceitos (o mesmo texto da lista da tela).</summary>
    public static readonly string[] Assuntos = ["Dúvida de uso", "Problema ou erro", "Sugestão", "Assinatura e pagamento", "Outro"];

    public const int MensagemMinima = 10;
    public const int MensagemMaxima = 4000;
    /// <summary>Mensagens por conta a cada 15 minutos (o freio é o mesmo do login): o suficiente pra quem precisa, pouco pra virar spam.</summary>
    public const int MensagensPorJanela = 5;

    public const string EmailSuportePadrao = "suporte@gestortatico.com.br";

    [HttpPost("fale-conosco")]
    public async Task<IActionResult> FaleConosco(FaleConoscoRequest request, CancellationToken ct)
    {
        var assunto = request.Assunto?.Trim();
        if (assunto is null || !Assuntos.Contains(assunto)) return BadRequest("Escolha o assunto da mensagem.");
        var mensagem = request.Mensagem?.Trim() ?? string.Empty;
        if (mensagem.Length < MensagemMinima) return BadRequest("Escreva a mensagem com um pouco mais de detalhe.");
        if (mensagem.Length > MensagemMaxima) return BadRequest($"A mensagem pode ter no máximo {MensagemMaxima} caracteres.");

        var destino = config["Ajuda:EmailSuporte"];
        if (string.IsNullOrWhiteSpace(destino)) destino = EmailSuportePadrao;
        if (!email.Configurado)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, $"O envio de mensagens está indisponível agora. Escreva para {destino}.");

        var usuarioId = this.UsuarioIdAtual();
        var chave = $"fale-conosco:{usuarioId}";
        if (limitador.EstaBloqueado(chave, MensagensPorJanela, out var restante))
            return StatusCode(StatusCodes.Status429TooManyRequests,
                $"Você já enviou várias mensagens. Tente de novo em {Math.Max(1, (int)Math.Ceiling(restante.TotalMinutes))} minutos.");

        var usuario = await db.Usuarios.AsNoTracking().FirstAsync(u => u.Id == usuarioId, ct);
        var clube = await db.Clientes.AsNoTracking().Where(c => c.Id == clienteAtual.Id).Select(c => c.Nome).FirstAsync(ct);
        var pagina = string.IsNullOrWhiteSpace(request.Pagina) ? null : request.Pagina.Trim()[..Math.Min(request.Pagina.Trim().Length, 200)];
        var navegador = Request.Headers.UserAgent.ToString();

        var corpoSuporte = $"""
            <p><strong>{Html(assunto)}</strong> — mensagem de {Html(usuario.Nome)} pelo Fale conosco.</p>
            <p>{Paragrafos(mensagem)}</p>
            <p style="font-size:13px;color:#555">
            Clube: {Html(clube)} ({clienteAtual.Id})<br>
            Conta: {Html(usuario.Nome)} &lt;{Html(usuario.Email)}&gt; — {RotuloPapel(usuario.Papel)}<br>
            Tela de onde escreveu: {Html(pagina ?? "não informada")}<br>
            Navegador: {Html(string.IsNullOrWhiteSpace(navegador) ? "não informado" : navegador[..Math.Min(navegador.Length, 300)])}
            </p>
            <p style="font-size:13px;color:#555">Responda este e-mail para falar direto com {Html(usuario.Nome)}.</p>
            """;
        try
        {
            await email.EnviarAsync(destino, $"Suporte {MarcaProduto.Nome}", $"[Fale conosco] {assunto} — {clube}", corpoSuporte, ct,
                responderPara: usuario.Email);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Falha ao enviar o Fale conosco da conta {Usuario}.", usuarioId);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, $"Não foi possível enviar agora. Tente de novo ou escreva para {destino}.");
        }
        limitador.RegistrarFalha(chave); // conta todo envio aceito, não só falhas

        // A cópia pra quem escreveu é cortesia: se falhar, a mensagem já chegou ao suporte.
        try
        {
            await email.EnviarAsync(usuario.Email, usuario.Nome, $"Recebemos sua mensagem — {MarcaProduto.Nome}", $"""
                <p>Olá, {Html(usuario.Nome)}!</p>
                <p>Recebemos sua mensagem sobre <strong>{Html(assunto)}</strong> e vamos responder neste e-mail, normalmente em até 1 dia útil.</p>
                <p style="font-size:13px;color:#555">Sua mensagem:<br>{Paragrafos(mensagem)}</p>
                """, ct, responderPara: destino);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Fale conosco enviado, mas a cópia para a conta {Usuario} falhou.", usuarioId);
        }

        logger.LogInformation("Fale conosco enviado pela conta {Usuario} do cliente {Cliente}: {Assunto}.", usuarioId, clienteAtual.Id, assunto);
        return NoContent();
    }

    internal static string Html(string texto) => WebUtility.HtmlEncode(texto);

    /// <summary>Texto livre em HTML seguro, mantendo as quebras de linha.</summary>
    internal static string Paragrafos(string texto) => Html(texto).Replace("\r\n", "\n").Replace("\n", "<br>");

    private static string RotuloPapel(PapelUsuario papel) => papel switch
    {
        PapelUsuario.Admin => "Administrador",
        PapelUsuario.Coordenador => "Coordenação",
        PapelUsuario.Educador => "Professor",
        PapelUsuario.Financeiro => "Financeiro",
        PapelUsuario.Secretaria => "Secretaria",
        _ => papel.ToString()
    };
}

/// <summary>"Fale com o clube" do portal: os pais falam com o clube, não com o produto. Mostra o nome do clube e os contatos das
/// unidades ativas (endereço, telefone, WhatsApp e e-mail, que o próprio clube cadastra em Unidades) e, nas que têm e-mail, deixa o
/// pai mandar uma mensagem pelo próprio portal: chega no e-mail da unidade com Reply-To no pai e com os atletas dele (nome e
/// categoria), pro clube saber de quem se trata sem perguntar.</summary>
[ApiController]
[Route("api/portal/contato-clube")]
[Authorize(Roles = GruposDePapeis.Responsavel)]
public class ContatoClubeController(
    EscolaDbContext db,
    IClienteAtual clienteAtual,
    IEmailSender email,
    LimitadorTentativasLogin limitador,
    ILogger<ContatoClubeController> logger) : ControllerBase
{
    /// <summary>Assuntos aceitos (o mesmo texto da lista da tela).</summary>
    public static readonly string[] Assuntos = ["Mensalidade e pagamento", "Treinos e jogos", "Cadastro e documentos", "Outro"];

    [HttpGet]
    public async Task<ActionResult<ContatoClubeDto>> Obter(CancellationToken ct)
    {
        var clube = await db.Clientes.AsNoTracking().Where(c => c.Id == clienteAtual.Id).Select(c => c.Nome).FirstAsync(ct);
        var unidades = await db.Unidades.AsNoTracking().Where(u => u.Ativa).OrderBy(u => u.Nome)
            .Select(u => new ContatoUnidadeDto(u.Id, u.Nome, u.Endereco, u.Telefone, u.WhatsApp, u.Email)).ToListAsync(ct);
        return Ok(new ContatoClubeDto(clube, unidades, await AtletasDoResponsavelAsync(ct)));
    }

    /// <summary>Atletas ativos do responsável logado, "Nome (Categoria)" — só os filhos dele.</summary>
    private Task<List<string>> AtletasDoResponsavelAsync(CancellationToken ct)
    {
        var responsavelId = this.ResponsavelIdAtual();
        return db.AlunoResponsaveis.AsNoTracking().Where(ar => ar.ResponsavelId == responsavelId && ar.Aluno.Ativo)
            .OrderBy(ar => ar.Aluno.Nome).Select(ar => ar.Aluno.Nome + " (" + ar.Aluno.Turma.Nome + ")").ToListAsync(ct);
    }

    [HttpPost("mensagem")]
    public async Task<IActionResult> EnviarMensagem(MensagemClubeRequest request, CancellationToken ct)
    {
        var assunto = request.Assunto?.Trim();
        if (assunto is null || !Assuntos.Contains(assunto)) return BadRequest("Escolha o assunto da mensagem.");
        var mensagem = request.Mensagem?.Trim() ?? string.Empty;
        if (mensagem.Length < AjudaController.MensagemMinima) return BadRequest("Escreva a mensagem com um pouco mais de detalhe.");
        if (mensagem.Length > AjudaController.MensagemMaxima) return BadRequest($"A mensagem pode ter no máximo {AjudaController.MensagemMaxima} caracteres.");

        var unidade = await db.Unidades.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.UnidadeId && u.Ativa, ct);
        if (unidade is null) return NotFound("Unidade não encontrada.");
        if (string.IsNullOrWhiteSpace(unidade.Email)) return BadRequest("Esta unidade não tem e-mail de contato. Use o telefone ou o WhatsApp.");
        if (!email.Configurado)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, $"O envio de mensagens está indisponível agora. Escreva para {unidade.Email}.");

        var usuarioId = this.UsuarioIdAtual();
        var chave = $"fale-com-clube:{usuarioId}";
        if (limitador.EstaBloqueado(chave, AjudaController.MensagensPorJanela, out var restante))
            return StatusCode(StatusCodes.Status429TooManyRequests,
                $"Você já enviou várias mensagens. Tente de novo em {Math.Max(1, (int)Math.Ceiling(restante.TotalMinutes))} minutos.");

        var usuario = await db.Usuarios.AsNoTracking().FirstAsync(u => u.Id == usuarioId, ct);
        var atletas = await AtletasDoResponsavelAsync(ct);
        var clube = await db.Clientes.AsNoTracking().Where(c => c.Id == clienteAtual.Id).Select(c => c.Nome).FirstAsync(ct);

        var corpo = $"""
            <p><strong>{AjudaController.Html(assunto)}</strong> — mensagem de {AjudaController.Html(usuario.Nome)} pelo portal da família.</p>
            <p>{AjudaController.Paragrafos(mensagem)}</p>
            <p style="font-size:13px;color:#555">
            Responsável: {AjudaController.Html(usuario.Nome)} &lt;{AjudaController.Html(usuario.Email)}&gt;<br>
            {(atletas.Count == 1 ? "Atleta" : "Atletas")}: {AjudaController.Html(atletas.Count == 0 ? "nenhum ativo" : string.Join(", ", atletas))}<br>
            Unidade: {AjudaController.Html(unidade.Nome)}
            </p>
            <p style="font-size:13px;color:#555">Responda este e-mail para falar direto com {AjudaController.Html(usuario.Nome)}.</p>
            """;
        try
        {
            await email.EnviarAsync(unidade.Email, clube, $"[Portal] {assunto} — {usuario.Nome}", corpo, ct, responderPara: usuario.Email);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Falha ao enviar a mensagem do portal da conta {Usuario}.", usuarioId);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, $"Não foi possível enviar agora. Tente de novo ou escreva para {unidade.Email}.");
        }
        limitador.RegistrarFalha(chave); // conta todo envio aceito

        try
        {
            await email.EnviarAsync(usuario.Email, usuario.Nome, $"Sua mensagem foi enviada — {clube}", $"""
                <p>Olá, {AjudaController.Html(usuario.Nome)}!</p>
                <p>Sua mensagem sobre <strong>{AjudaController.Html(assunto)}</strong> foi enviada para {AjudaController.Html(clube)}.
                A resposta chega neste e-mail.</p>
                <p style="font-size:13px;color:#555">Sua mensagem:<br>{AjudaController.Paragrafos(mensagem)}</p>
                """, ct, responderPara: unidade.Email);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Mensagem do portal enviada, mas a cópia para a conta {Usuario} falhou.", usuarioId);
        }
        return NoContent();
    }
}
