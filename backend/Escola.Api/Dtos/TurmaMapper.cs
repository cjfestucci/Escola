using Escola.Domain.Entities;

namespace Escola.Api.Dtos;

public static class TurmaMapper
{
    public static TurmaDto ToDto(this Turma turma)
    {
        var vinculo = turma.Educadores.FirstOrDefault();

        return new TurmaDto(
            turma.Id,
            turma.Nome,
            turma.Periodo.ToString(),
            turma.HorarioEntrada,
            turma.HorarioSaida,
            turma.Alunos.Count,
            vinculo?.UsuarioId,
            vinculo?.Usuario.Nome,
            turma.UnidadeId,
            turma.Unidade.Nome,
            turma.Ativa,
            turma.ValorMensalidade);
    }
}
