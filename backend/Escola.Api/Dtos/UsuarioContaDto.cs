namespace Escola.Api.Dtos;

/// <param name="ConvitePendente">A conta ainda não cadastrou a senha (o convite por e-mail não foi aceito).</param>
public record UsuarioContaDto(Guid Id, string Nome, string Email, string Papel, bool Ativo, bool ConvitePendente = false, bool DoisFatores = false);

/// <summary>Resultado de criar uma conta da equipe ou de mandar um link de senha pra ela.</summary>
/// <param name="Link">Só quando o e-mail <b>não</b> saiu (ex.: sem SMTP): a Gestão repassa o link à pessoa. É segredo — equivale a
/// definir a senha da conta.</param>
public record EnvioLinkContaDto(UsuarioContaDto Conta, bool EmailEnviado, string? Link, string? Aviso);
