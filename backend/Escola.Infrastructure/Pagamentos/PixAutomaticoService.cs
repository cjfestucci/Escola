using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Escola.Infrastructure.Auditoria;
using Escola.Infrastructure.Data;
using Escola.Infrastructure.Tempo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Escola.Infrastructure.Pagamentos;

public interface IPixAutomaticoService
{
    /// <summary>Código Pix "copia e cola" <b>dinâmico</b> (criado no provedor, com baixa automática) pra cobrar <paramref name="valor"/>
    /// desta cobrança. Devolve <c>null</c> se não há Pix automático nesta escola, falta dado (chave Pix pro BB, CPF do responsável pro
    /// Asaas) ou o provedor falhou — quem chama usa então o Pix estático de sempre, pra o pai nunca ficar sem como pagar.</summary>
    Task<string?> ObterCopiaEColaAsync(Cobranca cobranca, decimal valor, string? chavePix, CancellationToken ct = default);

    /// <summary>Há Pix automático nesta escola (não exige chave Pix estática quando é o Asaas)?</summary>
    Task<bool> DisponivelAsync(CancellationToken ct = default);

    /// <summary>Confere no banco (nunca confia no aviso recebido) se a cobrança Pix de <paramref name="txid"/> foi paga e, se foi, dá baixa.</summary>
    Task<bool> ConfirmarAsync(string txid, CancellationToken ct = default);

    /// <summary>Pergunta ao banco pelas cobranças Pix ainda abertas. Roda a cada poucos minutos e cobre o caso do webhook falhar ou nem existir.</summary>
    Task<int> ConciliarPendentesAsync(CancellationToken ct = default);
}

public sealed class PixAutomaticoService(
    EscolaDbContext db,
    IProvedorPixResolver provedores,
    IRelogioEscola relogio,
    IAuditoriaService auditoria,
    ILogger<PixAutomaticoService> logger) : IPixAutomaticoService
{
    /// <summary>Quantas cobranças Pix são conferidas por rodada (as mais antigas sem verificação primeiro).</summary>
    private const int LoteConciliacao = 50;

    /// <summary>Depois de expirada, a cobrança ainda é consultada por esse tempo — um pagamento no limite pode confirmar com atraso.</summary>
    private static readonly TimeSpan FolgaPosExpiracao = TimeSpan.FromHours(6);

    /// <summary>Uma cobrança só é reaproveitada se ainda tiver pelo menos isso de validade (senão o pai abriria um código prestes a expirar).</summary>
    private static readonly TimeSpan ValidadeMinimaParaReuso = TimeSpan.FromHours(1);

    public async Task<bool> DisponivelAsync(CancellationToken ct = default) => await provedores.ParaNovaCobrancaAsync(ct) is not null;

    public async Task<string?> ObterCopiaEColaAsync(Cobranca cobranca, decimal valor, string? chavePix, CancellationToken ct = default)
    {
        var provedor = await provedores.ParaNovaCobrancaAsync(ct);
        if (provedor is null) return null;
        // O BB cobra na chave Pix cadastrada da escola; o Asaas usa a chave da subconta dela.
        if (provedor.Tipo == ProvedorPagamento.BancoDoBrasil && string.IsNullOrWhiteSpace(chavePix)) return null;

        var agora = DateTime.UtcNow;
        var reaproveitavel = await db.CobrancasPix
            .Where(p => p.CobrancaId == cobranca.Id && p.Provedor == provedor.Tipo && p.Status == StatusCobrancaPix.Ativa && p.Valor == valor && p.ExpiraEm > agora + ValidadeMinimaParaReuso)
            .OrderByDescending(p => p.CriadoEm)
            .FirstOrDefaultAsync(ct);
        if (reaproveitavel is not null) return reaproveitavel.PixCopiaECola;

        var hoje = await relogio.HojeAsync();
        // Em atraso o valor muda todo dia (juros): validade curta. Em dia: vale até o fim do vencimento (de 1 a 60 dias).
        var expiracaoSegundos = cobranca.Vencimento < hoje
            ? 3 * 86400
            : Math.Clamp((cobranca.Vencimento.DayNumber - hoje.DayNumber + 1) * 86400, 86400, 60 * 86400);

        // O Asaas exige o pagador (CPF): o responsável financeiro do aluno (ou o primeiro responsável), se tiver CPF cadastrado.
        PagadorPix? pagador = null;
        Responsavel? responsavelPagador = null;
        if (provedor.ExigePagador)
        {
            responsavelPagador = await db.AlunoResponsaveis
                .Where(ar => ar.AlunoId == cobranca.AlunoId && ar.Responsavel.Cpf != null && ar.Responsavel.Cpf != "")
                .OrderByDescending(ar => ar.ResponsavelFinanceiro)
                .Select(ar => ar.Responsavel)
                .FirstOrDefaultAsync(ct);
            if (responsavelPagador is null)
            {
                logger.LogInformation("Cobrança {CobrancaId}: responsável sem CPF — usando o Pix estático (sem baixa automática).", cobranca.Id);
                return null;
            }
            pagador = new PagadorPix(responsavelPagador.Id, responsavelPagador.Nome, responsavelPagador.Cpf!, responsavelPagador.Email, responsavelPagador.IdClienteAsaas);
        }

        try
        {
            var criada = await provedor.CriarCobrancaAsync(
                new DadosNovaCobrancaPix(cobranca.Id, chavePix ?? string.Empty, valor, cobranca.Vencimento < hoje ? hoje : cobranca.Vencimento,
                    expiracaoSegundos, cobranca.Descricao, pagador), ct);
            if (criada.IdClienteExternoCriado is { } novoCliente && responsavelPagador is not null)
                responsavelPagador.IdClienteAsaas = novoCliente;

            // As cobranças anteriores ainda abertas (valor diferente) continuam ativas e consultadas até expirarem: se alguém
            // pagar um código antigo, o pagamento ainda é reconhecido e dá baixa.
            db.CobrancasPix.Add(new CobrancaPix
            {
                Id = Guid.NewGuid(),
                CobrancaId = cobranca.Id,
                TxId = criada.IdExterno,
                Valor = valor,
                PixCopiaECola = criada.PixCopiaECola,
                Provedor = provedor.Tipo,
                Status = StatusCobrancaPix.Ativa,
                CriadoEm = agora,
                ExpiraEm = agora.AddSeconds(expiracaoSegundos)
            });
            await db.SaveChangesAsync(ct);
            return criada.PixCopiaECola;
        }
        catch (PixProvedorException ex)
        {
            logger.LogWarning(ex, "Não foi possível criar a cobrança Pix no provedor {Provedor}; usando o Pix estático.", provedor.Tipo);
            return null;
        }
    }

    public async Task<bool> ConfirmarAsync(string txid, CancellationToken ct = default)
    {
        var pix = await db.CobrancasPix.Include(p => p.Cobranca).FirstOrDefaultAsync(p => p.TxId == txid, ct);
        if (pix is null) return false;
        return await ConferirAsync(pix, ct);
    }

    public async Task<int> ConciliarPendentesAsync(CancellationToken ct = default)
    {
        var limite = DateTime.UtcNow - FolgaPosExpiracao;
        var pendentes = await db.CobrancasPix
            .Include(p => p.Cobranca)
            .Where(p => p.Status == StatusCobrancaPix.Ativa)
            .OrderBy(p => p.UltimaVerificacaoEm == null ? DateTime.MinValue : p.UltimaVerificacaoEm)
            .Take(LoteConciliacao)
            .ToListAsync(ct);

        var baixas = 0;
        foreach (var pix in pendentes)
        {
            try
            {
                if (await ConferirAsync(pix, ct)) baixas++;
                else if (pix.ExpiraEm < limite)
                {
                    pix.Status = StatusCobrancaPix.Encerrada;
                    await db.SaveChangesAsync(ct);
                }
            }
            catch (PixProvedorException ex)
            {
                // Um título com problema não pode travar os demais; tenta de novo na próxima rodada.
                logger.LogWarning(ex, "Falha ao conferir a cobrança Pix {TxId}.", pix.TxId);
            }
        }

        return baixas;
    }

    /// <summary>Consulta o banco e aplica o resultado. Devolve true se acabou de dar baixa.</summary>
    private async Task<bool> ConferirAsync(CobrancaPix pix, CancellationToken ct)
    {
        if (pix.Status == StatusCobrancaPix.Concluida) return false;

        // Confere no provedor que criou a cobrança (uma escola pode ter trocado do BB pro Asaas com cobranças antigas abertas).
        var provedor = await provedores.ParaAsync(pix.Provedor, ct);
        if (provedor is null) return false;

        var consulta = await provedor.ConsultarCobrancaAsync(pix.TxId, ct);
        pix.UltimaVerificacaoEm = DateTime.UtcNow;

        if (consulta is null || consulta.Removida)
        {
            pix.Status = StatusCobrancaPix.Encerrada;
            await db.SaveChangesAsync(ct);
            return false;
        }

        var recebido = consulta.Concluida ? consulta.Pix.FirstOrDefault() : null;
        if (recebido is null)
        {
            await db.SaveChangesAsync(ct);
            return false;
        }

        pix.Status = StatusCobrancaPix.Concluida;
        pix.ConcluidoEm = recebido.HorarioUtc;
        pix.EndToEndId = recebido.EndToEndId;
        pix.ValorRecebido = recebido.Valor;

        var cobranca = pix.Cobranca;
        var valorTexto = recebido.Valor.ToString("N2", new System.Globalization.CultureInfo("pt-BR"));
        var baixou = false;

        if (cobranca.Cancelada)
        {
            // Dinheiro entrou numa cobrança que a escola cancelou: não reabrimos sozinhos, mas deixamos o alerta no histórico.
            auditoria.RegistrarSistema(nameof(Cobranca), cobranca.Id, AcaoAuditoria.Editado,
                $"ATENÇÃO: Pix de R$ {valorTexto} recebido, mas a cobrança está cancelada — confira e, se for o caso, devolva o valor.");
        }
        else if (cobranca.Paga)
        {
            auditoria.RegistrarSistema(nameof(Cobranca), cobranca.Id, AcaoAuditoria.Editado,
                $"ATENÇÃO: Pix de R$ {valorTexto} recebido, mas a cobrança já estava paga — possível pagamento em duplicidade; confira e, se for o caso, devolva o valor.");
        }
        else
        {
            cobranca.Paga = true;
            cobranca.PagoEm = await relogio.DataLocalAsync(recebido.HorarioUtc);
            cobranca.ValorPago = recebido.Valor;
            auditoria.RegistrarSistema(nameof(Cobranca), cobranca.Id, AcaoAuditoria.Editado,
                $"Paga via Pix (baixa automática{(pix.Provedor == ProvedorPagamento.Asaas ? ", Asaas" : string.Empty)}): R$ {valorTexto} — {cobranca.Descricao}");
            baixou = true;
        }

        await db.SaveChangesAsync(ct);
        return baixou;
    }
}
