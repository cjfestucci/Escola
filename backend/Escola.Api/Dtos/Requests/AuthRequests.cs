namespace Escola.Api.Dtos.Requests;

/// <param name="Codigo">Código do app autenticador (6 dígitos) — só as contas com segundo fator (hoje, o Suporte) precisam.</param>
/// <param name="ClienteId">Escola escolhida, quando o e-mail tem conta em mais de uma (a primeira resposta lista as opções).</param>
public record LoginRequest(string Email, string Senha, string? Codigo = null, Guid? ClienteId = null);

public record EsqueciSenhaRequest(string Email);

public record RedefinirSenhaRequest(string Token, string NovaSenha);
