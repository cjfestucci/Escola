using System.Security.Claims;
using Escola.Domain.Enums;
using Escola.Infrastructure.Assinaturas;
using Escola.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Escola.Api.Auth;

/// <summary>Resultado da conferência de uma requisição autenticada.</summary>
/// <param name="Motivo">Por que o token não vale mais; nulo se vale.</param>
/// <param name="SoAssinatura">O token vale, mas a assinatura do clube está bloqueada e a conta é o Admin: ele só pode usar a tela da
/// assinatura (pra pagar). Ver <see cref="Autenticacao.LiberadoComAssinaturaBloqueada"/>.</param>
public sealed record ConferenciaSessao(string? Motivo, bool SoAssinatura = false);

/// <summary>Conferências feitas a cada requisição autenticada, além da assinatura do JWT.</summary>
public static class Autenticacao
{
    /// <summary>Instante de emissão do token, em ticks UTC.</summary>
    public const string ClaimEmitidoEm = "emitidoEm";

    /// <summary>Chave em <c>HttpContext.Items</c> marcando a requisição de um Admin com a assinatura bloqueada.</summary>
    public const string ItemSoAssinatura = "soAssinatura";

    public const string MensagemAssinaturaBloqueada =
        "O acesso deste clube está suspenso: a assinatura está pendente. Regularize em Configurações → Assinatura.";

    public static async Task<ConferenciaSessao> ConferirAsync(EscolaDbContext db, ClaimsPrincipal principal, Guid clienteId)
    {
        if (!Guid.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var usuarioId))
            return new("Token sem usuário.");

        var conta = await db.Usuarios.Where(u => u.Id == usuarioId)
            .Select(u => new { u.Ativo, u.Papel, u.SessoesValidasDesde })
            .FirstOrDefaultAsync();

        if (conta is null) return new("Conta não existe mais.");
        if (!conta.Ativo) return new("Conta desativada.");

        if (conta.SessoesValidasDesde is { } desde)
        {
            // Token de antes da revogação não vale. Token antigo sem o instante de emissão também não (não dá pra provar que é posterior).
            var emitido = long.TryParse(principal.FindFirst(ClaimEmitidoEm)?.Value, out var ticks) ? ticks : 0;
            if (emitido < desde.Ticks) return new("Sessão encerrada.");
        }

        // Cliente suspenso corta as sessões abertas de todo mundo, menos do Suporte (que precisa entrar pra reativar).
        if (conta.Papel == PapelUsuario.Suporte) return new(null);
        if (!await db.Clientes.AnyAsync(c => c.Id == clienteId && c.Ativo))
            return new("Acesso do cliente suspenso.");

        // Assinatura pelo site pendente/suspensa: a equipe sai; o Admin fica, mas só pra pagar.
        if (await AssinaturaBloqueadaAsync(db))
            return conta.Papel == PapelUsuario.Admin ? new(null, SoAssinatura: true) : new("Assinatura do clube suspensa.");

        return new(null);
    }

    /// <summary>Cliente sem assinatura (provisionado à mão) nunca é bloqueado por aqui. Consulta no cliente do contexto.</summary>
    public static async Task<bool> AssinaturaBloqueadaAsync(EscolaDbContext db) =>
        await db.Assinaturas.Select(a => (SituacaoAssinatura?)a.Situacao).FirstOrDefaultAsync() is { } situacao
        && SituacaoAssinaturaCalculo.Bloqueia(situacao);

    /// <summary>O que o Admin ainda pode chamar com a assinatura bloqueada: a própria assinatura, o login e a configuração pública.</summary>
    public static bool LiberadoComAssinaturaBloqueada(HttpRequest requisicao)
    {
        var caminho = requisicao.Path;
        return caminho.StartsWithSegments("/api/assinaturas")
            || caminho.StartsWithSegments("/api/auth")
            || (HttpMethods.IsGet(requisicao.Method) && caminho.Equals("/api/configuracao/escola", StringComparison.OrdinalIgnoreCase));
    }
}
