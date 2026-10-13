using System.Security.Cryptography;
using System.Text;
using Escola.Domain.Entities;
using Escola.Infrastructure;
using Escola.Infrastructure.Email;

namespace Escola.Api.Servicos;

/// <param name="Enviado">O e-mail com o código foi entregue (em desenvolvimento sem SMTP, o código vai pro log e isto é falso).</param>
/// <param name="Aguardar">Pediram outro código antes do intervalo mínimo: nada foi enviado e o código anterior continua valendo.</param>
public record ResultadoCodigoEmail(bool Enviado, bool Aguardar, string? Aviso);

/// <summary>Verificação em duas etapas por código enviado por e-mail: 6 dígitos, vale 10 minutos, uso único, guardado só como hash.
/// Mais fraca que o app autenticador (quem invade o e-mail também pede "esqueci a senha"), mas protege contra o caso mais comum —
/// senha vazada de outro site — e é a que o cliente consegue usar sem instalar nada.</summary>
public sealed class CodigoEmailService(IEmailSender emailSender, IHostEnvironment ambiente, ILogger<CodigoEmailService> logger)
{
    public static readonly TimeSpan Validade = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan IntervaloMinimo = TimeSpan.FromSeconds(60);

    /// <summary>Dá pra oferecer o método: há SMTP (ou é desenvolvimento, onde o código sai no log).</summary>
    public bool Disponivel => emailSender.Configurado || ambiente.IsDevelopment();

    /// <summary>Gera e envia um código novo (invalida o anterior). Não salva: quem chama faz o <c>SaveChanges</c>.</summary>
    public async Task<ResultadoCodigoEmail> EnviarAsync(Usuario usuario, string? nomeEscola)
    {
        var agora = DateTime.UtcNow;
        if (usuario.CodigoEmailEnviadoEm is { } ultimo && agora - ultimo < IntervaloMinimo && usuario.CodigoEmailHash is not null)
            return new ResultadoCodigoEmail(false, true, "Já enviamos um código há pouco. Confira sua caixa de entrada (e o spam) ou aguarde um minuto para pedir outro.");

        var codigo = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        usuario.CodigoEmailHash = Hash(usuario.Id, codigo);
        usuario.CodigoEmailExpiraEm = agora + Validade;
        usuario.CodigoEmailEnviadoEm = agora;

        if (!emailSender.Configurado)
        {
            // Sem SMTP o código não chega. Em desenvolvimento ele vai pro log pra dar pra testar; fora dele é segredo.
            if (ambiente.IsDevelopment())
                logger.LogWarning("SMTP não configurado. Código de verificação para {Email} (somente desenvolvimento): {Codigo}", usuario.Email, codigo);
            else
                logger.LogError("Código de verificação por e-mail gerado, mas o SMTP não está configurado — nada foi enviado.");
            return new ResultadoCodigoEmail(false, false, "O envio de e-mail não está configurado neste ambiente: o código não foi entregue.");
        }

        try
        {
            var escola = string.IsNullOrWhiteSpace(nomeEscola) ? MarcaProduto.Nome : nomeEscola;
            var corpo = $"""
                <p>Olá, {Html(usuario.Nome)}!</p>
                <p>Seu código para entrar em <b>{Html(escola)}</b> é:</p>
                <p style="font-size:28px;font-weight:700;letter-spacing:6px">{codigo}</p>
                <p>Ele vale por {(int)Validade.TotalMinutes} minutos e só pode ser usado uma vez.</p>
                <p>Se não foi você que tentou entrar, troque sua senha: alguém pode ter descoberto a senha atual.</p>
                """;
            await emailSender.EnviarAsync(usuario.Email, usuario.Nome, $"{codigo} é seu código de verificação — {escola}", corpo);
            return new ResultadoCodigoEmail(true, false, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao enviar o código de verificação por e-mail.");
            return new ResultadoCodigoEmail(false, false, "Não foi possível enviar o e-mail agora. Tente de novo em instantes.");
        }
    }

    /// <summary>Confere o código e, se valer, consome (uso único). Não salva.</summary>
    public static bool TentarUsar(Usuario usuario, string? codigo)
    {
        var limpo = new string((codigo ?? string.Empty).Where(char.IsDigit).ToArray());
        if (limpo.Length != 6 || usuario.CodigoEmailHash is null || usuario.CodigoEmailExpiraEm is not { } expira || expira < DateTime.UtcNow)
            return false;
        var esperado = Encoding.ASCII.GetBytes(usuario.CodigoEmailHash);
        var recebido = Encoding.ASCII.GetBytes(Hash(usuario.Id, limpo));
        if (!CryptographicOperations.FixedTimeEquals(esperado, recebido)) return false;

        usuario.CodigoEmailHash = null;
        usuario.CodigoEmailExpiraEm = null;
        return true;
    }

    /// <summary>"f***@escola.com" — pra tela dizer pra onde foi o código sem expor o endereço inteiro a quem só sabe a senha.</summary>
    public static string Mascarar(string email)
    {
        var arroba = email.IndexOf('@');
        if (arroba <= 0) return email;
        return email[0] + new string('*', Math.Max(3, arroba - 1)) + email[arroba..];
    }

    private static string Hash(Guid usuarioId, string codigo) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{usuarioId:N}:{codigo}")));

    private static string Html(string texto) => System.Net.WebUtility.HtmlEncode(texto);
}
