namespace Escola.Domain.Entities;

/// <summary>Pedido de redefinição de senha por e-mail ("esqueci minha senha"). O token em si <b>nunca é guardado</b> —
/// só o hash SHA-256: quem lê o banco não consegue redefinir a senha de ninguém. Uso único e com validade curta.</summary>
public class RedefinicaoSenha
{
    public Guid Id { get; set; }

    public Guid UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    /// <summary>SHA-256 (hex minúsculo) do token enviado por e-mail.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTime CriadoEm { get; set; }
    public DateTime ExpiraEm { get; set; }

    /// <summary>Preenchido quando o link foi usado (ou invalidado por um pedido mais novo / pela troca de senha).</summary>
    public DateTime? UsadoEm { get; set; }

    /// <summary>Convite de conta nova (e-mail confirmado + primeira senha), em vez de uma redefinição. Muda a validade (7 dias) e o texto do histórico.</summary>
    public bool Convite { get; set; }
}
