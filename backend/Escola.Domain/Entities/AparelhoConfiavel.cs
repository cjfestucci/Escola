namespace Escola.Domain.Entities;

/// <summary>"Lembrar este aparelho por 30 dias": depois de passar pela verificação em duas etapas, o navegador guarda um token e os
/// próximos logins dali pedem só a senha. Só o hash do token fica no banco. Deixa de valer ao expirar, ao desativar a verificação e
/// a cada <see cref="Usuario.EncerrarSessoes"/> (troca de senha, "encerrar sessões", conta desativada) — vale só se foi criado depois de
/// <see cref="Usuario.SessoesValidasDesde"/>.</summary>
public class AparelhoConfiavel
{
    public Guid Id { get; set; }

    public Guid UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    /// <summary>SHA-256 (hex) do token guardado no navegador.</summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>Navegador/sistema (User-Agent resumido), pra pessoa reconhecer o aparelho.</summary>
    public string? Descricao { get; set; }

    public DateTime CriadoEm { get; set; }
    public DateTime ExpiraEm { get; set; }
    public DateTime? UltimoUsoEm { get; set; }
}
