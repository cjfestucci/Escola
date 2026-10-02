using Escola.Domain.Enums;

namespace Escola.Api.Dtos.Requests;

// Id nulo = novo responsável (cria); Id preenchido = atualiza o já vinculado ao aluno.
// Um vínculo existente que não aparecer mais na lista é desfeito (não apaga o Responsavel).
public record ResponsavelInput(Guid? Id, string Nome, string Email, string? Telefone, bool ResponsavelFinanceiro);

public record CriarOuEditarAlunoRequest(
    string Nome,
    DateOnly DataNascimento,
    Guid TurmaId,
    string? FotoUrl,
    List<ResponsavelInput> Responsaveis,
    PosicaoAtleta? Posicao = null);
