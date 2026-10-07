using Escola.Domain.Entities;

namespace Escola.Api.Dtos;

public static class AlunoMapper
{
    /// <param name="bloqueado">Derivado das mensalidades em atraso (<c>IBloqueioAlunoService</c>) — não é um campo do aluno.</param>
    /// <param name="atestadoValidoAte">Da Ficha de Saúde (que pode nem existir) — buscado à parte pela listagem.</param>
    public static AlunoDto ToDto(this Aluno aluno, bool bloqueado = false, DateOnly? atestadoValidoAte = null) =>
        new(aluno.Id, aluno.Nome, aluno.DataNascimento, aluno.FotoUrl, aluno.TurmaId, aluno.Turma.Nome, aluno.Ativo, aluno.Posicao, bloqueado,
            atestadoValidoAte, aluno.MatriculaConfirmadaEm is null);

    /// <param name="contasPendentes">Ids dos responsáveis cujo login ainda não foi ativado (convite em aberto).</param>
    public static AlunoDetalheDto ToDetalheDto(this Aluno aluno, bool bloqueado = false, IReadOnlySet<Guid>? contasPendentes = null) => new(
        aluno.Id,
        aluno.Nome,
        aluno.DataNascimento,
        aluno.FotoUrl,
        aluno.TurmaId,
        aluno.Turma.Nome,
        aluno.Ativo,
        aluno.Responsaveis
            .Select(ar => new ResponsavelResumoDto(ar.Responsavel.Id, ar.Responsavel.Nome, ar.Responsavel.Email, ar.Responsavel.Telefone,
                ar.ResponsavelFinanceiro, contasPendentes?.Contains(ar.ResponsavelId) ?? false, ar.Responsavel.Cpf))
            .ToList(),
        [],
        aluno.Posicao,
        bloqueado,
        aluno.DescontoMensalidadePercentual,
        aluno.MotivoDesconto,
        aluno.MatriculaConfirmadaEm is null,
        aluno.MatriculaConfirmadaEm);
}
