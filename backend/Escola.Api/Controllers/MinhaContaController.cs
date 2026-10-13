using Escola.Api.Auth;
using Escola.Api.Servicos;
using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Escola.Infrastructure;
using Escola.Infrastructure.Auditoria;
using Escola.Infrastructure.Auth;
using Escola.Infrastructure.Clientes;
using Escola.Infrastructure.Data;
using Escola.Infrastructure.Seguranca;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Escola.Api.Controllers;

/// <param name="Disponivel">O ambiente tem o cofre de segredos (Segredos:Chave) — sem ele não dá pra guardar o segredo do app com segurança.</param>
/// <param name="Metodo">"App" ou "Email" (nulo = desativada).</param>
/// <param name="EmailDisponivel">O ambiente envia e-mail (SMTP; em desenvolvimento o código sai no log da API).</param>
public record DoisFatoresDto(bool Ativo, DateTime? AtivadoEm, int CodigosRecuperacaoRestantes, bool Disponivel, string? Metodo, bool EmailDisponivel);

/// <param name="Segredo">Pra digitar no app, se a câmera não ler o QR Code.</param>
/// <param name="UriOtpAuth">Vira o QR Code (gerado no navegador — o segredo não vai pra nenhum serviço externo).</param>
public record InicioDoisFatoresDto(string Segredo, string UriOtpAuth);

/// <param name="Aviso">Ex.: o e-mail não pôde ser entregue, ou já foi enviado há pouco.</param>
public record EnvioCodigoDto(string EmailMascarado, string? Aviso);

public record CodigoRequest(string? Codigo);

public record DesativarDoisFatoresRequest(string? Senha, string? Codigo);

/// <summary>Os códigos de recuperação aparecem só aqui, uma vez.</summary>
public record CodigosRecuperacaoDto(IReadOnlyList<string> Codigos);

/// <summary>Verificação em duas etapas da própria conta — código de um app autenticador <b>ou</b> enviado por e-mail. Opcional, pra
/// qualquer conta — menos o Suporte, cujo segundo fator é obrigatório (app) e vem da configuração do deploy.</summary>
[ApiController]
[Route("api/minha-conta/dois-fatores")]
[Authorize]
public class MinhaContaController(EscolaDbContext db, CofreSegredos cofre, IAuditoriaService auditoria, CodigoEmailService codigoEmail, ClienteAtual clienteAtual, LimitadorTentativasLogin limitador) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DoisFatoresDto>> Obter()
    {
        var u = await ContaAsync();
        if (u is null) return NotFound();
        return Ok(ParaDto(u));
    }

    /// <summary>Gera um segredo novo (ainda não vale pro login): a pessoa cadastra no app e confirma com o 1º código.</summary>
    [HttpPost("iniciar")]
    public async Task<ActionResult<InicioDoisFatoresDto>> Iniciar()
    {
        var u = await ContaAsync();
        if (u is null) return NotFound();
        if (MotivoNaoPodeAtivar(u) is { } motivo) return BadRequest(motivo);
        if (!cofre.Disponivel) return BadRequest("A verificação em duas etapas pelo app não está disponível neste ambiente.");

        var segredo = Totp.GerarSegredo();
        u.TotpSegredoPendenteCriptografado = cofre.Criptografar(segredo);
        await db.SaveChangesAsync();
        return Ok(new InicioDoisFatoresDto(segredo, Totp.UriOtpAuth(MarcaProduto.Nome, u.Email, segredo)));
    }

    [HttpPost("confirmar")]
    public async Task<ActionResult<CodigosRecuperacaoDto>> Confirmar(CodigoRequest request)
    {
        var u = await ContaAsync();
        if (u is null) return NotFound();
        if (u.TotpSegredoPendenteCriptografado is null) return BadRequest("Comece a ativação de novo.");
        if (Bloqueado(u) is { } bloqueio) return bloqueio;

        var segredo = cofre.Descriptografar(u.TotpSegredoPendenteCriptografado);
        if (!Totp.TentarValidar(segredo, request.Codigo, DateTime.UtcNow, null, out _))
        {
            limitador.RegistrarFalha(Chave(u));
            return BadRequest("Código inválido. Confira se o horário do celular está certo e digite o código que aparece agora no app.");
        }

        u.TotpSegredoCriptografado = u.TotpSegredoPendenteCriptografado;
        u.TotpSegredoPendenteCriptografado = null;
        u.TotpAtivadoEm = DateTime.UtcNow;
        return Ok(await AtivarAsync(u, "Verificação em duas etapas ativada (app autenticador)"));
    }

    /// <summary>Começa a ativação por e-mail: manda um código pro e-mail da conta (o que prova que a pessoa recebe nele).</summary>
    [HttpPost("email/iniciar")]
    public async Task<ActionResult<EnvioCodigoDto>> IniciarEmail()
    {
        var u = await ContaAsync();
        if (u is null) return NotFound();
        if (MotivoNaoPodeAtivar(u) is { } motivo) return BadRequest(motivo);
        if (!codigoEmail.Disponivel) return BadRequest("O envio de e-mail não está configurado neste ambiente.");
        return Ok(await EnviarCodigoAsync(u));
    }

    [HttpPost("email/confirmar")]
    public async Task<ActionResult<CodigosRecuperacaoDto>> ConfirmarEmail(CodigoRequest request)
    {
        var u = await ContaAsync();
        if (u is null) return NotFound();
        if (MotivoNaoPodeAtivar(u) is { } motivo) return BadRequest(motivo);
        if (Bloqueado(u) is { } bloqueio) return bloqueio;
        if (!CodigoEmailService.TentarUsar(u, request.Codigo))
        {
            limitador.RegistrarFalha(Chave(u));
            return BadRequest("Código inválido ou expirado. Peça um novo código.");
        }

        u.DoisFatoresEmailAtivadoEm = DateTime.UtcNow;
        return Ok(await AtivarAsync(u, "Verificação em duas etapas ativada (código por e-mail)"));
    }

    /// <summary>Com a verificação por e-mail ativa: manda um código pra desativar ou gerar novos códigos de recuperação.</summary>
    [HttpPost("email/enviar-codigo")]
    public async Task<ActionResult<EnvioCodigoDto>> EnviarCodigo()
    {
        var u = await ContaAsync();
        if (u is null) return NotFound();
        if (!u.DoisFatoresPorEmail) return BadRequest("Esta conta não usa código por e-mail.");
        return Ok(await EnviarCodigoAsync(u));
    }

    /// <summary>Pede a senha e um código (do app, do e-mail ou de recuperação): quem achou um computador com a sessão aberta não desliga a proteção.</summary>
    [HttpPost("desativar")]
    public async Task<ActionResult<DoisFatoresDto>> Desativar(DesativarDoisFatoresRequest request)
    {
        var u = await ContaAsync();
        if (u is null) return NotFound();
        if (!u.DoisFatoresAtivo) return BadRequest("A verificação em duas etapas não está ativa.");
        if (Bloqueado(u) is { } bloqueio) return bloqueio;
        if (string.IsNullOrEmpty(request.Senha) || !SenhaHasher.Verificar(request.Senha, u.SenhaHash))
        {
            limitador.RegistrarFalha(Chave(u));
            return BadRequest("Senha incorreta.");
        }
        if (!CodigoConfere(u, request.Codigo))
        {
            limitador.RegistrarFalha(Chave(u));
            return BadRequest("Código inválido ou já utilizado.");
        }

        Limpar(u);
        auditoria.Registrar(nameof(Usuario), u.Id, AcaoAuditoria.Editado, u.Id, "Verificação em duas etapas desativada");
        await db.SaveChangesAsync();
        return Ok(ParaDto(u));
    }

    /// <summary>Gera códigos de recuperação novos (os antigos deixam de valer). Pede um código do app ou do e-mail.</summary>
    [HttpPost("novos-codigos")]
    public async Task<ActionResult<CodigosRecuperacaoDto>> NovosCodigos(CodigoRequest request)
    {
        var u = await ContaAsync();
        if (u is null) return NotFound();
        if (!u.DoisFatoresAtivo) return BadRequest("A verificação em duas etapas não está ativa.");
        if (Bloqueado(u) is { } bloqueio) return bloqueio;
        if (CodigosRecuperacao.PareceCodigoDeRecuperacao(request.Codigo) || !CodigoConfere(u, request.Codigo))
        {
            limitador.RegistrarFalha(Chave(u));
            return BadRequest(u.DoisFatoresPorEmail ? "Digite o código que enviamos por e-mail." : "Digite o código que aparece agora no app autenticador.");
        }

        var codigos = CodigosRecuperacao.Gerar();
        u.CodigosRecuperacaoHash = CodigosRecuperacao.Guardar(codigos);
        auditoria.Registrar(nameof(Usuario), u.Id, AcaoAuditoria.Editado, u.Id, "Códigos de recuperação da verificação em duas etapas gerados de novo");
        await db.SaveChangesAsync();
        return Ok(new CodigosRecuperacaoDto(codigos));
    }

    /// <summary>Tira a verificação em duas etapas de uma conta (usado também pela Gestão, pra quem perdeu o celular/o e-mail e os códigos).</summary>
    public static void Limpar(Usuario u)
    {
        u.TotpSegredoCriptografado = null;
        u.TotpSegredoPendenteCriptografado = null;
        u.TotpAtivadoEm = null;
        u.DoisFatoresEmailAtivadoEm = null;
        u.CodigoEmailHash = null;
        u.CodigoEmailExpiraEm = null;
        u.CodigosRecuperacaoHash = null;
    }

    private DoisFatoresDto ParaDto(Usuario u) => new(
        u.DoisFatoresAtivo,
        u.TotpAtivadoEm ?? u.DoisFatoresEmailAtivadoEm,
        CodigosRecuperacao.Restantes(u.CodigosRecuperacaoHash),
        cofre.Disponivel,
        u.DoisFatoresPorEmail ? "Email" : u.TotpSegredoCriptografado is not null ? "App" : null,
        codigoEmail.Disponivel);

    /// <summary>Mesmo freio do login (8 erros em 15 min): sem ele, quem tem a sessão aberta poderia testar códigos em massa.</summary>
    private const int LimiteFalhas = 8;

    private static string Chave(Usuario u) => $"minha-conta:{u.Id}";

    private ObjectResult? Bloqueado(Usuario u)
    {
        if (!limitador.EstaBloqueado(Chave(u), LimiteFalhas, out var restante)) return null;
        var minutos = Math.Max(1, (int)Math.Ceiling(restante.TotalMinutes));
        return StatusCode(StatusCodes.Status429TooManyRequests, $"Muitas tentativas. Tente novamente em {minutos} {(minutos == 1 ? "minuto" : "minutos")}.");
    }

    private static string? MotivoNaoPodeAtivar(Usuario u) =>
        u.Papel == PapelUsuario.Suporte ? "O segundo fator do Suporte é configurado no ambiente, não por aqui."
        : u.DoisFatoresAtivo ? "A verificação em duas etapas já está ativa. Desative antes de trocar o método."
        : null;

    private async Task<CodigosRecuperacaoDto> AtivarAsync(Usuario u, string detalhe)
    {
        var codigos = CodigosRecuperacao.Gerar();
        u.CodigosRecuperacaoHash = CodigosRecuperacao.Guardar(codigos);
        auditoria.Registrar(nameof(Usuario), u.Id, AcaoAuditoria.Editado, u.Id, detalhe);
        await db.SaveChangesAsync();
        return new CodigosRecuperacaoDto(codigos);
    }

    private async Task<EnvioCodigoDto> EnviarCodigoAsync(Usuario u)
    {
        var nomeEscola = await db.Clientes.Where(c => c.Id == clienteAtual.Id).Select(c => c.Nome).FirstOrDefaultAsync();
        var envio = await codigoEmail.EnviarAsync(u, nomeEscola);
        await db.SaveChangesAsync();
        return new EnvioCodigoDto(CodigoEmailService.Mascarar(u.Email), envio.Aviso);
    }

    private bool CodigoConfere(Usuario u, string? codigo)
    {
        if (CodigosRecuperacao.PareceCodigoDeRecuperacao(codigo))
        {
            if (!CodigosRecuperacao.TentarUsar(u.CodigosRecuperacaoHash, codigo, out var restantes)) return false;
            u.CodigosRecuperacaoHash = restantes;
            return true;
        }
        if (u.DoisFatoresPorEmail) return CodigoEmailService.TentarUsar(u, codigo);
        return Totp.TentarValidar(cofre.Descriptografar(u.TotpSegredoCriptografado!), codigo, DateTime.UtcNow, null, out _);
    }

    private async Task<Usuario?> ContaAsync()
    {
        var id = this.UsuarioIdAtual();
        return await db.Usuarios.FirstOrDefaultAsync(u => u.Id == id);
    }
}
