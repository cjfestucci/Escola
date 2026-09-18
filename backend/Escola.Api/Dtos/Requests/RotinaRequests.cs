using Escola.Domain.Enums;

namespace Escola.Api.Dtos.Requests;

// UsuarioId identifica o educador que está lançando o registro. Vira um claim do
// usuário autenticado assim que o login (Usuario) estiver implementado — por ora é
// passado explicitamente pelo cliente.

public record CriarRegistroAlimentacaoRequest(
    Guid UsuarioId,
    Refeicao Refeicao,
    StatusAlimentacao Status,
    string? Observacao,
    string? FotoUrl);

public record CriarRegistroSonoRequest(
    Guid UsuarioId,
    TimeOnly HoraInicio,
    TimeOnly? HoraFim,
    string? Observacao,
    string? FotoUrl);

public record CriarRegistroHigieneRequest(
    Guid UsuarioId,
    TipoHigiene Tipo,
    string? Observacao,
    string? FotoUrl);

public record CriarRegistroHumorRequest(
    Guid UsuarioId,
    Humor Humor,
    string? Observacao,
    string? FotoUrl);

public record CriarRegistroMomentoRequest(
    Guid UsuarioId,
    string? Observacao,
    string? FotoUrl);
