using Escola.Domain.Enums;

namespace Escola.Api.Dtos.Requests;

public record CriarOuEditarCampeonatoRequest(string Nome, DateOnly DataInicio, DateOnly? DataFim, string? Observacao, int? AmarelosParaSuspensao = 3);

public record CriarOuEditarJogoRequest(
    Guid? CampeonatoId,
    Guid TurmaId,
    string Adversario,
    DateOnly Data,
    TimeOnly Hora,
    string? Local,
    LocalJogo Mando,
    StatusJogo Status,
    int? GolsPro,
    int? GolsContra,
    string? Observacao);

public record ConvocadoInput(Guid AlunoId, bool Titular, int Gols, int CartoesAmarelos, bool CartaoVermelho);

public record SalvarConvocacaoRequest(List<ConvocadoInput> Convocados);
