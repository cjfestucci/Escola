namespace Escola.Api.Dtos.Requests;

/// <param name="Codigo">Código do app autenticador (6 dígitos) — só as contas com segundo fator (hoje, o Suporte) precisam.</param>
public record LoginRequest(string Email, string Senha, string? Codigo = null);

public record EsqueciSenhaRequest(string Email);

public record RedefinirSenhaRequest(string Token, string NovaSenha);
