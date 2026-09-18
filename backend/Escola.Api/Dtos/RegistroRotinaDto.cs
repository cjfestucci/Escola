namespace Escola.Api.Dtos;

/// <summary>
/// Modelo de leitura único para a linha do tempo, com os campos específicos de cada
/// categoria (alimentação, sono, higiene, humor) preenchidos só quando fazem sentido.
/// </summary>
public record RegistroRotinaDto(
    Guid Id,
    Guid AlunoId,
    string Categoria,
    DateTime RegistradoEm,
    string? Observacao,
    string? FotoUrl,
    string CriadoPorNome,
    string? Refeicao,
    string? StatusAlimentacao,
    TimeOnly? HoraInicio,
    TimeOnly? HoraFim,
    string? TipoHigiene,
    string? Humor);
