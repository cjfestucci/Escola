using Escola.Api.Auth;
using Escola.Api.Dtos;
using Escola.Api.Dtos.Requests;
using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Escola.Infrastructure.Auditoria;
using Escola.Infrastructure.Data;
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
    /// (inclusive antes do login) pra calcular "hoje" — e fuso horário não é dado sensível.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ConfiguracaoEscolaDto>> Obter()
    {
        var config = await db.ConfiguracoesEscola.FirstOrDefaultAsync();
        return Ok(new ConfiguracaoEscolaDto(config?.Id, config?.FusoHorario ?? RelogioEscola.FusoPadrao));
    }

    [HttpPut]
    [Authorize(Roles = GruposDePapeis.Gestao)]
    public async Task<ActionResult<ConfiguracaoEscolaDto>> Editar(EditarConfiguracaoEscolaRequest request)
    {
        var fusoNovo = request.FusoHorario?.Trim();
        if (!RelogioEscola.FusoValido(fusoNovo))
            return BadRequest("Fuso horário inválido.");

        var config = await db.ConfiguracoesEscola.FirstOrDefaultAsync();
        var criando = config is null;
        if (config is null)
        {
            config = new ConfiguracaoEscola { Id = Guid.NewGuid(), FusoHorario = RelogioEscola.FusoPadrao };
            db.ConfiguracoesEscola.Add(config);
        }

        var fusoAntes = config.FusoHorario;
        config.FusoHorario = fusoNovo!;
        config.AtualizadoEm = DateTime.UtcNow;

        auditoria.Registrar(nameof(ConfiguracaoEscola), config.Id, criando ? AcaoAuditoria.Criado : AcaoAuditoria.Editado, this.UsuarioIdAtual(),
            AuditoriaDetalhe.MontarAlteracoes(("Fuso horário", fusoAntes, config.FusoHorario)));
        await db.SaveChangesAsync();

        return Ok(new ConfiguracaoEscolaDto(config.Id, config.FusoHorario));
    }
}
