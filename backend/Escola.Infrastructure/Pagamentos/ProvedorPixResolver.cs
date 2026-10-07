using Escola.Domain.Enums;
using Escola.Infrastructure.Data;
using Escola.Infrastructure.Pagamentos.Asaas;
using Escola.Infrastructure.Seguranca;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Escola.Infrastructure.Pagamentos;

/// <summary>Escolhe o provedor de Pix automático da escola atual: a <b>subconta Asaas</b> dela, se existir (no ambiente configurado e com
/// chave Pix criada); senão o Banco do Brasil, se as credenciais do deploy forem desta escola; senão nenhum (Pix estático, baixa manual).</summary>
public interface IProvedorPixResolver
{
    /// <summary>Provedor pra criar uma cobrança nova agora. Nulo = sem Pix automático nesta escola.</summary>
    Task<IProvedorPix?> ParaNovaCobrancaAsync(CancellationToken ct = default);

    /// <summary>Provedor pra conferir uma cobrança já criada por <paramref name="tipo"/>. Nulo se não dá mais (ex.: conta desconectada).</summary>
    Task<IProvedorPix?> ParaAsync(ProvedorPagamento tipo, CancellationToken ct = default);
}

public sealed class ProvedorPixResolver(
    EscolaDbContext db,
    IProvedorPix bancoDoBrasil,
    AsaasApi asaas,
    IOptions<OpcoesAsaas> opcoesAsaas,
    CofreSegredos cofre,
    ILogger<ProvedorPixResolver> logger) : IProvedorPixResolver
{
    public async Task<IProvedorPix?> ParaNovaCobrancaAsync(CancellationToken ct = default)
    {
        var asaasDaEscola = await AsaasAsync(exigirChavePix: true, ct);
        if (asaasDaEscola is not null) return asaasDaEscola;
        return bancoDoBrasil.Configurado ? bancoDoBrasil : null;
    }

    public async Task<IProvedorPix?> ParaAsync(ProvedorPagamento tipo, CancellationToken ct = default) => tipo switch
    {
        ProvedorPagamento.Asaas => await AsaasAsync(exigirChavePix: false, ct),
        _ => bancoDoBrasil.Configurado ? bancoDoBrasil : null
    };

    private async Task<IProvedorPix?> AsaasAsync(bool exigirChavePix, CancellationToken ct)
    {
        if (!opcoesAsaas.Value.Configurado || !cofre.Disponivel) return null;

        var conta = await db.ContasPagamento.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Provedor == ProvedorPagamento.Asaas && c.Ambiente == opcoesAsaas.Value.NomeAmbiente, ct);
        if (conta is null || (exigirChavePix && !conta.ChavePixCriada)) return null;

        try
        {
            return new AsaasProvedorPix(asaas, cofre.Descriptografar(conta.ApiKeyCriptografada));
        }
        catch (Exception ex) when (ex is System.Security.Cryptography.CryptographicException or FormatException)
        {
            // Chave do cofre trocada/perdida: a conta precisa ser reconectada. Nunca derruba a cobrança (cai no Pix estático).
            logger.LogError(ex, "Não foi possível descriptografar a chave da conta de pagamento da escola (Segredos:Chave mudou?).");
            return null;
        }
    }
}
