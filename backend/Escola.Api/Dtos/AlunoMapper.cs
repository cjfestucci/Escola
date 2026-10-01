using Escola.Domain.Entities;

namespace Escola.Api.Dtos;

public static class AlunoMapper
{
    public static AlunoDto ToDto(this Aluno aluno) =>
        new(aluno.Id, aluno.Nome, aluno.DataNascimento, aluno.FotoUrl, aluno.TurmaId, aluno.Turma.Nome, aluno.Ativo);

    public static AlunoDetalheDto ToDetalheDto(this Aluno aluno) => new(
        aluno.Id,
        aluno.Nome,
        aluno.DataNascimento,
        aluno.FotoUrl,
        aluno.TurmaId,
        aluno.Turma.Nome,
        aluno.Ativo,
        aluno.Responsaveis
            .Select(ar => new ResponsavelResumoDto(ar.Responsavel.Id, ar.Responsavel.Nome, ar.Responsavel.Email, ar.Responsavel.Telefone, ar.ResponsavelFinanceiro))
            .ToList());
}
