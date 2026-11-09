using System.Globalization;
using System.Net;
using System.Text;
using Escola.Domain.Entities;
using Escola.Domain.Enums;

namespace Escola.Api.Servicos;

/// <summary>Uma linha do e-mail: um atleta do responsável e a situação dele neste jogo.</summary>
public record SituacaoNoJogo(string Atleta, string Situacao, bool Convocado);

/// <summary>E-mail que avisa o responsável da convocação (botão "Divulgar convocação"). Cada responsável recebe <b>só os filhos dele</b>:
/// nunca o nome nem a situação de outros atletas.</summary>
public static class EmailConvocacao
{
    public static (string Assunto, string Corpo) Montar(string clube, string responsavel, Jogo jogo, IReadOnlyList<SituacaoNoJogo> atletas, string? urlPortal)
    {
        var data = jogo.Data.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("pt-BR"));
        var hora = jogo.Hora.ToString("HH:mm", CultureInfo.InvariantCulture);
        var confronto = $"{jogo.Turma.Nome} x {jogo.Adversario}";

        var corpo = new StringBuilder();
        corpo.Append($"<p>Olá, {Html(responsavel)}!</p>");
        corpo.Append($"<p>O <strong>{Html(clube)}</strong> divulgou a convocação do jogo <strong>{Html(confronto)}</strong>.</p>");
        corpo.Append("<ul style=\"padding-left:18px;\">");
        foreach (var a in atletas)
            corpo.Append($"<li><strong>{Html(a.Atleta)}</strong>: {(a.Convocado ? "✅ " : string.Empty)}{Html(a.Situacao)}</li>");
        corpo.Append("</ul>");

        corpo.Append($"<p>📅 {data} às {hora}");
        if (!string.IsNullOrWhiteSpace(jogo.Local)) corpo.Append($" · 📍 {Html(jogo.Local)}");
        corpo.Append($" · {Html(jogo.Mando.Rotulo())}");
        if (jogo.Campeonato is not null) corpo.Append($" · {Html(jogo.Campeonato.Nome)}");
        corpo.Append("</p>");

        if (!string.IsNullOrWhiteSpace(urlPortal))
            corpo.Append($"<p><a href=\"{Html(urlPortal)}\">Ver no Portal da Família</a></p>");
        corpo.Append("<p style=\"color:#666;font-size:12px;\">Você recebeu este e-mail porque é responsável por atleta do clube. Em caso de dúvida, fale com o clube.</p>");

        return ($"Convocação: {confronto} — {data}", corpo.ToString());
    }

    private static string Html(string texto) => WebUtility.HtmlEncode(texto);
}
