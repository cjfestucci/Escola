using Escola.Api.Auth;
using Escola.Api.Dtos;
using Escola.Api.Dtos.Requests;
using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Escola.Infrastructure.Auditoria;
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
public class ConfiguracaoEscolaController(EscolaDbContext db, IAuditoriaService auditoria) : ControllerBase
{
    /// <summary>Anônimo de propósito: o frontend precisa do fuso antes de qualquer tela renderizar
    /// (inclusive antes do login) pra calcular "hoje", e da cor do tema pra já pintar a tela de login —
    /// nenhum dos dois é dado sensível.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ConfiguracaoEscolaDto>> Obter()
    {
        var config = await db.ConfiguracoesEscola.FirstOrDefaultAsync();
        return Ok(new ConfiguracaoEscolaDto(config?.Id, config?.FusoHorario ?? RelogioEscola.FusoPadrao, config?.CorPrincipal));
    }

    [HttpPut]
    [Authorize(Roles = GruposDePapeis.Gestao)]
    public async Task<ActionResult<ConfiguracaoEscolaDto>> Editar(EditarConfiguracaoEscolaRequest request)
    {
        var fusoNovo = request.FusoHorario?.Trim();
        if (!RelogioEscola.FusoValido(fusoNovo))
            return BadRequest("Fuso horário inválido.");

        var corNova = CorPrincipal.Normalizar(request.CorPrincipal);
        if (corNova is not null)
        {
            if (!CorPrincipal.FormatoValido(corNova))
                return BadRequest("Cor inválida. Use o formato #RRGGBB, por exemplo #6C5DD3.");
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

        var fusoAntes = config.FusoHorario;
        var corAntes = config.CorPrincipal;
        config.FusoHorario = fusoNovo!;
        config.CorPrincipal = corNova;
        config.AtualizadoEm = DateTime.UtcNow;

        auditoria.Registrar(nameof(ConfiguracaoEscola), config.Id, criando ? AcaoAuditoria.Criado : AcaoAuditoria.Editado, this.UsuarioIdAtual(),
            AuditoriaDetalhe.MontarAlteracoes(
                ("Fuso horário", fusoAntes, config.FusoHorario),
                ("Cor principal", corAntes ?? "padrão", config.CorPrincipal ?? "padrão")));
        await db.SaveChangesAsync();

        return Ok(new ConfiguracaoEscolaDto(config.Id, config.FusoHorario, config.CorPrincipal));
    }
}
