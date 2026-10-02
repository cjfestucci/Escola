using Escola.Domain.Entities;

namespace Escola.Api.Dtos;

public record DocumentoSaudeDto(
    Guid Id,
    Guid AlunoId,
    string NomeArquivo,
    string ContentType,
    long TamanhoBytes,
    string EnviadoPorNome,
    DateTime EnviadoEm);

public static class DocumentoSaudeMapper
{
    /// <summary>Exige Usuario já carregado (Include).</summary>
    public static DocumentoSaudeDto ToDto(this DocumentoSaude d) => new(
        d.Id,
        d.AlunoId,
        d.NomeArquivo,
        d.ContentType,
        d.TamanhoBytes,
        d.Usuario.Nome,
        d.EnviadoEm);
}
