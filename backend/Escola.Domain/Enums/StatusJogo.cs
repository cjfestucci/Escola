namespace Escola.Domain.Enums;

public enum StatusJogo
{
    Agendado,
    Realizado,
    Cancelado
}

public static class StatusJogoExtensions
{
    public static string Rotulo(this StatusJogo status) => status switch
    {
        StatusJogo.Agendado => "Agendado",
        StatusJogo.Realizado => "Realizado",
        StatusJogo.Cancelado => "Cancelado",
        _ => status.ToString()
    };
}
