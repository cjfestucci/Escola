namespace Escola.Api.Dtos;

/// <param name="Valor">Valor nominal da cobrança (sem multa/juros).</param>
/// <param name="Multa">Multa por atraso <b>calculada agora</b> (zero se não está atrasada ou a escola não cobra multa).</param>
/// <param name="Juros">Juros por atraso calculados agora.</param>
/// <param name="ValorAtualizado">Valor + multa + juros — é o que o Pix cobra e o que será gravado como <c>ValorPago</c> ao marcar como paga.</param>
/// <param name="ValorPago">Quanto foi recebido de fato (nulo se não paga).</param>
/// <param name="Competencia">Primeiro dia do mês da mensalidade (só nas geradas em lote).</param>
public record CobrancaDto(
    Guid Id,
    Guid AlunoId,
    string AlunoNome,
    string TurmaNome,
    string Descricao,
    decimal Valor,
    DateOnly Vencimento,
    bool Paga,
    DateOnly? PagoEm,
    bool Cancelada,
    DateOnly? CanceladaEm,
    decimal Multa = 0,
    decimal Juros = 0,
    decimal ValorAtualizado = 0,
    int DiasAtraso = 0,
    decimal? ValorPago = null,
    DateOnly? Competencia = null);
