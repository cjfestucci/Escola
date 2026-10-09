using Escola.Api.Auth;
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
    ClienteAtual clienteAtual,
    IServiceScopeFactory escopos,
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

        // O login é um só pra todas as escolas: o mesmo e-mail pode ter conta em mais de uma — cada conta recebe o próprio link
        // (com o nome da escola no e-mail). Busca sem o filtro de cliente (não há cliente numa requisição sem login).
        var contas = await db.Usuarios.IgnoreQueryFilters()
            .Where(u => u.Email.ToLower() == email && u.Ativo && u.Papel != PapelUsuario.Suporte)
            .Select(u => new { u.Id, ClienteId = EF.Property<Guid>(u, EscolaDbContext.ColunaCliente) })
            .ToListAsync();
        var idsClientes = contas.Select(c => c.ClienteId).Distinct().ToList();
        var clientesAtivos = await db.Clientes.Where(c => idsClientes.Contains(c.Id) && c.Ativo).ToDictionaryAsync(c => c.Id, c => c.Nome);

        foreach (var conta in contas.Where(c => clientesAtivos.ContainsKey(c.ClienteId)))
        {
            // Um escopo por escola: o link (e o log) gravam no cliente da conta.
            using var escopo = escopos.CreateScope();
            escopo.ServiceProvider.GetRequiredService<ClienteAtual>().Definir(conta.ClienteId);
            var dbConta = escopo.ServiceProvider.GetRequiredService<EscolaDbContext>();
            var usuario = await dbConta.Usuarios.FirstAsync(u => u.Id == conta.Id);
            var agora = DateTime.UtcNow;
            var recente = await dbConta.RedefinicoesSenha.AnyAsync(r => r.UsuarioId == usuario.Id && r.CriadoEm > agora - IntervaloMinimoEntrePedidos);
            if (!recente)
                await escopo.ServiceProvider.GetRequiredService<ILinkSenhaService>().EnviarAsync(usuario, TipoLinkSenha.Redefinicao, clientesAtivos[conta.ClienteId]);
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

        // Sem login não há cliente: ele vem do próprio link (o hash do token é único no banco todo).
        var clienteDoLink = await db.RedefinicoesSenha.IgnoreQueryFilters()
            .Where(r => r.TokenHash == hash)
            .Select(r => EF.Property<Guid>(r, EscolaDbContext.ColunaCliente))
            .FirstOrDefaultAsync();
        if (clienteDoLink == Guid.Empty) return BadRequest(linkInvalido);
        clienteAtual.Definir(clienteDoLink);

        var pedido = await db.RedefinicoesSenha.Include(r => r.Usuario)
            .FirstOrDefaultAsync(r => r.TokenHash == hash && r.UsadoEm == null && r.ExpiraEm > agora);
        if (pedido is null || !pedido.Usuario.Ativo) return BadRequest(linkInvalido);

        pedido.Usuario.SenhaHash = SenhaHasher.Hash(request.NovaSenha);
        pedido.Usuario.EncerrarSessoes(); // quem tinha a senha antiga (ou um aparelho perdido) sai de todas as sessões

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
        // Freio por e-mail (o login é um só pra todas as escolas).
        var chave = email;

        // Freio por conta (vale pra senha E pro código do segundo fator): sem ele, 1 milhão de códigos de 6 dígitos é questão de minutos.
        var ehEmailDoSuporte = string.Equals(email, config["Suporte:Email"]?.Trim(), StringComparison.OrdinalIgnoreCase);
        if (limitador.EstaBloqueado(chave, ehEmailDoSuporte ? LimiteFalhasLoginSuporte : LimiteFalhasLogin, out var restante))
        {
            var minutos = Math.Max(1, (int)Math.Ceiling(restante.TotalMinutes));
            return StatusCode(StatusCodes.Status429TooManyRequests, $"Muitas tentativas. Tente novamente em {minutos} {(minutos == 1 ? "minuto" : "minutos")}.");
        }

        // Todas as escolas compartilham esta tela: procura o e-mail em todas e fica com as contas cuja senha confere. Cada escola
        // continua tendo a própria conta (e a própria senha) — quem tem a mesma senha em mais de uma escolhe em qual entrar.
        var contas = await db.Usuarios.IgnoreQueryFilters()
            .Where(u => u.Email.ToLower() == email)
            .Select(u => new { Usuario = u, ClienteId = EF.Property<Guid>(u, EscolaDbContext.ColunaCliente) })
            .ToListAsync();
        var validas = contas.Where(c => SenhaHasher.Verificar(request.Senha, c.Usuario.SenhaHash)).ToList();
        if (request.ClienteId is { } escolhido)
            validas = validas.Where(c => c.ClienteId == escolhido).ToList();

        if (validas.Count == 0)
        {
            limitador.RegistrarFalha(chave);
            return Unauthorized("E-mail ou senha inválidos.");
        }

        if (validas.Count > 1)
        {
            var ids = validas.Select(v => v.ClienteId).ToList();
            var nomes = await db.Clientes.Where(c => ids.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Nome);
            return Ok(LoginRespostaDto.ExigeEscolhaDeCliente(validas
                .Select(v => new OpcaoClienteLoginDto(v.ClienteId, nomes.GetValueOrDefault(v.ClienteId, "Escola")))
                .OrderBy(o => o.Nome)
                .ToList()));
        }

        var usuario = validas[0].Usuario;
        var clienteDaConta = validas[0].ClienteId;
        clienteAtual.Definir(clienteDaConta);

        if (!usuario.Ativo)
            return Unauthorized("Esta conta foi desativada. Fale com a coordenação.");

        // Cliente suspenso: ninguém do cliente entra — mas o Suporte sim, senão não haveria como reativá-lo pela tela.
        // (Checado depois da senha, pra não revelar a suspensão a quem não tem credencial.)
        if (usuario.Papel != PapelUsuario.Suporte && !await db.Clientes.AnyAsync(c => c.Id == clienteDaConta && c.Ativo))
            return Unauthorized("O acesso desta escola está suspenso. Entre em contato com o suporte.");

        // Assinatura do clube pendente/suspensa: só o Admin entra (pra pagar); o Suporte, como sempre, também.
        if (usuario.Papel is not (PapelUsuario.Suporte or PapelUsuario.Admin) && await Autenticacao.AssinaturaBloqueadaAsync(db))
            return Unauthorized("O acesso deste clube está suspenso por pendência na assinatura. Fale com o administrador do clube.");

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
        var token = GerarToken(usuario.Id, usuario.Nome, usuario.Papel.ToString(), usuario.ResponsavelId, clienteDaConta);
        return Ok(new LoginRespostaDto(token, usuario.Id, usuario.Nome, usuario.Papel.ToString(), usuario.ResponsavelId, ClienteId: clienteDaConta));
    }

    private string GerarToken(Guid usuarioId, string nome, string papel, Guid? responsavelId, Guid clienteId)
    {
        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Chave"]!));
        var credenciais = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuarioId.ToString()),
            new(ClaimTypes.Name, nome),
            new(ClaimTypes.Role, papel),
            new("clienteId", clienteId.ToString()),
            // Instante de emissão com precisão de tick (o "iat" do JWT é em segundos): comparado com Usuario.SessoesValidasDesde pra revogar sessões.
            new(Autenticacao.ClaimEmitidoEm, DateTime.UtcNow.Ticks.ToString())
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
