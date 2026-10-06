using System.Security.Claims;
using Escola.Domain.Enums;
using Escola.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Escola.Api.Auth;

/// <summary>Conferências feitas a cada requisição autenticada, além da assinatura do JWT.</summary>
public static class Autenticacao
{
    /// <summary>Instante de emissão do token, em ticks UTC.</summary>
    public const string ClaimEmitidoEm = "emitidoEm";

    /// <returns>O motivo pelo qual o token não vale mais, ou nulo se vale.</returns>
    public static async Task<string?> MotivoDeRecusaAsync(EscolaDbContext db, ClaimsPrincipal principal, Guid clienteId)
    {
        if (!Guid.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var usuarioId))
            return "Token sem usuário.";

        var conta = await db.Usuarios.Where(u => u.Id == usuarioId)
            .Select(u => new { u.Ativo, u.Papel, u.SessoesValidasDesde })
            .FirstOrDefaultAsync();

        if (conta is null) return "Conta não existe mais.";
        if (!conta.Ativo) return "Conta desativada.";

        if (conta.SessoesValidasDesde is { } desde)
        {
            // Token de antes da revogação não vale. Token antigo sem o instante de emissão também não (não dá pra provar que é posterior).
            var emitido = long.TryParse(principal.FindFirst(ClaimEmitidoEm)?.Value, out var ticks) ? ticks : 0;
            if (emitido < desde.Ticks) return "Sessão encerrada.";
        }

        // Cliente suspenso corta as sessões abertas de todo mundo, menos do Suporte (que precisa entrar pra reativar).
        if (conta.Papel != PapelUsuario.Suporte && !await db.Clientes.AnyAsync(c => c.Id == clienteId && c.Ativo))
            return "Acesso do cliente suspenso.";

        return null;
    }
}
