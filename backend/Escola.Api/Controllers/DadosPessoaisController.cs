using System.IO.Compression;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Escola.Api.Auth;
using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Escola.Infrastructure.Auditoria;
using Escola.Infrastructure.Data;
using Escola.Infrastructure.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Escola.Api.Controllers;

/// <summary>LGPD — direito de acesso e portabilidade (art. 18): tudo o que o sistema guarda sobre um aluno, num ZIP. Só a Gestão gera;
/// quem entrega à família é a escola. Cada exportação fica no histórico do aluno.</summary>
[ApiController]
[Route("api/alunos/{alunoId:guid}/dados-pessoais")]
[Authorize(Roles = GruposDePapeis.Gestao)]
public class DadosPessoaisController(
    EscolaDbContext db,
    IAuditoriaService auditoria,
    IDocumentoStorage documentos,
    ILogger<DadosPessoaisController> logger) : ControllerBase
{
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // acentos legíveis no arquivo (não vai pra HTML)
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        Converters = { new JsonStringEnumConverter() }
    };

    [HttpGet("exportar")]
    public async Task<IActionResult> Exportar(Guid alunoId)
    {
        var aluno = await db.Alunos.AsNoTracking().Include(a => a.Turma).FirstOrDefaultAsync(a => a.Id == alunoId);
        if (aluno is null) return NotFound("Aluno não encontrado.");

        var responsaveis = await db.AlunoResponsaveis.AsNoTracking().Where(ar => ar.AlunoId == alunoId)
            .Select(ar => new { ar.Responsavel.Nome, ar.Responsavel.Email, ar.Responsavel.Telefone, ar.ResponsavelFinanceiro })
            .ToListAsync();
        var ficha = await db.FichasSaude.AsNoTracking().Where(f => f.AlunoId == alunoId)
            .Select(f => new
            {
                f.TipoSanguineo, f.Alergias, f.RestricoesAlimentares, f.MedicamentosEmUso, f.CondicoesSaude, f.PlanoSaude,
                f.PediatraNome, f.PediatraTelefone, f.ContatoEmergenciaNome, f.ContatoEmergenciaTelefone,
                f.VacinacaoEmDia, f.AutorizaUsoImagem, f.AtestadoValidoAte, f.AtualizadoEm
            })
            .FirstOrDefaultAsync();
        var docs = await db.DocumentosSaude.AsNoTracking().Where(d => d.AlunoId == alunoId).OrderBy(d => d.EnviadoEm).ToListAsync();
        var presencas = await db.RegistrosPresenca.AsNoTracking().Where(p => p.AlunoId == alunoId).OrderBy(p => p.Data)
            .Select(p => new { p.Data, p.Status, Turma = p.Turma.Nome })
            .ToListAsync();
        // A rotina é TPH (alimentação, sono, higiene...): serializa pelo tipo real de cada registro.
        var rotina = (await db.RegistrosRotina.AsNoTracking().Include(r => r.Fotos).Where(r => r.AlunoId == alunoId)
            .OrderBy(r => r.RegistradoEm).ToListAsync())
            .Select(r => (object)r).ToList();
        var cobrancas = await db.Cobrancas.AsNoTracking().Where(c => c.AlunoId == alunoId).OrderBy(c => c.Vencimento)
            .Select(c => new { c.Descricao, c.Valor, c.Vencimento, c.Paga, c.PagoEm, c.ValorPago, c.Cancelada, c.CanceladaEm })
            .ToListAsync();
        var jogos = await db.JogoAtletas.AsNoTracking().Where(j => j.AlunoId == alunoId).OrderBy(j => j.Jogo.Data)
            .Select(j => new
            {
                j.Jogo.Data, j.Jogo.Adversario, Campeonato = j.Jogo.Campeonato != null ? j.Jogo.Campeonato.Nome : null,
                j.Titular, j.Gols, j.CartoesAmarelos, j.CartaoVermelho
            })
            .ToListAsync();
        var termos = await db.TermosAceite.AsNoTracking().Where(t => t.AlunoId == alunoId).OrderBy(t => t.AceitoEm)
            .Select(t => new { Responsavel = t.Responsavel.Nome, t.Versao, t.AceitoEm, t.Ip, t.NavegadorUserAgent, t.TextoAceito })
            .ToListAsync();
        var fichaId = await db.FichasSaude.Where(f => f.AlunoId == alunoId).Select(f => (Guid?)f.Id).FirstOrDefaultAsync();
        var historico = await db.LogsAuditoria.AsNoTracking()
            .Where(l => l.EntidadeId == alunoId || (fichaId != null && l.EntidadeId == fichaId))
            .OrderBy(l => l.RegistradoEm)
            .Select(l => new { l.RegistradoEm, l.EntidadeTipo, l.Acao, Usuario = l.Usuario != null ? l.Usuario.Nome : "Sistema", l.Detalhe })
            .ToListAsync();

        var dados = new
        {
            GeradoEm = DateTime.UtcNow,
            Observacao = "Dados pessoais do(a) aluno(a) guardados no sistema da escola (LGPD, art. 18). Datas e horas em UTC.",
            Aluno = new
            {
                aluno.Nome, aluno.DataNascimento, Turma = aluno.Turma.Nome, aluno.Ativo, aluno.Posicao, aluno.FotoUrl,
                aluno.DescontoMensalidadePercentual, aluno.MotivoDesconto, aluno.MatriculaConfirmadaEm
            },
            Responsaveis = responsaveis,
            FichaSaude = ficha,
            DocumentosSaude = docs.Select(d => new { d.NomeArquivo, d.ContentType, d.TamanhoBytes, d.EnviadoEm, Arquivo = $"documentos/{NomeNoZip(d)}" }),
            Frequencia = presencas,
            Rotina = rotina,
            Cobrancas = cobrancas,
            Jogos = jogos,
            TermosAceitos = termos,
            Historico = historico
        };

        var zip = new MemoryStream();
        using (var arquivo = new ZipArchive(zip, ZipArchiveMode.Create, leaveOpen: true))
        {
            await using (var json = arquivo.CreateEntry("dados.json", CompressionLevel.Optimal).Open())
                await JsonSerializer.SerializeAsync(json, dados, Json);

            foreach (var doc in docs)
            {
                try
                {
                    await using var origem = documentos.Abrir(doc.ArquivoArmazenado);
                    await using var destino = arquivo.CreateEntry($"documentos/{NomeNoZip(doc)}", CompressionLevel.Optimal).Open();
                    await origem.CopyToAsync(destino);
                }
                catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
                {
                    logger.LogWarning("Documento de saúde {Id} sem arquivo no disco — exportado só o registro.", doc.Id);
                }
            }
        }

        auditoria.Registrar(nameof(Aluno), alunoId, AcaoAuditoria.Editado, this.UsuarioIdAtual(), "Dados pessoais exportados (LGPD)");
        await db.SaveChangesAsync();

        zip.Position = 0;
        var nome = new string(aluno.Nome.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray()).Trim('-');
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(zip, "application/zip", $"dados-{nome}-{DateTime.UtcNow:yyyy-MM-dd}.zip");
    }

    /// <summary>Prefixo com o id evita colisão entre dois anexos de mesmo nome.</summary>
    private static string NomeNoZip(DocumentoSaude d) =>
        $"{d.Id.ToString()[..8]}-{string.Concat(d.NomeArquivo.Split(Path.GetInvalidFileNameChars()))}";
}
