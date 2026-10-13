using System.Collections.Concurrent;
using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Escola.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Escola.Api.Servicos;

/// <summary>Contas que já tiveram o uso do dia registrado nesta instância (uma consulta ao banco por conta por dia, não por requisição).
/// Em memória: com várias instâncias, cada uma registra o seu primeiro uso do dia — no máximo algumas linhas a mais.</summary>
public sealed class UsoDiarioRegistrado
{
    private readonly ConcurrentDictionary<Guid, DateOnly> _ultimoDia = new();

    /// <summary>Verdadeiro só na primeira vez do dia (UTC) pra esta conta.</summary>
    public bool Marcar(Guid usuarioId)
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var anterior = _ultimoDia.GetValueOrDefault(usuarioId);
        _ultimoDia[usuarioId] = hoje;
        return anterior != hoje;
    }
}

/// <summary>Grava os registros de acesso do Marco Civil (art. 15): quem usou a aplicação, quando e de qual IP. Ver <see cref="RegistroAcesso"/>.</summary>
public sealed class RegistroAcessoService(EscolaDbContext db, IHttpContextAccessor http, UsoDiarioRegistrado usoDiario)
{
    /// <summary>Por quanto tempo os registros ficam guardados (Marco Civil, art. 15: no mínimo 6 meses).</summary>
    public static readonly TimeSpan Retencao = TimeSpan.FromDays(183);

    /// <summary>Adiciona ao contexto; quem chama salva junto com o resto da operação.</summary>
    public void Registrar(Usuario usuario, TipoRegistroAcesso tipo)
    {
        var requisicao = http.HttpContext?.Request;
        var navegador = requisicao?.Headers.UserAgent.ToString();
        db.RegistrosAcesso.Add(new RegistroAcesso
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            Email = usuario.Email,
            Tipo = tipo,
            Ip = http.HttpContext?.Connection.RemoteIpAddress?.ToString(),
            Navegador = string.IsNullOrEmpty(navegador) ? null : navegador[..Math.Min(navegador.Length, 300)],
            OcorridoEm = DateTime.UtcNow
        });
        if (tipo == TipoRegistroAcesso.Login) usoDiario.Marcar(usuario.Id);
    }

    /// <summary>Primeiro uso do dia de um login já aberto. Barato nas demais requisições (só olha a memória).</summary>
    public async Task RegistrarUsoDiarioAsync(Guid usuarioId, CancellationToken ct = default)
    {
        if (!usoDiario.Marcar(usuarioId)) return;
        var usuario = await db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == usuarioId, ct);
        if (usuario is null) return;
        Registrar(usuario, TipoRegistroAcesso.UsoDiario);
        await db.SaveChangesAsync(ct);
    }
}
