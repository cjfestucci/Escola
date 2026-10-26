using Escola.Api.Auth;
using Escola.Api.Dtos;
using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Escola.Infrastructure.Auditoria;
using Escola.Infrastructure.Data;
using Escola.Infrastructure.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Escola.Api.Controllers;

[ApiController]
[Route("api/alunos/{alunoId:guid}/documentos-saude")]
[Authorize]
public class DocumentosSaudeController(EscolaDbContext db, IDocumentoStorage storage, IAuditoriaService auditoria) : ControllerBase
{
    private const long TamanhoMaximoBytes = 10 * 1024 * 1024;
    private const int MaximoPorAluno = 20;

    /// <summary>Equipe vê/baixa os documentos de qualquer aluno; um Responsável só os do(s) próprio(s) filho(s).</summary>
    [HttpGet]
    [Authorize(Roles = $"{GruposDePapeis.Equipe},{GruposDePapeis.Responsavel}")]
    public async Task<ActionResult<List<DocumentoSaudeDto>>> Listar(Guid alunoId)
    {
        if (!await db.Alunos.AnyAsync(a => a.Id == alunoId)) return NotFound("Aluno não encontrado.");
        if (!await PodeAcessarAsync(alunoId)) return Forbid();

        var documentos = await db.DocumentosSaude
            .Include(d => d.Usuario)
            .Where(d => d.AlunoId == alunoId)
            .OrderByDescending(d => d.EnviadoEm)
            .ToListAsync();
        return Ok(documentos.Select(d => d.ToDto()));
    }

    [HttpGet("{id:guid}/arquivo")]
    [Authorize(Roles = $"{GruposDePapeis.Equipe},{GruposDePapeis.Responsavel}")]
    public async Task<IActionResult> Baixar(Guid alunoId, Guid id)
    {
        if (!await PodeAcessarAsync(alunoId)) return Forbid();

        var documento = await db.DocumentosSaude.FirstOrDefaultAsync(d => d.Id == id && d.AlunoId == alunoId);
        if (documento is null) return NotFound("Documento não encontrado.");

        Stream stream;
        try
        {
            stream = storage.Abrir(documento.ArquivoArmazenado);
        }
        catch (FileNotFoundException)
        {
            return NotFound("O arquivo deste documento não foi encontrado.");
        }

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(stream, documento.ContentType, documento.NomeArquivo);
    }

    [HttpPost]
    [Authorize(Roles = GruposDePapeis.Cadastro)]
    [RequestSizeLimit(TamanhoMaximoBytes + 1024 * 1024)]
    public async Task<ActionResult<DocumentoSaudeDto>> Enviar(Guid alunoId, IFormFile arquivo, CancellationToken ct)
    {
        if (!await db.Alunos.AnyAsync(a => a.Id == alunoId, ct)) return NotFound("Aluno não encontrado.");

        if (arquivo is null || arquivo.Length == 0) return BadRequest("Nenhum arquivo enviado.");
        if (arquivo.Length > TamanhoMaximoBytes) return BadRequest("Arquivo maior que o limite de 10MB.");

        if (await db.DocumentosSaude.CountAsync(d => d.AlunoId == alunoId, ct) >= MaximoPorAluno)
            return BadRequest($"Limite de {MaximoPorAluno} documentos por aluno atingido.");

        // O tipo vem do conteúdo real (assinatura do arquivo), não do Content-Type/extensão que o cliente declara.
        await using var entrada = arquivo.OpenReadStream();
        var tipo = await DetectorDocumento.DetectarAsync(entrada, ct);
        if (tipo is null) return BadRequest("Formato não suportado. Envie um PDF ou uma imagem (JPEG, PNG ou WEBP).");

        entrada.Position = 0;
        var armazenado = await storage.SalvarAsync(entrada, tipo.Value.Extensao, ct);

        var nomeOriginal = Path.GetFileName(arquivo.FileName);
        if (string.IsNullOrWhiteSpace(nomeOriginal)) nomeOriginal = $"documento{tipo.Value.Extensao}";
        if (nomeOriginal.Length > 200) nomeOriginal = nomeOriginal[^200..];

        var documento = new DocumentoSaude
        {
            Id = Guid.NewGuid(),
            AlunoId = alunoId,
            NomeArquivo = nomeOriginal,
            ContentType = tipo.Value.ContentType,
            TamanhoBytes = arquivo.Length,
            ArquivoArmazenado = armazenado,
            UsuarioId = this.UsuarioIdAtual(),
            EnviadoEm = DateTime.UtcNow
        };

        try
        {
            db.DocumentosSaude.Add(documento);
            auditoria.Registrar(nameof(Aluno), alunoId, AcaoAuditoria.Editado, this.UsuarioIdAtual(), $"Documento de saúde anexado: {documento.NomeArquivo}");
            await db.SaveChangesAsync(ct);
        }
        catch
        {
            storage.Excluir(armazenado);
            throw;
        }

        var criado = await db.DocumentosSaude.Include(d => d.Usuario).FirstAsync(d => d.Id == documento.Id, ct);
        return CreatedAtAction(nameof(Listar), new { alunoId }, criado.ToDto());
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = GruposDePapeis.Cadastro)]
    public async Task<IActionResult> Remover(Guid alunoId, Guid id)
    {
        var documento = await db.DocumentosSaude.FirstOrDefaultAsync(d => d.Id == id && d.AlunoId == alunoId);
        if (documento is null) return NotFound("Documento não encontrado.");

        // Remoção de verdade (e não "desativar"): um documento de saúde anexado ao aluno errado precisa
        // poder sumir por completo. O rastro de quem removeu o quê fica na auditoria do aluno.
        db.DocumentosSaude.Remove(documento);
        auditoria.Registrar(nameof(Aluno), alunoId, AcaoAuditoria.Editado, this.UsuarioIdAtual(), $"Documento de saúde removido: {documento.NomeArquivo}");
        await db.SaveChangesAsync();

        storage.Excluir(documento.ArquivoArmazenado);
        return NoContent();
    }

    private async Task<bool> PodeAcessarAsync(Guid alunoId)
    {
        if (!User.IsInRole("Responsavel")) return true;

        var responsavelId = this.ResponsavelIdAtual();
        return await db.AlunoResponsaveis.AnyAsync(ar => ar.AlunoId == alunoId && ar.ResponsavelId == responsavelId);
    }
}
