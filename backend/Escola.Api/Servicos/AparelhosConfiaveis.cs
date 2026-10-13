using System.Security.Cryptography;
using System.Text;
using Escola.Domain.Entities;
using Escola.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Escola.Api.Servicos;

/// <summary>"Lembrar este aparelho por 30 dias" (ver <see cref="AparelhoConfiavel"/>). O token só serve pra pular o código — a senha
/// continua sendo pedida sempre.</summary>
public static class AparelhosConfiaveis
{
    public static readonly TimeSpan Validade = TimeSpan.FromDays(30);

    /// <summary>Uma conta não acumula aparelhos sem fim: passando disso, o mais antigo é esquecido.</summary>
    public const int MaximoPorConta = 10;

    /// <summary>Algum dos tokens enviados é um aparelho lembrado desta conta, ainda válido? (Marca o uso.) Não salva.</summary>
    public static async Task<bool> ReconheceAsync(EscolaDbContext db, Usuario usuario, IReadOnlyList<string>? tokens)
    {
        if (tokens is not { Count: > 0 }) return false;
        var hashes = tokens.Where(t => !string.IsNullOrWhiteSpace(t) && t.Length <= 100).Take(20).Select(Hash).ToList();
        if (hashes.Count == 0) return false;

        var agora = DateTime.UtcNow;
        var desde = usuario.SessoesValidasDesde ?? DateTime.MinValue;
        var aparelho = await db.AparelhosConfiaveis
            .Where(a => a.UsuarioId == usuario.Id && hashes.Contains(a.TokenHash) && a.ExpiraEm > agora && a.CriadoEm > desde)
            .FirstOrDefaultAsync();
        if (aparelho is null) return false;

        aparelho.UltimoUsoEm = agora;
        return true;
    }

    /// <summary>Cria o aparelho e devolve o token (só esta vez — no banco fica o hash). Não salva.</summary>
    public static async Task<string> CriarAsync(EscolaDbContext db, Usuario usuario, string? userAgent)
    {
        var agora = DateTime.UtcNow;
        var existentes = await db.AparelhosConfiaveis.Where(a => a.UsuarioId == usuario.Id).OrderByDescending(a => a.CriadoEm).ToListAsync();
        // Some com os vencidos e, se ainda passar do limite, com os mais antigos.
        var descartar = existentes.Where(a => a.ExpiraEm <= agora).Concat(existentes.Where(a => a.ExpiraEm > agora).Skip(MaximoPorConta - 1));
        db.AparelhosConfiaveis.RemoveRange(descartar.Distinct());

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        db.AparelhosConfiaveis.Add(new AparelhoConfiavel
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            TokenHash = Hash(token),
            Descricao = DescreverNavegador(userAgent),
            CriadoEm = agora,
            ExpiraEm = agora + Validade
        });
        return token;
    }

    /// <summary>Quantos aparelhos lembrados ainda valem (pra tela Minha conta).</summary>
    public static Task<int> ContarAsync(EscolaDbContext db, Usuario usuario)
    {
        var agora = DateTime.UtcNow;
        var desde = usuario.SessoesValidasDesde ?? DateTime.MinValue;
        return db.AparelhosConfiaveis.CountAsync(a => a.UsuarioId == usuario.Id && a.ExpiraEm > agora && a.CriadoEm > desde);
    }

    /// <summary>Esquece todos os aparelhos da conta (desativar a verificação, "esquecer aparelhos", Gestão removendo o 2FA). Não salva.</summary>
    public static async Task EsquecerTodosAsync(EscolaDbContext db, Guid usuarioId) =>
        db.AparelhosConfiaveis.RemoveRange(await db.AparelhosConfiaveis.Where(a => a.UsuarioId == usuarioId).ToListAsync());

    /// <summary>"Chrome no Windows" — o suficiente pra pessoa reconhecer, sem guardar o User-Agent inteiro.</summary>
    public static string DescreverNavegador(string? userAgent)
    {
        var ua = userAgent ?? string.Empty;
        var navegador = ua.Contains("Edg/") ? "Edge" : ua.Contains("OPR/") ? "Opera" : ua.Contains("Firefox/") ? "Firefox"
            : ua.Contains("Chrome/") ? "Chrome" : ua.Contains("Safari/") ? "Safari" : "Navegador";
        var sistema = ua.Contains("Android") ? "Android" : ua.Contains("iPhone") || ua.Contains("iPad") ? "iOS" : ua.Contains("Windows") ? "Windows"
            : ua.Contains("Mac OS") ? "Mac" : ua.Contains("Linux") ? "Linux" : null;
        return sistema is null ? navegador : $"{navegador} no {sistema}";
    }

    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
