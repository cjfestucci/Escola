namespace Escola.Api.Dtos.Requests;

/// <summary>Configurações → Geral (Gestão): só a cor. O fuso horário é editado pelo Suporte na tela Plataforma
/// (<see cref="EditarFusoHorarioRequest"/>) — um campo "fusoHorario" enviado aqui é ignorado.</summary>
public record EditarConfiguracaoEscolaRequest(string? CorPrincipal);

public record EditarFusoHorarioRequest(string FusoHorario);
