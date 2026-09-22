namespace Escola.Api.Dtos;

public record CobrancaDto(
    Guid Id,
    Guid AlunoId,
    string AlunoNome,
    string TurmaNome,
    string Descricao,
    decimal Valor,
    DateOnly Vencimento,
    bool Paga,
    DateOnly? PagoEm);
