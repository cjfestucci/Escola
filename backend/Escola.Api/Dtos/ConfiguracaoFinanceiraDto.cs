namespace Escola.Api.Dtos;

public record ConfiguracaoFinanceiraDto(
    string? PixChave,
    string? PixNomeRecebedor,
    string? PixCidade,
    bool Configurado);

public record PixCobrancaDto(string CodigoCopiaECola);
