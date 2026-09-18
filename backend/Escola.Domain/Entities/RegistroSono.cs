namespace Escola.Domain.Entities;

public class RegistroSono : RegistroRotina
{
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly? HoraFim { get; set; }
}
