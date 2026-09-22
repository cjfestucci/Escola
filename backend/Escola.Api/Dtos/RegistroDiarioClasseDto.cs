namespace Escola.Api.Dtos;

public record RegistroDiarioClasseDto(
    Guid Id,
    Guid TurmaId,
    string Titulo,
    string? Descricao,
    IReadOnlyList<string> FotoUrls,
    DateTime RegistradoEm,
    string CriadoPorNome);
