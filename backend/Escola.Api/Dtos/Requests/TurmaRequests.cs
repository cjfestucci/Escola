using Escola.Domain.Enums;

namespace Escola.Api.Dtos.Requests;

// Os mesmos campos servem para criar e editar. ProfessorId é opcional: nulo desvincula
// qualquer educador que a turma já tivesse.
public record CriarOuEditarTurmaRequest(
    string Nome,
    Periodo Periodo,
    TimeOnly HorarioEntrada,
    TimeOnly HorarioSaida,
    Guid? ProfessorId);
