using Escola.Domain.Enums;
using Escola.Infrastructure.Assinaturas;
using Escola.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Escola.Api.Servicos;

/// <summary>Recalcula periodicamente a situação de todas as assinaturas: é o que faz o teste grátis acabar e a suspensão por atraso
/// acontecer (mudanças só de data, sem evento nenhum do gateway), e o que pega pagamentos quando o webhook não chega. Uma assinatura por
/// vez, cada uma no escopo do próprio cliente.</summary>
public sealed class AssinaturasWorker(
    IServiceScopeFactory escopos,
    IOptions<OpcoesAssinatura> opcoes,
    ILogger<AssinaturasWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken parada)
    {
        var intervalo = TimeSpan.FromMinutes(Math.Max(1, opcoes.Value.IntervaloAtualizacaoMinutos));
        using var relogio = new PeriodicTimer(intervalo);
        try
        {
            do
            {
                foreach (var cliente in await ClientesComAssinaturaAsync(parada))
                {
                    try
                    {
                        await using var escopo = escopos.CreateAsyncScope();
                        await escopo.ServiceProvider.GetRequiredService<IAssinaturaService>().AtualizarAsync(cliente, parada);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        logger.LogError(ex, "Falha ao atualizar a assinatura do cliente {Cliente}.", cliente);
                    }
                }
            } while (await relogio.WaitForNextTickAsync(parada));
        }
        catch (OperationCanceledException)
        {
            // Encerrando a aplicação.
        }
    }

    private async Task<List<Guid>> ClientesComAssinaturaAsync(CancellationToken ct)
    {
        await using var escopo = escopos.CreateAsyncScope();
        return await escopo.ServiceProvider.GetRequiredService<EscolaDbContext>().Assinaturas.IgnoreQueryFilters()
            .Where(a => a.Situacao != SituacaoAssinatura.Cancelada)
            .Select(a => EF.Property<Guid>(a, EscolaDbContext.ColunaCliente))
            .ToListAsync(ct);
    }
}
