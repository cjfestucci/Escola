namespace Escola.Api.Dtos;

/// <param name="ContaPendente">O login do portal ainda não foi ativado (convite enviado, senha não criada).</param>
public record ResponsavelResumoDto(Guid Id, string Nome, string Email, string? Telefone, bool ResponsavelFinanceiro, bool ContaPendente = false, string? Cpf = null);
