namespace Escola.Api.Dtos;

/// <summary>Escola em que a pessoa tem conta (só aparece pra quem acertou a senha de mais de uma).</summary>
public record OpcaoClienteLoginDto(Guid Id, string Nome);

/// <param name="RequerSegundoFator">Senha certa, falta o código do app autenticador (Suporte): sem token ainda.</param>
/// <param name="EscolherCliente">A senha confere em mais de uma escola: a tela pede pra escolher e reenvia com <c>clienteId</c>. Sem token ainda.</param>
public record LoginRespostaDto(
    string Token,
    Guid UsuarioId,
    string Nome,
    string Papel,
    Guid? ResponsavelId,
    bool RequerSegundoFator = false,
    IReadOnlyList<OpcaoClienteLoginDto>? EscolherCliente = null,
    Guid? ClienteId = null)
{
    public static LoginRespostaDto ExigeSegundoFator() => new(string.Empty, Guid.Empty, string.Empty, string.Empty, null, RequerSegundoFator: true);

    public static LoginRespostaDto ExigeEscolhaDeCliente(IReadOnlyList<OpcaoClienteLoginDto> opcoes) =>
        new(string.Empty, Guid.Empty, string.Empty, string.Empty, null, EscolherCliente: opcoes);
}
