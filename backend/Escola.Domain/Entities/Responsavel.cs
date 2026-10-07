namespace Escola.Domain.Entities;

public class Responsavel
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefone { get; set; }

    /// <summary>CPF (só dígitos). Opcional no cadastro, mas o gateway exige do pagador pra emitir cobrança com baixa automática.</summary>
    public string? Cpf { get; set; }

    /// <summary>Id deste responsável como cliente na subconta Asaas da escola (criado no primeiro pagamento automático).</summary>
    public string? IdClienteAsaas { get; set; }

    public ICollection<AlunoResponsavel> Alunos { get; set; } = new List<AlunoResponsavel>();
}
