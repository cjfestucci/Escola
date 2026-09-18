using Escola.Domain.Enums;

namespace Escola.Api.Dtos.Requests;

// UsuarioId identifica o educador que está lançando (ou editando) o registro. Vira um claim
// do usuário autenticado assim que o login (Usuario) estiver implementado — por ora é
// passado explicitamente pelo cliente. Os mesmos registros servem para criar e para editar.
// FotoUrls: até 4 URLs devolvidas por POST /api/uploads, na ordem em que devem aparecer.

public record CriarRegistroAlimentacaoRequest(
    Guid UsuarioId,
    Refeicao Refeicao,
    StatusAlimentacao Status,
    string? Observacao,
    List<string>? FotoUrls);

public record CriarRegistroSonoRequest(
    Guid UsuarioId,
    TimeOnly HoraInicio,
    TimeOnly? HoraFim,
    string? Observacao,
    List<string>? FotoUrls);

public record CriarRegistroHigieneRequest(
    Guid UsuarioId,
    TipoHigiene Tipo,
    string? Observacao,
    List<string>? FotoUrls);

public record CriarRegistroHumorRequest(
    Guid UsuarioId,
    Humor Humor,
    string? Observacao,
    List<string>? FotoUrls);

public record CriarRegistroMomentoRequest(
    Guid UsuarioId,
    string? Observacao,
    List<string>? FotoUrls);
