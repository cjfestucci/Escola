namespace Escola.Infrastructure.Email;

public class ConfiguracaoSmtp
{
    public string? Host { get; set; }
    public int Porta { get; set; } = 587;
    public string? Usuario { get; set; }
    public string? Senha { get; set; }
    public string RemetenteNome { get; set; } = MarcaProduto.Nome;
    public string? RemetenteEmail { get; set; }
}
