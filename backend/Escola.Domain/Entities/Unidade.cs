namespace Escola.Domain.Entities;

public class Unidade
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Endereco { get; set; }
    public string? Telefone { get; set; }
    /// <summary>WhatsApp da unidade pros pais (opcional; "Fale com o clube" do portal abre a conversa). Guardado como digitado, ex.: "(16) 99123-4567".</summary>
    public string? WhatsApp { get; set; }
    /// <summary>E-mail de contato da unidade pros pais (opcional; "Fale com o clube" abre o programa de e-mail).</summary>
    public string? Email { get; set; }
    public bool Ativa { get; set; } = true;

    public ICollection<Turma> Turmas { get; set; } = new List<Turma>();
}
