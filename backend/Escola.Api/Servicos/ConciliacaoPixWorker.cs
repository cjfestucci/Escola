using Escola.Infrastructure.Clientes;
using Escola.Infrastructure.Pagamentos;
using Microsoft.Extensions.Options;

namespace Escola.Api.Servicos;

/// <summary>Pergunta ao banco, de tempos em tempos, se as cobranças Pix abertas foram pagas e dá baixa. É o que faz a baixa
/// automática funcionar <b>sem precisar de um endereço público</b> pro webhook (e cobre o caso do webhook falhar). Só é
/// registrado quando a integração com o banco está configurada.</summary>
public sealed class ConciliacaoPixWorker(IServiceScopeFactory escopos, IOptions<OpcoesPixBb> opcoes, ILogger<ConciliacaoPixWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken parada)
    {
        var intervalo = TimeSpan.FromSeconds(Math.Max(10, opcoes.Value.IntervaloConciliacaoSegundos));
        logger.LogInformation("Conciliação de Pix ativa: consultando o banco a cada {Segundos}s.", (int)intervalo.TotalSeconds);

        using var relogio = new PeriodicTimer(intervalo);
        try
        {
            while (await relogio.WaitForNextTickAsync(parada))
            {
                try
                {
                    await using var escopo = escopos.CreateAsyncScope();
                    // As credenciais do BB pertencem a um cliente só (OpcoesPixBb.ClienteId): a conciliação roda no escopo dele.
                    escopo.ServiceProvider.GetRequiredService<ClienteAtual>().Definir(opcoes.Value.ClienteId!.Value);
                    var servico = escopo.ServiceProvider.GetRequiredService<IPixAutomaticoService>();
                    var baixas = await servico.ConciliarPendentesAsync(parada);
                    if (baixas > 0) logger.LogInformation("Conciliação de Pix: {Baixas} cobrança(s) paga(s) e baixada(s).", baixas);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Uma rodada com erro (banco fora do ar, etc.) não pode derrubar o serviço: tenta de novo no próximo ciclo.
                    logger.LogError(ex, "Falha na rodada de conciliação de Pix.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Encerrando a aplicação.
        }
    }
}
