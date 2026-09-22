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
        c.PagoEm);

    public static ConfiguracaoFinanceiraDto ToDto(this ConfiguracaoFinanceira c) => new(
        c.PixChave,
        c.PixNomeRecebedor,
        c.PixCidade,
        !string.IsNullOrWhiteSpace(c.PixChave) && !string.IsNullOrWhiteSpace(c.PixNomeRecebedor) && !string.IsNullOrWhiteSpace(c.PixCidade));
}
