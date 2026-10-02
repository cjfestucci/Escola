namespace Escola.Domain.Entities;

/// <summary>Arquivo anexado à saúde do aluno (exame, cartão de vacina, laudo...). Pertence ao Aluno, não à
/// FichaSaude, porque a ficha só passa a existir quando alguém a salva pela primeira vez — um documento pode
/// ser anexado antes disso. O conteúdo fica fora de wwwroot e só sai por endpoint autorizado.</summary>
public class DocumentoSaude
{
    public Guid Id { get; set; }

    public Guid AlunoId { get; set; }
    public Aluno Aluno { get; set; } = null!;

    public string NomeArquivo { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long TamanhoBytes { get; set; }

    /// <summary>Nome do arquivo no disco (GUID + extensão) — nunca o nome enviado pelo usuário.</summary>
    public string ArquivoArmazenado { get; set; } = string.Empty;

    public Guid UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public DateTime EnviadoEm { get; set; }
}
