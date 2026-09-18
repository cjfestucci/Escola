using Escola.Domain.Enums;

namespace Escola.Domain.Entities;

public class RegistroAlimentacao : RegistroRotina
{
    public Refeicao Refeicao { get; set; }
    public StatusAlimentacao Status { get; set; }
}
