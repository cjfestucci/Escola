using Escola.Domain.Entities;

namespace Escola.Api.Dtos;

public static class CobrancaMapper
{
    public static CobrancaDto ToDto(this Cobranca c) => new(
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
        c.CanceladaEm);

    public static ConfiguracaoFinanceiraDto ToDto(this ConfiguracaoFinanceira c) => new(
        c.Id == Guid.Empty ? null : c.Id,
        c.PixChave,
        c.PixTipoChave,
        c.PixNomeRecebedor,
        c.PixCidade,
        c.DiasParaBloqueio,
        !string.IsNullOrWhiteSpace(c.PixChave) && !string.IsNullOrWhiteSpace(c.PixNomeRecebedor) && !string.IsNullOrWhiteSpace(c.PixCidade));
}
