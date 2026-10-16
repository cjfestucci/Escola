namespace Escola.Infrastructure.Email;

public class ConfiguracaoSmtp
{
    public string? Host { get; set; }
    public int Porta { get; set; } = 587;
    public string? Usuario { get; set; }
    public string? Senha { get; set; }
    public string RemetenteNome { get; set; } = MarcaProduto.Nome;
    public string? RemetenteEmail { get; set; }

    /// <summary>Modo seguro de teste: preenchido, <b>todo</b> e-mail vai pra esse endereço (o destinatário original vai no assunto,
    /// "[TESTE → fulano@x.com] …"). Usado no ambiente de desenvolvimento, cujo banco tem e-mails fictícios que podem ser de gente de
    /// verdade. Vazio (produção) = envia normalmente.</summary>
    public string? RedirecionarTudoPara { get; set; }
}
