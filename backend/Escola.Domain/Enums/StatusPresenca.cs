namespace Escola.Domain.Enums;

public enum StatusPresenca
{
    Presente = 0,
    Falta = 1,
    /// <summary>Falta com justificativa (atestado, aviso prévio…): não pesa na frequência nem interrompe/soma "faltas seguidas".</summary>
    Justificada = 2
}

public static class StatusPresencaExtensions
{
    public static string Rotulo(this StatusPresenca status) => status switch
    {
        StatusPresenca.Presente => "Presente",
        StatusPresenca.Falta => "Falta",
        StatusPresenca.Justificada => "Justificada",
        _ => status.ToString()
    };
}
