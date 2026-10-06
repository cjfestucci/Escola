using Escola.Domain.Enums;

namespace Escola.Api.Dtos.Requests;

public record PresencaItemInput(Guid AlunoId, StatusPresenca Status);

/// <summary>Substitui a chamada inteira da turma naquele dia.</summary>
public record SalvarChamadaRequest(DateOnly Data, List<PresencaItemInput> Itens);
