namespace Escola.Api.Dtos;

/// <summary>Senha temporária mostrada uma única vez, logo após criar um login (equipe ou responsável).</summary>
public record SenhaGeradaDto(string Nome, string Email, string Senha);
