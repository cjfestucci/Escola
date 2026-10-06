namespace Escola.Api.Dtos;

public record TurmaDto(
    Guid Id,
    string Nome,
    string Periodo,
    TimeOnly HorarioEntrada,
    TimeOnly HorarioSaida,
    int QuantidadeAlunos,
    Guid? ProfessorId,
    string? ProfessorNome,
    Guid UnidadeId,
    string UnidadeNome,
    bool Ativa,
    decimal? ValorMensalidade = null);
