using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Escola.Infrastructure.Email;

public class SmtpEmailSender(IOptions<ConfiguracaoSmtp> opcoes) : IEmailSender
{
    private readonly ConfiguracaoSmtp _config = opcoes.Value;

    public bool Configurado =>
        !string.IsNullOrWhiteSpace(_config.Host) &&
        !string.IsNullOrWhiteSpace(_config.Usuario) &&
        !string.IsNullOrWhiteSpace(_config.Senha) &&
        !string.IsNullOrWhiteSpace(_config.RemetenteEmail);

    public async Task EnviarAsync(string destinatarioEmail, string destinatarioNome, string assunto, string corpoHtml, CancellationToken ct = default,
        string? responderPara = null)
    {
        if (!Configurado)
            throw new InvalidOperationException("Envio de e-mail não configurado (faltam credenciais de SMTP).");

        var mensagem = new MimeMessage();
        mensagem.From.Add(new MailboxAddress(_config.RemetenteNome, _config.RemetenteEmail!));
        // Modo seguro (dev): nada sai pra destinatário de verdade; o original fica visível no assunto.
        var redirecionar = _config.RedirecionarTudoPara;
        if (!string.IsNullOrWhiteSpace(redirecionar))
        {
            mensagem.To.Add(new MailboxAddress(destinatarioNome, redirecionar));
            assunto = $"[TESTE → {destinatarioEmail}] {assunto}";
        }
        else
        {
            mensagem.To.Add(new MailboxAddress(destinatarioNome, destinatarioEmail));
        }
        // No modo seguro o "responder" também fica no endereço de teste (senão a resposta iria pro e-mail fictício).
        if (!string.IsNullOrWhiteSpace(responderPara))
            mensagem.ReplyTo.Add(MailboxAddress.Parse(string.IsNullOrWhiteSpace(redirecionar) ? responderPara : redirecionar));
        mensagem.Subject = assunto;
        // Todo e-mail sai no layout da marca (ModeloEmail), com a versão em texto puro junto.
        mensagem.Body = new BodyBuilder
        {
            HtmlBody = ModeloEmail.Html(assunto, corpoHtml),
            TextBody = ModeloEmail.Texto(corpoHtml)
        }.ToMessageBody();

        using var cliente = new SmtpClient();
        await cliente.ConnectAsync(_config.Host!, _config.Porta, SecureSocketOptions.StartTls, ct);
        await cliente.AuthenticateAsync(_config.Usuario!, _config.Senha!, ct);
        await cliente.SendAsync(mensagem, ct);
        await cliente.DisconnectAsync(true, ct);
    }
}
