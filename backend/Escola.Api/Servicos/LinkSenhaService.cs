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
    Convite,
    /// <summary>Convite de responsável feito pela Matrícula: mesmo link de 7 dias do <see cref="Convite"/>, mas o e-mail fala da matrícula
    /// e avisa que ela só vale depois do aceite do termo no portal.</summary>
    ConviteMatricula
}

/// <param name="Gerado">O token foi criado (se falso, não há link nenhum — ex.: falta <c>App:UrlBase</c>).</param>
/// <param name="Entregue">O e-mail foi de fato enviado (falso sem SMTP configurado, ou se o envio falhou).</param>
/// <param name="Link">O link gerado. Segredo (equivale a definir a senha da conta): só deve ser mostrado a quem pode entregá-lo ao dono
/// quando o e-mail não saiu.</param>
public sealed record ResultadoLinkSenha(bool Gerado, bool Entregue, string? Aviso, string? Link = null);

public interface ILinkSenhaService
{
    /// <summary>Cria um link de uso único pra <paramref name="usuario"/> definir a senha e o manda por e-mail. Um link novo invalida os
    /// anteriores ainda abertos da mesma conta. Nunca lança por falha de entrega: devolve o que aconteceu.</summary>
    Task<ResultadoLinkSenha> EnviarAsync(Usuario usuario, TipoLinkSenha tipo, string? nomeCliente = null, IReadOnlyList<string>? nomesAlunos = null);
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

    public async Task<ResultadoLinkSenha> EnviarAsync(Usuario usuario, TipoLinkSenha tipo, string? nomeCliente = null, IReadOnlyList<string>? nomesAlunos = null)
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

        var convite = tipo is TipoLinkSenha.Convite or TipoLinkSenha.ConviteMatricula;
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
            return new ResultadoLinkSenha(true, false, "O envio de e-mail não está configurado neste ambiente (SMTP): o link não foi entregue.", link);
        }

        try
        {
            var (assunto, corpo) = tipo switch
            {
                TipoLinkSenha.ConviteMatricula => MontarConviteMatricula(usuario.Nome, nomeCliente, nomesAlunos ?? [], link),
                TipoLinkSenha.Convite => MontarConvite(usuario.Nome, nomeCliente, link),
                _ => MontarRedefinicao(usuario.Nome, nomeCliente, link)
            };
            await emailSender.EnviarAsync(usuario.Email, usuario.Nome, assunto, corpo);
            return new ResultadoLinkSenha(true, true, null, link);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao enviar o e-mail com o link de senha.");
            return new ResultadoLinkSenha(true, false, "Não foi possível enviar o e-mail agora. Tente reenviar em instantes.", link);
        }
    }

    public static string HashDoToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    private static (string Assunto, string Corpo) MontarRedefinicao(string nome, string? nomeCliente, string link)
    {
        var nomeSeguro = WebUtility.HtmlEncode(nome);
        var linkSeguro = WebUtility.HtmlEncode(link);
        // O mesmo e-mail pode ter conta em mais de uma escola (cada uma recebe o próprio link): o nome da escola diz qual é qual.
        var daEscola = string.IsNullOrWhiteSpace(nomeCliente) ? string.Empty : $" em <strong>{WebUtility.HtmlEncode(nomeCliente)}</strong>";
        return (string.IsNullOrWhiteSpace(nomeCliente) ? "Redefinição de senha" : $"Redefinição de senha — {nomeCliente}", $"""
                <p>Olá, {nomeSeguro}!</p>
                <p>Recebemos um pedido para redefinir a senha da sua conta{daEscola}. Para criar uma nova senha, clique no link abaixo:</p>
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

    private static (string Assunto, string Corpo) MontarConviteMatricula(string nome, string? nomeCliente, IReadOnlyList<string> nomesAlunos, string link)
    {
        var nomeSeguro = WebUtility.HtmlEncode(nome);
        var clienteSeguro = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(nomeCliente) ? "a escola" : nomeCliente);
        var alunosSeguro = WebUtility.HtmlEncode(nomesAlunos.Count == 0 ? "seu(sua) filho(a)" : string.Join(", ", nomesAlunos));
        var linkSeguro = WebUtility.HtmlEncode(link);
        return ($"Confirme a matrícula — {nomeCliente ?? "Rotina Escola"}", $"""
                <p>Olá, {nomeSeguro}!</p>
                <p>A matrícula de <strong>{alunosSeguro}</strong> foi registrada por <strong>{clienteSeguro}</strong>.</p>
                <p>Para confirmar este e-mail e criar a sua senha de acesso ao portal, clique no link abaixo:</p>
                <p><a href="{linkSeguro}">Confirmar e-mail e criar minha senha</a></p>
                <p>Depois, ao entrar no portal, leia e aceite o termo de matrícula. <strong>A matrícula só é efetivada depois desse aceite.</strong></p>
                <p>O link vale por 7 dias e só pode ser usado uma vez. Se você não reconhece esta matrícula, ignore este e-mail e fale com a escola.</p>
                """);
    }
}
