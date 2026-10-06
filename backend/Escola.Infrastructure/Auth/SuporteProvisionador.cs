using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Escola.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Escola.Infrastructure.Auth;

/// <summary>Garante, na subida do sistema, a conta de <b>Suporte</b> (equipe do produto) do cliente deste deploy — a partir da
/// configuração do ambiente (<c>Suporte:Email</c>, <c>Suporte:Nome</c>, <c>Suporte:SenhaHash</c>), nunca de uma tela. O banco é a
/// fonte da conta (o log de auditoria precisa de um autor real), mas a <b>configuração é a fonte da verdade</b>: a cada subida a conta é
/// recriada/atualizada de lá, então trocar a senha é trocar o hash na configuração, e <b>remover a configuração desativa a conta</b>
/// (é assim que se revoga o acesso do suporte a um cliente).</summary>
public static class SuporteProvisionador
{
    public static async Task GarantirAsync(EscolaDbContext db, string? email, string? nome, string? senhaHash, string? totpSegredo, ILogger logger)
    {
        var existente = await db.Usuarios.FirstOrDefaultAsync(u => u.Papel == PapelUsuario.Suporte);
        var configurado = !string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(senhaHash);

        if (!configurado)
        {
            if (existente is { Ativo: true })
            {
                existente.Ativo = false;
                existente.EncerrarSessoes();
                await db.SaveChangesAsync();
                logger.LogWarning("Conta de Suporte desativada: a configuração Suporte:* foi removida deste ambiente.");
            }

            return;
        }

        if (!HashValido(senhaHash!))
        {
            // Config quebrada não pode virar "login com qualquer coisa" nem derrubar o sistema: a conta fica sem acesso.
            logger.LogError("Suporte:SenhaHash inválido (gere com: dotnet run --project Escola.Api -- --gerar-hash-senha). Conta de Suporte NÃO provisionada.");
            if (existente is { Ativo: true })
            {
                existente.Ativo = false;
                existente.EncerrarSessoes();
                await db.SaveChangesAsync();
            }

            return;
        }

        // Segundo fator é obrigatório: sem segredo TOTP válido a conta não existe (melhor sem suporte do que suporte só com senha).
        if (!Totp.SegredoValido(totpSegredo))
        {
            logger.LogError("Suporte:TotpSegredo ausente ou inválido (gere com: dotnet run --project Escola.Api -- --gerar-segredo-totp). Conta de Suporte NÃO provisionada.");
            if (existente is { Ativo: true })
            {
                existente.Ativo = false;
                existente.EncerrarSessoes();
                await db.SaveChangesAsync();
            }

            return;
        }

        var emailNormalizado = email!.Trim();
        var nomeFinal = string.IsNullOrWhiteSpace(nome) ? "Suporte" : nome.Trim();

        // O e-mail é único por cliente: se já é de outra conta do cliente, não dá pra criar (e não mexemos na conta dele).
        var emUso = await db.Usuarios.AnyAsync(u => u.Email.ToLower() == emailNormalizado.ToLower() && u.Papel != PapelUsuario.Suporte);
        if (emUso)
        {
            logger.LogError("Suporte:Email ({Email}) já é de outra conta deste cliente — conta de Suporte NÃO provisionada.", emailNormalizado);
            return;
        }

        if (existente is null)
        {
            db.Usuarios.Add(new Usuario
            {
                Id = Guid.NewGuid(), Nome = nomeFinal, Email = emailNormalizado, SenhaHash = senhaHash!,
                Papel = PapelUsuario.Suporte, Ativo = true
            });
            await db.SaveChangesAsync();
            logger.LogInformation("Conta de Suporte criada para este ambiente.");
            return;
        }

        if (existente.Nome != nomeFinal || existente.Email != emailNormalizado || existente.SenhaHash != senhaHash || !existente.Ativo)
        {
            existente.Nome = nomeFinal;
            existente.Email = emailNormalizado;
            // Credencial trocada (ou conta reativada): sessões antigas do Suporte não sobrevivem.
            if (existente.SenhaHash != senhaHash || !existente.Ativo) existente.EncerrarSessoes();
            existente.SenhaHash = senhaHash!;
            existente.Ativo = true;
            await db.SaveChangesAsync();
            logger.LogInformation("Conta de Suporte atualizada a partir da configuração.");
        }
    }

    /// <summary>Formato produzido por <see cref="SenhaHasher.Hash"/>: "salt.hash", os dois em base64.</summary>
    private static bool HashValido(string hash)
    {
        var partes = hash.Split('.');
        if (partes.Length != 2) return false;
        try
        {
            return Convert.FromBase64String(partes[0]).Length > 0 && Convert.FromBase64String(partes[1]).Length > 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
