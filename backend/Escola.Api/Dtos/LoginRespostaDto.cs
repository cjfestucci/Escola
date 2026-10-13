namespace Escola.Api.Dtos;

/// <summary>Escola em que a pessoa tem conta (só aparece pra quem acertou a senha de mais de uma).</summary>
public record OpcaoClienteLoginDto(Guid Id, string Nome);

/// <param name="RequerSegundoFator">Senha certa, falta o código (do app autenticador ou enviado por e-mail): sem token ainda.</param>
/// <param name="EscolherCliente">A senha confere em mais de uma escola: a tela pede pra escolher e reenvia com <c>clienteId</c>. Sem token ainda.</param>
/// <param name="MetodoSegundoFator">"App" ou "Email" — muda o texto da tela (e o "reenviar código" só existe no e-mail).</param>
/// <param name="EmailMascarado">Pra onde foi o código ("f***@escola.com"), sem mostrar o endereço inteiro a quem só sabe a senha.</param>
/// <param name="Aviso">Ex.: o e-mail não pôde ser enviado, ou já foi enviado há pouco.</param>
/// <param name="PodeLembrarAparelho">A tela mostra "lembrar este aparelho" (nunca pro Suporte).</param>
/// <param name="TokenAparelho">Só quando a pessoa marcou "lembrar este aparelho": o navegador guarda e manda nos próximos logins.</param>
public record LoginRespostaDto(
    string Token,
    Guid UsuarioId,
    string Nome,
    string Papel,
    Guid? ResponsavelId,
    bool RequerSegundoFator = false,
    IReadOnlyList<OpcaoClienteLoginDto>? EscolherCliente = null,
    Guid? ClienteId = null,
    string? MetodoSegundoFator = null,
    string? EmailMascarado = null,
    string? Aviso = null,
    string? TokenAparelho = null,
    bool PodeLembrarAparelho = false)
{
    public static LoginRespostaDto ExigeSegundoFator(string metodo = "App", string? emailMascarado = null, string? aviso = null, bool podeLembrarAparelho = false) =>
        new(string.Empty, Guid.Empty, string.Empty, string.Empty, null, RequerSegundoFator: true,
            MetodoSegundoFator: metodo, EmailMascarado: emailMascarado, Aviso: aviso, PodeLembrarAparelho: podeLembrarAparelho);

    public static LoginRespostaDto ExigeEscolhaDeCliente(IReadOnlyList<OpcaoClienteLoginDto> opcoes) =>
        new(string.Empty, Guid.Empty, string.Empty, string.Empty, null, EscolherCliente: opcoes);
}
