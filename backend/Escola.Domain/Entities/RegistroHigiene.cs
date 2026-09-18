using Escola.Domain.Enums;

namespace Escola.Domain.Entities;

public class RegistroHigiene : RegistroRotina
{
    public TipoHigiene Tipo { get; set; }
}
