using System.Net;
using System.Security.Cryptography;
using System.Text;
using Escola.Domain.Entities;
using Escola.Infrastructure.Data;
using Escola.Infrastructure.Email;
using Microsoft.EntityFrameworkCore;

namespace Escola.Api.Servicos;

public enum TipoLinkSenha
{
    /// <summary>"Esqueci minha senha" ou pedido do suporte pra quem já tem conta ativa: link de 1 hora.</summary>
    Redefinicao,
    /// <summary>Convite de uma conta nova (ex.: o Admin da escola criado pelo Suporte): confirma o e-mail e cadastra a primeira senha.
    /// Link de 7 dias — a pessoa precisa de tempo pra ver o e-mail.</summary>
    Convite
}

/// <param name="Gerado">O token foi criado (se falso, não há link nenhum — ex.: falta <c>App:UrlBase</c>).</param>
/// <param name="Entregue">O e-mail foi de fato enviado (falso sem SMTP configurado, ou se o envio falhou).</param>
public sealed record ResultadoLinkSenha(bool Gerado, bool Entregue, string? Aviso);

public interface ILinkSenhaService
{
    /// <summary>Cria um link de uso único pra <paramref name="usuario"/> definir a senha e o manda por e-mail. Um link novo invalida os
    /// anteriores ainda abertos da mesma conta. Nunca lança por falha de entrega: devolve o que aconteceu.</summary>
    Task<ResultadoLinkSenha> EnviarAsync(Usuario usuario, TipoLinkSenha tipo, string? nomeCliente = null);
}

public sealed class LinkSenhaService(
    EscolaDbContext db,
    IConfiguration config,
    IEmailSender emailSender,
    IHostEnvironment ambiente,
    ILogger<LinkSenhaService> logger) : ILinkSenhaService
{
    private static readonly TimeSpan ValidadeRedefinicao = TimeSpan.FromHours(1);
    private static readonly TimeSpan ValidadeConvite = TimeSpan.FromDays(7);

    public async Task<ResultadoLinkSenha> EnviarAsync(Usuario usuario, TipoLinkSenha tipo, string? nomeCliente = null)
    {
        // O endereço do site vem da configuração do deploy — nunca do cabeçalho da requisição (Origin/Host), que o atacante
        // controla: usar ele deixaria alguém pedir o link de outra pessoa e fazer o e-mail apontar pro site dele.
        var urlBase = config["App:UrlBase"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(urlBase))
        {
            logger.LogError("Link de senha pedido, mas App:UrlBase não está configurado — nenhum e-mail foi enviado.");
            return new ResultadoLinkSenha(false, false, "O endereço do site (App:UrlBase) não está configurado neste ambiente.");
        }

        var agora = DateTime.UtcNow;
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace('+', '-').Replace('/', '_').TrimEnd('=');

        var anteriores = await db.RedefinicoesSenha.Where(r => r.UsuarioId == usuario.Id && r.UsadoEm == null).ToListAsync();
        foreach (var anterior in anteriores) anterior.UsadoEm = agora;

        var convite = tipo == TipoLinkSenha.Convite;
        db.RedefinicoesSenha.Add(new RedefinicaoSenha
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            TokenHash = HashDoToken(token),
            CriadoEm = agora,
            ExpiraEm = agora + (convite ? ValidadeConvite : ValidadeRedefinicao),
            Convite = convite
        });
        await db.SaveChangesAsync();

        var link = $"{urlBase}/redefinir-senha?token={token}{(convite ? "&convite=1" : string.Empty)}";

        if (!emailSender.Configurado)
        {
            // Sem SMTP não há como entregar. Em desenvolvimento o link aparece no log pra poder testar o fluxo; fora dele o link
            // é segredo e não vai pro log.
            if (ambiente.IsDevelopment())
                logger.LogWarning("SMTP não configurado. Link de {Tipo} para {Email} (somente desenvolvimento): {Link}", tipo, usuario.Email, link);
            else
                logger.LogError("Link de senha criado para uma conta, mas o SMTP não está configurado — nenhum e-mail foi enviado.");
            return new ResultadoLinkSenha(true, false, "O envio de e-mail não está configurado neste ambiente (SMTP): o link não foi entregue.");
        }

        try
        {
            var (assunto, corpo) = convite ? MontarConvite(usuario.Nome, nomeCliente, link) : MontarRedefinicao(usuario.Nome, link);
            await emailSender.EnviarAsync(usuario.Email, usuario.Nome, assunto, corpo);
            return new ResultadoLinkSenha(true, true, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao enviar o e-mail com o link de senha.");
            return new ResultadoLinkSenha(true, false, "Não foi possível enviar o e-mail agora. Tente reenviar em instantes.");
        }
    }

    public static string HashDoToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    private static (string Assunto, string Corpo) MontarRedefinicao(string nome, string link)
    {
        var nomeSeguro = WebUtility.HtmlEncode(nome);
        var linkSeguro = WebUtility.HtmlEncode(link);
        return ("Redefinição de senha", $"""
                <p>Olá, {nomeSeguro}!</p>
                <p>Recebemos um pedido para redefinir a senha da sua conta. Para criar uma nova senha, clique no link abaixo:</p>
                <p><a href="{linkSeguro}">Criar nova senha</a></p>
                <p>O link vale por 1 hora e só pode ser usado uma vez. Se você não pediu isso, pode ignorar este e-mail — sua senha continua a mesma.</p>
                """);
    }

    private static (string Assunto, string Corpo) MontarConvite(string nome, string? nomeCliente, string link)
    {
        var nomeSeguro = WebUtility.HtmlEncode(nome);
        var clienteSeguro = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(nomeCliente) ? "sua escola" : nomeCliente);
        var linkSeguro = WebUtility.HtmlEncode(link);
        return ($"Convite de acesso — {nomeCliente ?? "Rotina Escola"}", $"""
                <p>Olá, {nomeSeguro}!</p>
                <p>Foi criado um acesso de <strong>administrador</strong> para você no sistema de <strong>{clienteSeguro}</strong>.</p>
                <p>Para confirmar este e-mail e cadastrar a sua senha, clique no link abaixo:</p>
                <p><a href="{linkSeguro}">Confirmar e cadastrar minha senha</a></p>
                <p>O link vale por 7 dias e só pode ser usado uma vez. Se você não esperava este convite, pode ignorar este e-mail — nenhum acesso será liberado sem esse passo.</p>
                """);
    }
}
