using Escola.Api.Auth;
using Escola.Api.Dtos;
using Escola.Api.Dtos.Requests;
using Escola.Domain.Entities;
using Escola.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Escola.Api.Controllers;

[ApiController]
[Route("api/alunos/{alunoId:guid}/ficha-saude")]
[Authorize]
public class FichaSaudeController(EscolaDbContext db) : ControllerBase
{
    /// <summary>Equipe vê a ficha de qualquer aluno; um Responsável só a do(s) próprio(s) filho(s).</summary>
    [HttpGet]
    [Authorize(Roles = $"{GruposDePapeis.Equipe},{GruposDePapeis.Responsavel}")]
    public async Task<ActionResult<FichaSaudeDto>> Obter(Guid alunoId)
    {
        if (!await db.Alunos.AnyAsync(a => a.Id == alunoId))
            return NotFound("Aluno não encontrado.");

        if (User.IsInRole("Responsavel"))
        {
            var responsavelId = User.FindFirst("responsavelId")?.Value;
            var ehFilho = await db.AlunoResponsaveis.AnyAsync(ar => ar.AlunoId == alunoId && ar.ResponsavelId.ToString() == responsavelId);
            if (!ehFilho) return Forbid();
        }

        var ficha = await db.FichasSaude.FirstOrDefaultAsync(f => f.AlunoId == alunoId);
        return Ok(ficha is null ? FichaSaudeMapper.Vazia : ficha.ToDto());
    }

    [HttpPut]
    [Authorize(Roles = GruposDePapeis.Gestao)]
    public async Task<ActionResult<FichaSaudeDto>> Salvar(Guid alunoId, SalvarFichaSaudeRequest request)
    {
        if (!await db.Alunos.AnyAsync(a => a.Id == alunoId))
            return NotFound("Aluno não encontrado.");

        var ficha = await db.FichasSaude.FirstOrDefaultAsync(f => f.AlunoId == alunoId);
        if (ficha is null)
        {
            ficha = new FichaSaude { Id = Guid.NewGuid(), AlunoId = alunoId };
            db.FichasSaude.Add(ficha);
        }

        ficha.TipoSanguineo = request.TipoSanguineo;
        ficha.Alergias = request.Alergias;
        ficha.RestricoesAlimentares = request.RestricoesAlimentares;
        ficha.MedicamentosEmUso = request.MedicamentosEmUso;
        ficha.CondicoesSaude = request.CondicoesSaude;
        ficha.PlanoSaude = request.PlanoSaude;
        ficha.PediatraNome = request.PediatraNome;
        ficha.PediatraTelefone = request.PediatraTelefone;
        ficha.ContatoEmergenciaNome = request.ContatoEmergenciaNome;
        ficha.ContatoEmergenciaTelefone = request.ContatoEmergenciaTelefone;
        ficha.VacinacaoEmDia = request.VacinacaoEmDia;
        ficha.AutorizaUsoImagem = request.AutorizaUsoImagem;
        ficha.AtualizadoEm = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return Ok(ficha.ToDto());
    }
}
