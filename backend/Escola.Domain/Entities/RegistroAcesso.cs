using Escola.Domain.Enums;

namespace Escola.Domain.Entities;

/// <summary>Registro de acesso à aplicação (Marco Civil da Internet, art. 15): data e hora de uso a partir de um IP. Guardado por
/// <b>6 meses</b>, em sigilo — não há tela nem endpoint que o mostre; só sai por requisição judicial. Sem chave estrangeira pra conta, de
/// propósito: o registro precisa sobreviver à exclusão dos dados do clube (por isso guarda o e-mail).</summary>
public class RegistroAcesso
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public string Email { get; set; } = string.Empty;
    public TipoRegistroAcesso Tipo { get; set; }
    public string? Ip { get; set; }
    public string? Navegador { get; set; }
    public DateTime OcorridoEm { get; set; }
}
