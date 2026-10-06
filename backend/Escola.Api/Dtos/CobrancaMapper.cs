using Escola.Domain.Entities;
using Escola.Infrastructure.Financeiro;

namespace Escola.Api.Dtos;

public static class CobrancaMapper
{
    /// <param name="politica">Multa/juros da escola (<c>ConfiguracaoFinanceira</c>).</param>
    /// <param name="hoje">"Hoje" no fuso da escola (<c>IRelogioEscola</c>) — define se há atraso.</param>
    public static CobrancaDto ToDto(this Cobranca c, EncargosCobranca.Politica politica, DateOnly hoje)
    {
        var encargos = EncargosCobranca.Calcular(c.Valor, c.Vencimento, hoje, c.Paga, c.Cancelada, politica);
        return new CobrancaDto(
            c.Id,
            c.AlunoId,
            c.Aluno?.Nome ?? string.Empty,
            c.Aluno?.Turma?.Nome ?? string.Empty,
            c.Descricao,
            c.Valor,
            c.Vencimento,
            c.Paga,
            c.PagoEm,
            c.Cancelada,
            c.CanceladaEm,
            encargos.Multa,
            encargos.Juros,
            c.Valor + encargos.Total,
            encargos.DiasAtraso,
            c.ValorPago,
            c.Competencia);
    }

    public static ConfiguracaoFinanceiraDto ToDto(this ConfiguracaoFinanceira c) => new(
        c.Id == Guid.Empty ? null : c.Id,
        c.PixChave,
        c.PixTipoChave,
        c.PixNomeRecebedor,
        c.PixCidade,
        c.DiasParaBloqueio,
        !string.IsNullOrWhiteSpace(c.PixChave) && !string.IsNullOrWhiteSpace(c.PixNomeRecebedor) && !string.IsNullOrWhiteSpace(c.PixCidade),
        c.DiaVencimentoMensalidade,
        c.MultaAtrasoPercentual,
        c.JurosMensaisPercentual);

    public static EncargosCobranca.Politica ToPolitica(this ConfiguracaoFinanceira? c) =>
        c is null ? EncargosCobranca.Politica.Nenhuma : new(c.MultaAtrasoPercentual, c.JurosMensaisPercentual);
}
