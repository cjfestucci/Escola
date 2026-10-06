using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Escola.Api.Dtos;
using Escola.Api.Dtos.Requests;
using Escola.Api.Servicos;
using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Escola.Infrastructure.Auditoria;
using Escola.Infrastructure.Auth;
using Escola.Infrastructure.Clientes;
using Escola.Infrastructure.Data;
using Escola.Infrastructure.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Escola.Api.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController(
    EscolaDbContext db,
    IConfiguration config,
    IClienteAtual clienteAtual,
    ILinkSenhaService linkSenha,
    IAuditoriaService auditoria,
    LimitadorTentativasLogin limitador,
    ILogger<AuthController> logger) : ControllerBase
{
    /// <summary>Intervalo mínimo entre dois e-mails de redefinição pra mesma conta (evita usar o endpoint pra encher a caixa de alguém).</summary>
    private static readonly TimeSpan IntervaloMinimoEntrePedidos = TimeSpan.FromMinutes(2);

    private const int TamanhoMinimoSenha = 8;

    /// <summary>"Esqueci minha senha": manda um link de uso único pro e-mail da conta. A resposta é <b>sempre a mesma</b>
    /// (existindo a conta ou não) — senão o endpoint serviria pra descobrir quais e-mails têm cadastro.</summary>
    [HttpPost("esqueci-senha")]
    public async Task<IActionResult> EsqueciSenha(EsqueciSenhaRequest request)
    {
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        if (email.Length == 0) return BadRequest("Informe o e-mail.");

        var clienteAtivo = await db.Clientes.AnyAsync(c => c.Id == clienteAtual.Id && c.Ativo);
        var usuario = clienteAtivo ? await db.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == email && u.Ativo && u.Papel != PapelUsuario.Suporte) : null;

        if (usuario is not null)
        {
            var agora = DateTime.UtcNow;
            var recente = await db.RedefinicoesSenha.AnyAsync(r => r.UsuarioId == usuario.Id && r.CriadoEm > agora - IntervaloMinimoEntrePedidos);
            if (!recente) await linkSenha.EnviarAsync(usuario, TipoLinkSenha.Redefinicao);
        }

        return NoContent();
    }

    /// <summary>Define a nova senha a partir do link recebido por e-mail. Token inválido, expirado ou já usado: mesma mensagem.</summary>
    [HttpPost("redefinir-senha")]
    public async Task<IActionResult> RedefinirSenha(RedefinirSenhaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NovaSenha) || request.NovaSenha.Length < TamanhoMinimoSenha)
            return BadRequest($"A nova senha deve ter pelo menos {TamanhoMinimoSenha} caracteres.");

        const string linkInvalido = "Este link é inválido ou expirou. Peça um novo em \"Esqueceu a senha?\".";
        if (string.IsNullOrWhiteSpace(request.Token)) return BadRequest(linkInvalido);

        var hash = LinkSenhaService.HashDoToken(request.Token.Trim());
        var agora = DateTime.UtcNow;
        var pedido = await db.RedefinicoesSenha.Include(r => r.Usuario)
            .FirstOrDefaultAsync(r => r.TokenHash == hash && r.UsadoEm == null && r.ExpiraEm > agora);
        if (pedido is null || !pedido.Usuario.Ativo) return BadRequest(linkInvalido);

        pedido.Usuario.SenhaHash = SenhaHasher.Hash(request.NovaSenha);

        // Esse link (e qualquer outro ainda aberto dessa conta) não vale mais.
        var abertos = await db.RedefinicoesSenha.Where(r => r.UsuarioId == pedido.UsuarioId && r.UsadoEm == null).ToListAsync();
        foreach (var aberto in abertos) aberto.UsadoEm = agora;

        auditoria.Registrar(nameof(Usuario), pedido.UsuarioId, AcaoAuditoria.Editado, pedido.UsuarioId,
            pedido.Convite ? "Convite aceito: e-mail confirmado e senha cadastrada" : "Senha redefinida pelo link enviado por e-mail");
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Falhas (senha ou código) que bloqueiam a conta por 15 minutos. A do Suporte é mais apertada: tem mais poder.</summary>
    private const int LimiteFalhasLogin = 8;
    private const int LimiteFalhasLoginSuporte = 5;

    [HttpPost("entrar")]
    public async Task<ActionResult<LoginRespostaDto>> Entrar(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var chave = $"{clienteAtual.Id}:{email}";

        // Freio por conta (vale pra senha E pro código do segundo fator): sem ele, 1 milhão de códigos de 6 dígitos é questão de minutos.
        var ehEmailDoSuporte = string.Equals(email, config["Suporte:Email"]?.Trim(), StringComparison.OrdinalIgnoreCase);
        if (limitador.EstaBloqueado(chave, ehEmailDoSuporte ? LimiteFalhasLoginSuporte : LimiteFalhasLogin, out var restante))
        {
            var minutos = Math.Max(1, (int)Math.Ceiling(restante.TotalMinutes));
            return StatusCode(StatusCodes.Status429TooManyRequests, $"Muitas tentativas. Tente novamente em {minutos} {(minutos == 1 ? "minuto" : "minutos")}.");
        }

        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == email);

        if (usuario is null || !SenhaHasher.Verificar(request.Senha, usuario.SenhaHash))
        {
            limitador.RegistrarFalha(chave);
            return Unauthorized("E-mail ou senha inválidos.");
        }

        if (!usuario.Ativo)
            return Unauthorized("Esta conta foi desativada. Fale com a coordenação.");

        // Cliente suspenso: ninguém do cliente entra — mas o Suporte sim, senão não haveria como reativá-lo pela tela.
        // (Checado depois da senha, pra não revelar a suspensão a quem não tem credencial.)
        if (usuario.Papel != PapelUsuario.Suporte && !await db.Clientes.AnyAsync(c => c.Id == clienteAtual.Id && c.Ativo))
            return Unauthorized("O acesso desta escola está suspenso. Entre em contato com o suporte.");

        // Segundo fator (código do app autenticador): obrigatório pro Suporte. O segredo vem da configuração do deploy
        // (Suporte:TotpSegredo), nunca do banco — quem lê o banco não consegue gerar o código.
        if (usuario.Papel == PapelUsuario.Suporte)
        {
            var segredo = config["Suporte:TotpSegredo"];
            if (!Totp.SegredoValido(segredo))
                return Unauthorized("A conta de suporte deste ambiente não tem segundo fator configurado.");

            // Senha certa, falta o código: não emite token — a tela pede o código e reenvia.
            if (string.IsNullOrWhiteSpace(request.Codigo))
                return Ok(LoginRespostaDto.ExigeSegundoFator());

            if (!Totp.TentarValidar(segredo!, request.Codigo, DateTime.UtcNow, limitador.UltimoPasso(chave), out var passo))
            {
                limitador.RegistrarFalha(chave);
                return Unauthorized("Código de verificação inválido ou já utilizado.");
            }

            limitador.RegistrarPasso(chave, passo);
        }

        limitador.Limpar(chave);
        var token = GerarToken(usuario.Id, usuario.Nome, usuario.Papel.ToString(), usuario.ResponsavelId);
        return Ok(new LoginRespostaDto(token, usuario.Id, usuario.Nome, usuario.Papel.ToString(), usuario.ResponsavelId));
    }

    private string GerarToken(Guid usuarioId, string nome, string papel, Guid? responsavelId)
    {
        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Chave"]!));
        var credenciais = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuarioId.ToString()),
            new(ClaimTypes.Name, nome),
            new(ClaimTypes.Role, papel),
            new("clienteId", clienteAtual.Id.ToString())
        };
        if (responsavelId is { } id)
            claims.Add(new Claim("responsavelId", id.ToString()));

        // Conta de Suporte tem poder de configuração: sessão curta (12 h), em vez dos 30 dias de quem usa o app no dia a dia.
        var diasValidade = config.GetValue<int?>("Jwt:DiasValidade") ?? 30;
        var expira = papel == nameof(PapelUsuario.Suporte) ? DateTime.UtcNow.AddHours(12) : DateTime.UtcNow.AddDays(diasValidade);
        var token = new JwtSecurityToken(
            issuer: config["Jwt:Emissor"],
            claims: claims,
            expires: expira,
            signingCredentials: credenciais);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
