using Escola.Api.Auth;
using Escola.Api.Dtos;
using Escola.Api.Dtos.Requests;
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
public class ConfiguracaoEscolaController(EscolaDbContext db, IAuditoriaService auditoria, IClienteAtual clienteAtual) : ControllerBase
{
    /// <summary>Segmento (escola/clube) do cliente deste deploy. A tabela <c>Clientes</c> não tem filtro por cliente,
    /// então a linha é buscada pelo id configurado.</summary>
    private Task<SegmentoCliente> SegmentoAtualAsync() =>
        db.Clientes.Where(c => c.Id == clienteAtual.Id).Select(c => c.Segmento).FirstOrDefaultAsync();

    /// <summary>Anônimo de propósito: o frontend precisa do fuso antes de qualquer tela renderizar
    /// (inclusive antes do login) pra calcular "hoje", da cor do tema pra já pintar a tela de login e do segmento
    /// (escola/clube) pra já usar o vocabulário certo nela — nenhum dos três é dado sensível.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ConfiguracaoEscolaDto>> Obter()
    {
        var config = await db.ConfiguracoesEscola.FirstOrDefaultAsync();
        return Ok(new ConfiguracaoEscolaDto(config?.Id, config?.FusoHorario ?? RelogioEscola.FusoPadrao, config?.CorPrincipal, await SegmentoAtualAsync(), config?.LogoUrl));
    }

    [HttpPut]
    [Authorize(Roles = GruposDePapeis.GestaoOuSuporte)]
    public async Task<ActionResult<ConfiguracaoEscolaDto>> Editar(EditarConfiguracaoEscolaRequest request)
    {
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

        // O fuso horário não é editado aqui (é do Suporte, na tela Plataforma): só a cor muda.
        var corAntes = config.CorPrincipal;
        config.CorPrincipal = corNova;
        config.AtualizadoEm = DateTime.UtcNow;

        auditoria.Registrar(nameof(ConfiguracaoEscola), config.Id, criando ? AcaoAuditoria.Criado : AcaoAuditoria.Editado, this.UsuarioIdAtual(),
            AuditoriaDetalhe.MontarAlteracoes(("Cor principal", corAntes ?? "padrão", config.CorPrincipal ?? "padrão")));
        await db.SaveChangesAsync();

        return Ok(new ConfiguracaoEscolaDto(config.Id, config.FusoHorario, config.CorPrincipal, await SegmentoAtualAsync(), config.LogoUrl));
    }
}
