using Escola.Domain.Enums;

namespace Escola.Domain.Entities;

public class Usuario
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public PapelUsuario Papel { get; set; }
    public bool Ativo { get; set; } = true;

    /// <summary>Só valem os tokens emitidos a partir deste instante: o que permite derrubar as sessões já abertas (conta desativada, senha
    /// trocada, "encerrar sessões"). Nulo = nenhuma revogação feita. O JWT dura dias e não tem como ser "desemitido"; por isso cada
    /// requisição confere esta data (e <see cref="Ativo"/>) no banco.</summary>
    public DateTime? SessoesValidasDesde { get; set; }

    /// <summary>Invalida todos os tokens emitidos até agora (o próximo login emite um novo, que vale).</summary>
    public void EncerrarSessoes() => SessoesValidasDesde = DateTime.UtcNow;

    // Verificação em duas etapas (código do app autenticador), opcional por conta. O Suporte não usa estes campos: o segredo dele vem
    // da configuração do deploy. Segredos ficam CRIPTOGRAFADOS (CofreSegredos, chave Segredos:Chave) — quem lê o banco não gera código.

    /// <summary>Segredo TOTP ativo. Nulo = sem verificação em duas etapas.</summary>
    public string? TotpSegredoCriptografado { get; set; }
    /// <summary>Segredo gerado ao começar a ativação, esperando a confirmação com o 1º código.</summary>
    public string? TotpSegredoPendenteCriptografado { get; set; }
    public DateTime? TotpAtivadoEm { get; set; }
    /// <summary>Hashes (SHA-256) dos códigos de recuperação ainda não usados, separados por vírgula. Cada um vale uma vez.</summary>
    public string? CodigosRecuperacaoHash { get; set; }

    /// <summary>Verificação em duas etapas por <b>código enviado por e-mail</b> (alternativa ao app, mais simples pro cliente). Nulo = não usa.</summary>
    public DateTime? DoisFatoresEmailAtivadoEm { get; set; }
    /// <summary>Hash (SHA-256, com o id da conta) do último código enviado por e-mail — de uso único, some ao ser usado.</summary>
    public string? CodigoEmailHash { get; set; }
    public DateTime? CodigoEmailExpiraEm { get; set; }
    /// <summary>Quando o último código foi enviado (intervalo mínimo entre envios).</summary>
    public DateTime? CodigoEmailEnviadoEm { get; set; }

    public bool DoisFatoresPorEmail => DoisFatoresEmailAtivadoEm is not null;
    public bool DoisFatoresAtivo => TotpSegredoCriptografado is not null || DoisFatoresPorEmail;

    /// <summary>Preenchido quando Papel == Responsavel, ligando o login ao cadastro do responsável.</summary>
    public Guid? ResponsavelId { get; set; }
    public Responsavel? Responsavel { get; set; }

    /// <summary>Turmas do educador (Papel == Educador) — pode ter mais de uma.</summary>
    public ICollection<TurmaEducador> Turmas { get; set; } = new List<TurmaEducador>();
}
