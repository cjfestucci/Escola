namespace Escola.Api.Dtos;

public record ContaReceberDto(
    Guid Id,
    string Descricao,
    string? Origem,
    decimal Valor,
    DateOnly Vencimento,
    bool Recebida,
    DateOnly? RecebidoEm,
    bool Cancelada,
    DateOnly? CanceladaEm);
