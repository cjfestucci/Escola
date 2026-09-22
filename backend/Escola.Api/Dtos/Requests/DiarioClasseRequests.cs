namespace Escola.Api.Dtos.Requests;

public record CriarOuEditarRegistroDiarioClasseRequest(
    Guid UsuarioId,
    string Titulo,
    string? Descricao,
    List<string>? FotoUrls);
