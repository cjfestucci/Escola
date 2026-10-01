using Escola.Api.Auth;
using Escola.Api.Dtos;
using Escola.Api.Dtos.Requests;
using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Escola.Infrastructure.Auditoria;
using Escola.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Escola.Api.Controllers;

[ApiController]
[Route("api/alunos/{alunoId:guid}/ficha-saude")]
[Authorize]
public class FichaSaudeController(EscolaDbContext db, IAuditoriaService auditoria) : ControllerBase
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
        var acao = AcaoAuditoria.Editado;

        string? detalhe = null;
        if (ficha is null)
        {
            ficha = new FichaSaude { Id = Guid.NewGuid(), AlunoId = alunoId };
            db.FichasSaude.Add(ficha);
            acao = AcaoAuditoria.Criado;
        }
        else
        {
            var tipoSanguineoAntes = ficha.TipoSanguineo;
            var alergiasAntes = ficha.Alergias;
            var restricoesAlimentaresAntes = ficha.RestricoesAlimentares;
            var medicamentosEmUsoAntes = ficha.MedicamentosEmUso;
            var condicoesSaudeAntes = ficha.CondicoesSaude;
            var planoSaudeAntes = ficha.PlanoSaude;
            var pediatraNomeAntes = ficha.PediatraNome;
            var pediatraTelefoneAntes = ficha.PediatraTelefone;
            var contatoEmergenciaNomeAntes = ficha.ContatoEmergenciaNome;
            var contatoEmergenciaTelefoneAntes = ficha.ContatoEmergenciaTelefone;
            var vacinacaoEmDiaAntes = ficha.VacinacaoEmDia;
            var autorizaUsoImagemAntes = ficha.AutorizaUsoImagem;

            AplicarCampos(ficha, request);

            detalhe = AuditoriaDetalhe.MontarAlteracoes(
                ("Tipo sanguíneo", tipoSanguineoAntes, ficha.TipoSanguineo),
                ("Alergias", alergiasAntes, ficha.Alergias),
                ("Restrições alimentares", restricoesAlimentaresAntes, ficha.RestricoesAlimentares),
                ("Medicamentos em uso", medicamentosEmUsoAntes, ficha.MedicamentosEmUso),
                ("Condições de saúde", condicoesSaudeAntes, ficha.CondicoesSaude),
                ("Plano de saúde", planoSaudeAntes, ficha.PlanoSaude),
                ("Pediatra", pediatraNomeAntes, ficha.PediatraNome),
                ("Telefone do pediatra", pediatraTelefoneAntes, ficha.PediatraTelefone),
                ("Contato de emergência", contatoEmergenciaNomeAntes, ficha.ContatoEmergenciaNome),
                ("Telefone de emergência", contatoEmergenciaTelefoneAntes, ficha.ContatoEmergenciaTelefone),
                ("Vacinação em dia", vacinacaoEmDiaAntes, ficha.VacinacaoEmDia),
                ("Autoriza uso de imagem", autorizaUsoImagemAntes, ficha.AutorizaUsoImagem));
        }

        if (acao == AcaoAuditoria.Criado)
            AplicarCampos(ficha, request);

        auditoria.Registrar(nameof(FichaSaude), ficha.Id, acao, this.UsuarioIdAtual(), detalhe);
        await db.SaveChangesAsync();

        return Ok(ficha.ToDto());
    }

    private static void AplicarCampos(FichaSaude ficha, SalvarFichaSaudeRequest request)
    {
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
    }
}
