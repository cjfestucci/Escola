using Escola.Domain.Enums;

namespace Escola.Api.Dtos.Requests;

// Id nulo = novo responsável (cria); Id preenchido = atualiza o já vinculado ao aluno.
// Um vínculo existente que não aparecer mais na lista é desfeito (não apaga o Responsavel).
/// <param name="Cpf">Opcional; necessário pro pagamento automático (o gateway exige CPF do pagador). Aceita com ou sem pontuação.</param>
public record ResponsavelInput(Guid? Id, string Nome, string Email, string? Telefone, bool ResponsavelFinanceiro, string? Cpf = null);

public record CriarOuEditarAlunoRequest(
    string Nome,
    DateOnly DataNascimento,
    Guid TurmaId,
    string? FotoUrl,
    List<ResponsavelInput> Responsaveis,
    PosicaoAtleta? Posicao = null,
    decimal DescontoMensalidadePercentual = 0,
    string? MotivoDesconto = null);
