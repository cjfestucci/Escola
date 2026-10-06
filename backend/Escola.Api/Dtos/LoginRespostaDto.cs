namespace Escola.Api.Dtos;

/// <param name="RequerSegundoFator">Senha correta, mas falta o código do app autenticador: nenhum token é emitido (os demais campos vêm vazios).</param>
public record LoginRespostaDto(string Token, Guid UsuarioId, string Nome, string Papel, Guid? ResponsavelId, bool RequerSegundoFator = false)
{
    public static LoginRespostaDto ExigeSegundoFator() => new(string.Empty, Guid.Empty, string.Empty, string.Empty, null, RequerSegundoFator: true);
}
