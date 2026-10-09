using Escola.Api.Auth;
using Escola.Api.Dtos;
using Escola.Api.Dtos.Requests;
using Escola.Api.Servicos;
using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Escola.Infrastructure.Auditoria;
using Escola.Infrastructure.Clientes;
using Escola.Infrastructure.Data;
using Escola.Infrastructure.Tema;
using Escola.Infrastructure.Tempo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Escola.Api.Controllers;

[ApiController]
[Route("api/configuracao/escola")]
[Authorize]
public class ConfiguracaoEscolaController(EscolaDbContext db, IAuditoriaService auditoria, IClienteAtual clienteAtual, IdentidadeEscolaService identidade) : ControllerBase
{
    /// <summary>Anônimo de propósito: o frontend carrega isto antes de qualquer tela. <b>Logado</b>, devolve a configuração da escola do
    /// token (fuso, cor, segmento, logo). <b>Sem login</b> não há escola (a tela de login é a mesma pra todas, desde 2026-10-07): devolve o
    /// padrão do produto — fuso de Brasília, cor e marca padrão. Nada aqui é dado sensível.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ConfiguracaoEscolaDto>> Obter()
    {
        var config = await db.ConfiguracoesEscola.FirstOrDefaultAsync();
        var cliente = clienteAtual.Definido ? await db.Clientes.FirstOrDefaultAsync(c => c.Id == clienteAtual.Id) : null;
        return Ok(IdentidadeEscolaService.ParaDto(config, cliente));
    }

    /// <summary>Nome e fuso horário da escola — <b>só o Admin</b> (identidade da empresa; o Coordenador mexe só na cor) e o Suporte.
    /// Trocar o fuso muda o "hoje" de todos os usuários: a tela avisa antes.</summary>
    [HttpPut("dados")]
    [Authorize(Roles = GruposDePapeis.AdminOuSuporte)]
    public async Task<ActionResult<ConfiguracaoEscolaDto>> EditarDados(EditarDadosEscolaRequest request)
    {
        var (config, erro) = await identidade.DefinirDadosAsync(request.NomeEscola, request.FusoHorario, this.UsuarioIdAtual());
        return erro is not null ? BadRequest(erro) : Ok(config);
    }

    [HttpPut("logo")]
    [Authorize(Roles = GruposDePapeis.AdminOuSuporte)]
    [RequestSizeLimit(IdentidadeEscolaService.TamanhoMaximoLogoBytes + 64 * 1024)]
    public async Task<ActionResult<LogoDto>> EnviarLogo(IFormFile arquivo, CancellationToken ct)
    {
        var (url, erro) = await identidade.SalvarLogoAsync(arquivo, this.UsuarioIdAtual(), ct);
        return erro is not null ? BadRequest(erro) : Ok(new LogoDto(url));
    }

    [HttpDelete("logo")]
    [Authorize(Roles = GruposDePapeis.AdminOuSuporte)]
    public async Task<ActionResult<LogoDto>> RemoverLogo()
    {
        await identidade.RemoverLogoAsync(this.UsuarioIdAtual());
        return Ok(new LogoDto(null));
    }

    [HttpPut]
    [Authorize(Roles = GruposDePapeis.GestaoOuSuporte)]
    public async Task<ActionResult<ConfiguracaoEscolaDto>> Editar(EditarConfiguracaoEscolaRequest request)
    {
        var corNova = CorPrincipal.Normalizar(request.CorPrincipal);
        if (corNova is not null)
        {
            if (!CorPrincipal.FormatoValido(corNova))
                return BadRequest("Cor inválida. Use o formato #RRGGBB, por exemplo #0E2A3A.");
            if (CorPrincipal.ContrasteComBranco(corNova) < CorPrincipal.ContrasteMinimo)
                return BadRequest("Essa cor é clara demais: o texto branco dos botões e do menu ficaria difícil de ler. Escolha uma cor mais escura.");
        }

        var config = await db.ConfiguracoesEscola.FirstOrDefaultAsync();
        var criando = config is null;
        if (config is null)
        {
            config = new ConfiguracaoEscola { Id = Guid.NewGuid(), FusoHorario = RelogioEscola.FusoPadrao };
            db.ConfiguracoesEscola.Add(config);
        }

        // O fuso horário não é editado aqui (é do Suporte, na tela Plataforma): só a cor muda.
        var corAntes = config.CorPrincipal;
        config.CorPrincipal = corNova;
        config.AtualizadoEm = DateTime.UtcNow;

        auditoria.Registrar(nameof(ConfiguracaoEscola), config.Id, criando ? AcaoAuditoria.Criado : AcaoAuditoria.Editado, this.UsuarioIdAtual(),
            AuditoriaDetalhe.MontarAlteracoes(("Cor principal", corAntes ?? "padrão", config.CorPrincipal ?? "padrão")));
        await db.SaveChangesAsync();

        return Ok(IdentidadeEscolaService.ParaDto(config, await db.Clientes.FirstOrDefaultAsync(c => c.Id == clienteAtual.Id)));
    }
}
