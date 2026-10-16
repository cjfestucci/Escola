namespace Escola.Infrastructure.Email;

public interface IEmailSender
{
    /// <summary>Se falso, nenhuma credencial de SMTP foi configurada — EnviarAsync sempre lançaria.</summary>
    bool Configurado { get; }

    /// <param name="responderPara">E-mail que recebe a resposta (cabeçalho Reply-To) — ex.: o Fale conosco, pra responder direto a quem escreveu.</param>
    Task EnviarAsync(string destinatarioEmail, string destinatarioNome, string assunto, string corpoHtml, CancellationToken ct = default,
        string? responderPara = null);
}
