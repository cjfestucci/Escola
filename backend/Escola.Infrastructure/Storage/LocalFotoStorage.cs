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

    public void Excluir(string url)
    {
        // Só o que este armazenamento gravou ("/uploads/<nome>") e nunca fora da pasta.
        if (!url.StartsWith("/uploads/", StringComparison.Ordinal)) return;
        var raiz = Path.GetFullPath(pastaUploads);
        var caminho = Path.GetFullPath(Path.Combine(raiz, url["/uploads/".Length..]));
        if (!caminho.StartsWith(raiz + Path.DirectorySeparatorChar, StringComparison.Ordinal)) return;
        if (File.Exists(caminho)) File.Delete(caminho);
    }
}
