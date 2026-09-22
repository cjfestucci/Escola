namespace Escola.Infrastructure.Email;

public interface IEmailSender
{
    /// <summary>Se falso, nenhuma credencial de SMTP foi configurada — EnviarAsync sempre lançaria.</summary>
    bool Configurado { get; }

    Task EnviarAsync(string destinatarioEmail, string destinatarioNome, string assunto, string corpoHtml, CancellationToken ct = default);
}
