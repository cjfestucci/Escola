using Escola.Domain.Enums;
using Escola.Infrastructure.Clientes;
using Escola.Infrastructure.Data;
using Escola.Infrastructure.Pagamentos;
using Escola.Infrastructure.Pagamentos.Asaas;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Escola.Api.Servicos;

/// <summary>Pergunta periodicamente aos provedores (Asaas, BB) pelas cobranças Pix ainda abertas e dá baixa nas pagas — o caminho
/// principal da baixa automática (funciona sem endereço público; o webhook só acelera). Roda <b>escola por escola</b>, cada uma no
/// próprio escopo: as que têm subconta Asaas neste ambiente e a dona das credenciais do BB.</summary>
public sealed class ConciliacaoPixWorker(
    IServiceScopeFactory escopos,
    IOptions<OpcoesPixBb> opcoesBb,
    IOptions<OpcoesAsaas> opcoesAsaas,
    ILogger<ConciliacaoPixWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken parada)
    {
        var intervalo = TimeSpan.FromSeconds(Math.Max(10, opcoesBb.Value.IntervaloConciliacaoSegundos));
        logger.LogInformation("Conciliação de Pix ativa: consultando os provedores a cada {Segundos}s.", (int)intervalo.TotalSeconds);

        using var relogio = new PeriodicTimer(intervalo);
        try
        {
            while (await relogio.WaitForNextTickAsync(parada))
            {
                foreach (var cliente in await ClientesComPixAutomaticoAsync(parada))
                {
                    try
                    {
                        await using var escopo = escopos.CreateAsyncScope();
                        escopo.ServiceProvider.GetRequiredService<ClienteAtual>().Definir(cliente);
                        var baixas = await escopo.ServiceProvider.GetRequiredService<IPixAutomaticoService>().ConciliarPendentesAsync(parada);
                        if (baixas > 0) logger.LogInformation("Conciliação de Pix (cliente {Cliente}): {Baixas} cobrança(s) paga(s) e baixada(s).", cliente, baixas);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        // Uma escola com problema (provedor fora do ar, chave inválida) não pode travar as outras.
                        logger.LogError(ex, "Falha na conciliação de Pix do cliente {Cliente}.", cliente);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Encerrando a aplicação.
        }
    }

    private async Task<List<Guid>> ClientesComPixAutomaticoAsync(CancellationToken ct)
    {
        await using var escopo = escopos.CreateAsyncScope();
        var db = escopo.ServiceProvider.GetRequiredService<EscolaDbContext>();

        var clientes = new HashSet<Guid>();
        if (opcoesAsaas.Value.Configurado)
        {
            var ambiente = opcoesAsaas.Value.NomeAmbiente;
            clientes.UnionWith(await db.ContasPagamento.IgnoreQueryFilters()
                .Where(c => c.Provedor == ProvedorPagamento.Asaas && c.Ambiente == ambiente)
                .Select(c => EF.Property<Guid>(c, EscolaDbContext.ColunaCliente))
                .ToListAsync(ct));
        }
        if (opcoesBb.Value.Configurado && opcoesBb.Value.ClienteId is { } doBb) clientes.Add(doBb);
        return [.. clientes];
    }
}
