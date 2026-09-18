namespace Escola.Infrastructure.Storage;

/// <summary>Salva fotos em disco, abaixo de <paramref name="pastaUploads"/>. Uso em desenvolvimento.</summary>
public class LocalFotoStorage(string pastaUploads) : IFotoStorage
{
    public async Task<string> SalvarAsync(Stream conteudo, string nomeArquivo, string contentType, CancellationToken ct = default)
    {
        Directory.CreateDirectory(pastaUploads);

        var extensao = Path.GetExtension(nomeArquivo);
        var nomeUnico = $"{Guid.NewGuid()}{extensao}";
        var caminhoCompleto = Path.Combine(pastaUploads, nomeUnico);

        await using var arquivo = File.Create(caminhoCompleto);
        await conteudo.CopyToAsync(arquivo, ct);

        return $"/uploads/{nomeUnico}";
    }
}
