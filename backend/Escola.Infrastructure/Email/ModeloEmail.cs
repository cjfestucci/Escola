using System.Net;
using System.Text.RegularExpressions;

namespace Escola.Infrastructure.Email;

/// <summary>Layout único de todos os e-mails, aplicado no envio (<see cref="SmtpEmailSender"/>): quem escreve um e-mail só monta o miolo com
/// <c>&lt;p&gt;</c>, e um link sozinho num parágrafo (<c>&lt;p&gt;&lt;a href="…"&gt;Texto&lt;/a&gt;&lt;/p&gt;</c>) vira o botão de ação.
/// Tudo com estilo inline e tabelas — é o que Gmail, Outlook e os apps de celular respeitam.</summary>
public static partial class ModeloEmail
{
    private const string CorMarca = "#0E2A3A";
    private const string CorFundo = "#F3F5F7";
    private const string CorTexto = "#1F2933";
    private const string CorSuave = "#6B7785";

    /// <param name="urlBase">Endereço público do app (de onde vem a imagem da marca). Sem ele, o cabeçalho sai só com o nome.</param>
    public static string Html(string assunto, string corpo, string? urlBase)
    {
        var miolo = EstilizarMiolo(corpo);
        var logo = string.IsNullOrWhiteSpace(urlBase)
            ? string.Empty
            : $"""<img src="{WebUtility.HtmlEncode(urlBase.TrimEnd('/'))}/icons/icon-96x96.png" width="36" height="36" alt="" style="display:block;border:0;border-radius:8px">""";

        return $"""
            <!doctype html>
            <html lang="pt-BR">
            <head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>{WebUtility.HtmlEncode(assunto)}</title></head>
            <body style="margin:0;padding:0;background:{CorFundo};">
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="background:{CorFundo};">
              <tr><td align="center" style="padding:24px 12px;">
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="max-width:560px;">
                  <tr><td style="background:{CorMarca};border-radius:12px 12px 0 0;padding:16px 24px;">
                    <table role="presentation" cellpadding="0" cellspacing="0" border="0"><tr>
                      {(logo.Length > 0 ? $"<td style=\"padding-right:12px;\">{logo}</td>" : string.Empty)}
                      <td style="font-family:Arial,Helvetica,sans-serif;font-size:18px;font-weight:700;color:#FFFFFF;">{MarcaProduto.Nome}</td>
                    </tr></table>
                  </td></tr>
                  <tr><td style="background:#FFFFFF;padding:28px 24px 12px;font-family:Arial,Helvetica,sans-serif;font-size:15px;line-height:1.55;color:{CorTexto};border-radius:0 0 12px 12px;">
            {miolo}
                  </td></tr>
                  <tr><td style="padding:18px 24px;font-family:Arial,Helvetica,sans-serif;font-size:12px;line-height:1.5;color:{CorSuave};text-align:center;">
                    E-mail automático do {MarcaProduto.Nome} — por favor, não responda.<br>
                    <a href="https://gestortatico.com.br" style="color:{CorSuave};">gestortatico.com.br</a>
                  </td></tr>
                </table>
              </td></tr>
            </table>
            </body>
            </html>
            """;
    }

    /// <summary>Versão em texto puro (vai junto, em multipart): filtros de spam olham pra ela, e há leitor que só mostra ela.</summary>
    public static string Texto(string corpo)
    {
        var texto = LinkHtml().Replace(corpo, m => $"{WebUtility.HtmlDecode(m.Groups[2].Value)}: {WebUtility.HtmlDecode(m.Groups[1].Value)}");
        texto = Regex.Replace(texto, @"</p>|<br\s*/?>", "\n", RegexOptions.IgnoreCase);
        texto = Regex.Replace(texto, "<[^>]+>", string.Empty);
        texto = WebUtility.HtmlDecode(texto);
        var linhas = texto.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0);
        return string.Join("\n\n", linhas) + $"\n\n—\nE-mail automático do {MarcaProduto.Nome} — por favor, não responda.\n";
    }

    private static string EstilizarMiolo(string corpo)
    {
        // Link sozinho num parágrafo = ação principal: vira botão.
        var html = BotaoHtml().Replace(corpo, m => $"""
            <table role="presentation" cellpadding="0" cellspacing="0" border="0" style="margin:8px 0 22px;"><tr>
              <td style="background:{CorMarca};border-radius:8px;">
                <a style="display:inline-block;padding:13px 26px;font-family:Arial,Helvetica,sans-serif;font-size:15px;font-weight:700;color:#FFFFFF;text-decoration:none;" href="{m.Groups[1].Value}">{m.Groups[2].Value}</a>
              </td>
            </tr></table>
            """);
        // Demais links (no meio do texto) e parágrafos ganham o estilo da marca; o que já tem estilo próprio fica como está.
        html = html.Replace("<a href=", $"<a style=\"color:{CorMarca};font-weight:700;\" href=");
        html = html.Replace("<p>", "<p style=\"margin:0 0 16px;\">");
        return html;
    }

    [GeneratedRegex("""<p>\s*<a href="([^"]+)">([^<]+)</a>\s*</p>""")]
    private static partial Regex BotaoHtml();

    [GeneratedRegex("""<a [^>]*href="([^"]+)"[^>]*>([^<]+)</a>""")]
    private static partial Regex LinkHtml();
}
