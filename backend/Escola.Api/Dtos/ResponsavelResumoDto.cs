namespace Escola.Api.Dtos;

public record ResponsavelResumoDto(Guid Id, string Nome, string Email, string? Telefone, bool ResponsavelFinanceiro);
