using Escola.Domain.Enums;

namespace Escola.Api.Dtos.Requests;

// Os mesmos campos servem para criar e editar. ProfessorId é opcional: nulo desvincula
// qualquer educador que a turma já tivesse. UnidadeId é obrigatório — toda turma pertence a uma unidade.
public record CriarOuEditarTurmaRequest(
    string Nome,
    Periodo Periodo,
    TimeOnly HorarioEntrada,
    TimeOnly HorarioSaida,
    Guid? ProfessorId,
    Guid UnidadeId);
