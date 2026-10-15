using Escola.Domain.Enums;
using Escola.Infrastructure.Assinaturas;
using Escola.Infrastructure.Data;
using Escola.Infrastructure.Pagamentos.Asaas;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Escola.Api.Servicos;

/// <summary>Recalcula periodicamente a situação de todas as assinaturas: é o que faz o teste grátis acabar e a suspensão por atraso
/// acontecer (mudanças só de data, sem evento nenhum do gateway), e o que pega pagamentos quando o webhook não chega. Uma assinatura por
/// vez, cada uma no escopo do próprio cliente.</summary>
public sealed class AssinaturasWorker(
    IServiceScopeFactory escopos,
    IOptions<OpcoesAssinatura> opcoes,
    IOptions<OpcoesAsaas> opcoesAsaas,
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
                        var assinaturas = escopo.ServiceProvider.GetRequiredService<IAssinaturaService>();
                        await assinaturas.AtualizarAsync(cliente, parada);
                        await assinaturas.ProcessarRetencaoAsync(cliente, parada);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        logger.LogError(ex, "Falha ao atualizar a assinatura do cliente {Cliente}.", cliente);
                    }
                }
                await ApagarRegistrosAcessoAntigosAsync(parada);
                await ManterChaveDoAsaasAtivaAsync(parada);
            } while (await relogio.WaitForNextTickAsync(parada));
        }
        catch (OperationCanceledException)
        {
            // Encerrando a aplicação.
        }
    }

    private DateTime _ultimaConsultaAsaas = DateTime.MinValue;

    /// <summary>O Asaas desabilita chave de API sem uso por 3 meses — e sem clube assinando o sistema não chama o Asaas. Uma consulta de
    /// leitura por dia mantém a chave viva (e um erro aqui avisa no log que a chave caiu antes de um clube precisar dela).</summary>
    private async Task ManterChaveDoAsaasAtivaAsync(CancellationToken ct)
    {
        if (!opcoesAsaas.Value.Configurado || DateTime.UtcNow - _ultimaConsultaAsaas < TimeSpan.FromDays(1)) return;
        try
        {
            await using var escopo = escopos.CreateAsyncScope();
            await escopo.ServiceProvider.GetRequiredService<AsaasApi>().TestarContaRaizAsync(ct);
            _ultimaConsultaAsaas = DateTime.UtcNow;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "A chave de API do Asaas não respondeu — sem ela, os clubes novos não recebem fatura.");
        }
    }

    /// <summary>Registros de acesso (Marco Civil) passados os 6 meses de guarda obrigatória.</summary>
    private async Task ApagarRegistrosAcessoAntigosAsync(CancellationToken ct)
    {
        try
        {
            await using var escopo = escopos.CreateAsyncScope();
            var limite = DateTime.UtcNow - RegistroAcessoService.Retencao;
            var apagados = await escopo.ServiceProvider.GetRequiredService<EscolaDbContext>().RegistrosAcesso.IgnoreQueryFilters()
                .Where(r => r.OcorridoEm < limite).ExecuteDeleteAsync(ct);
            if (apagados > 0) logger.LogInformation("{Apagados} registros de acesso com mais de 6 meses apagados.", apagados);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Falha ao apagar registros de acesso antigos.");
        }
    }

    private async Task<List<Guid>> ClientesComAssinaturaAsync(CancellationToken ct)
    {
        await using var escopo = escopos.CreateAsyncScope();
        return await escopo.ServiceProvider.GetRequiredService<EscolaDbContext>().Assinaturas.IgnoreQueryFilters()
            .Select(a => EF.Property<Guid>(a, EscolaDbContext.ColunaCliente))
            .ToListAsync(ct);
    }
}
