using Escola.Api.Auth;
using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Escola.Infrastructure;
using Escola.Infrastructure.Auditoria;
using Escola.Infrastructure.Auth;
using Escola.Infrastructure.Data;
using Escola.Infrastructure.Seguranca;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Escola.Api.Controllers;

/// <param name="Disponivel">O ambiente tem o cofre de segredos (Segredos:Chave) — sem ele não dá pra guardar o segredo com segurança.</param>
public record DoisFatoresDto(bool Ativo, DateTime? AtivadoEm, int CodigosRecuperacaoRestantes, bool Disponivel);

/// <param name="Segredo">Pra digitar no app, se a câmera não ler o QR Code.</param>
/// <param name="UriOtpAuth">Vira o QR Code (gerado no navegador — o segredo não vai pra nenhum serviço externo).</param>
public record InicioDoisFatoresDto(string Segredo, string UriOtpAuth);

public record CodigoRequest(string? Codigo);

public record DesativarDoisFatoresRequest(string? Senha, string? Codigo);

/// <summary>Os códigos de recuperação aparecem só aqui, uma vez.</summary>
public record CodigosRecuperacaoDto(IReadOnlyList<string> Codigos);

/// <summary>Verificação em duas etapas da própria conta (código de um app autenticador). Opcional, pra qualquer conta — menos o Suporte, cujo
/// segundo fator é obrigatório e vem da configuração do deploy.</summary>
[ApiController]
[Route("api/minha-conta/dois-fatores")]
[Authorize]
public class MinhaContaController(EscolaDbContext db, CofreSegredos cofre, IAuditoriaService auditoria) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DoisFatoresDto>> Obter()
    {
        var u = await ContaAsync();
        if (u is null) return NotFound();
        return Ok(new DoisFatoresDto(u.DoisFatoresAtivo, u.TotpAtivadoEm, CodigosRecuperacao.Restantes(u.CodigosRecuperacaoHash), cofre.Disponivel));
    }

    /// <summary>Gera um segredo novo (ainda não vale pro login): a pessoa cadastra no app e confirma com o 1º código.</summary>
    [HttpPost("iniciar")]
    public async Task<ActionResult<InicioDoisFatoresDto>> Iniciar()
    {
        var u = await ContaAsync();
        if (u is null) return NotFound();
        if (u.Papel == PapelUsuario.Suporte) return BadRequest("O segundo fator do Suporte é configurado no ambiente, não por aqui.");
        if (!cofre.Disponivel) return BadRequest("A verificação em duas etapas não está disponível neste ambiente.");
        if (u.DoisFatoresAtivo) return BadRequest("A verificação em duas etapas já está ativa. Desative antes de cadastrar outro aparelho.");

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

        var segredo = cofre.Descriptografar(u.TotpSegredoPendenteCriptografado);
        if (!Totp.TentarValidar(segredo, request.Codigo, DateTime.UtcNow, null, out _))
            return BadRequest("Código inválido. Confira se o horário do celular está certo e digite o código que aparece agora no app.");

        var codigos = CodigosRecuperacao.Gerar();
        u.TotpSegredoCriptografado = u.TotpSegredoPendenteCriptografado;
        u.TotpSegredoPendenteCriptografado = null;
        u.TotpAtivadoEm = DateTime.UtcNow;
        u.CodigosRecuperacaoHash = CodigosRecuperacao.Guardar(codigos);
        auditoria.Registrar(nameof(Usuario), u.Id, AcaoAuditoria.Editado, u.Id, "Verificação em duas etapas ativada");
        await db.SaveChangesAsync();
        return Ok(new CodigosRecuperacaoDto(codigos));
    }

    /// <summary>Pede a senha e um código (do app ou de recuperação): quem achou um computador com a sessão aberta não desliga a proteção.</summary>
    [HttpPost("desativar")]
    public async Task<ActionResult<DoisFatoresDto>> Desativar(DesativarDoisFatoresRequest request)
    {
        var u = await ContaAsync();
        if (u is null) return NotFound();
        if (!u.DoisFatoresAtivo) return BadRequest("A verificação em duas etapas não está ativa.");
        if (string.IsNullOrEmpty(request.Senha) || !SenhaHasher.Verificar(request.Senha, u.SenhaHash)) return BadRequest("Senha incorreta.");
        if (!CodigoConfere(u, request.Codigo)) return BadRequest("Código inválido ou já utilizado.");

        Limpar(u);
        auditoria.Registrar(nameof(Usuario), u.Id, AcaoAuditoria.Editado, u.Id, "Verificação em duas etapas desativada");
        await db.SaveChangesAsync();
        return Ok(new DoisFatoresDto(false, null, 0, cofre.Disponivel));
    }

    /// <summary>Gera códigos de recuperação novos (os antigos deixam de valer). Pede um código do app.</summary>
    [HttpPost("novos-codigos")]
    public async Task<ActionResult<CodigosRecuperacaoDto>> NovosCodigos(CodigoRequest request)
    {
        var u = await ContaAsync();
        if (u is null) return NotFound();
        if (!u.DoisFatoresAtivo) return BadRequest("A verificação em duas etapas não está ativa.");
        if (CodigosRecuperacao.PareceCodigoDeRecuperacao(request.Codigo) || !CodigoConfere(u, request.Codigo))
            return BadRequest("Digite o código que aparece agora no app autenticador.");

        var codigos = CodigosRecuperacao.Gerar();
        u.CodigosRecuperacaoHash = CodigosRecuperacao.Guardar(codigos);
        auditoria.Registrar(nameof(Usuario), u.Id, AcaoAuditoria.Editado, u.Id, "Códigos de recuperação da verificação em duas etapas gerados de novo");
        await db.SaveChangesAsync();
        return Ok(new CodigosRecuperacaoDto(codigos));
    }

    /// <summary>Tira a verificação em duas etapas de uma conta (usado também pela Gestão, pra quem perdeu o celular e os códigos).</summary>
    public static void Limpar(Usuario u)
    {
        u.TotpSegredoCriptografado = null;
        u.TotpSegredoPendenteCriptografado = null;
        u.TotpAtivadoEm = null;
        u.CodigosRecuperacaoHash = null;
    }

    private bool CodigoConfere(Usuario u, string? codigo)
    {
        if (CodigosRecuperacao.PareceCodigoDeRecuperacao(codigo))
        {
            if (!CodigosRecuperacao.TentarUsar(u.CodigosRecuperacaoHash, codigo, out var restantes)) return false;
            u.CodigosRecuperacaoHash = restantes;
            return true;
        }
        return Totp.TentarValidar(cofre.Descriptografar(u.TotpSegredoCriptografado!), codigo, DateTime.UtcNow, null, out _);
    }

    private async Task<Usuario?> ContaAsync()
    {
        var id = this.UsuarioIdAtual();
        return await db.Usuarios.FirstOrDefaultAsync(u => u.Id == id);
    }
}
