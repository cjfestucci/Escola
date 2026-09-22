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

    public async Task EnviarAsync(string destinatarioEmail, string destinatarioNome, string assunto, string corpoHtml, CancellationToken ct = default)
    {
        if (!Configurado)
            throw new InvalidOperationException("Envio de e-mail não configurado (faltam credenciais de SMTP).");

        var mensagem = new MimeMessage();
        mensagem.From.Add(new MailboxAddress(_config.RemetenteNome, _config.RemetenteEmail));
        mensagem.To.Add(new MailboxAddress(destinatarioNome, destinatarioEmail));
        mensagem.Subject = assunto;
        mensagem.Body = new BodyBuilder { HtmlBody = corpoHtml }.ToMessageBody();

        using var cliente = new SmtpClient();
        await cliente.ConnectAsync(_config.Host, _config.Porta, SecureSocketOptions.StartTls, ct);
        await cliente.AuthenticateAsync(_config.Usuario, _config.Senha, ct);
        await cliente.SendAsync(mensagem, ct);
        await cliente.DisconnectAsync(true, ct);
    }
}
