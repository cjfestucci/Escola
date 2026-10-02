namespace Escola.Infrastructure.Storage;

/// <summary>Guarda documentos em disco numa pasta que NÃO é servida como arquivo estático. Uso em desenvolvimento.</summary>
public class LocalDocumentoStorage(string pasta) : IDocumentoStorage
{
    public async Task<string> SalvarAsync(Stream conteudo, string extensao, CancellationToken ct = default)
    {
        Directory.CreateDirectory(pasta);

        var nome = $"{Guid.NewGuid()}{extensao}";
        await using var arquivo = File.Create(CaminhoSeguro(nome));
        await conteudo.CopyToAsync(arquivo, ct);

        return nome;
    }

    public Stream Abrir(string arquivoArmazenado) => File.OpenRead(CaminhoSeguro(arquivoArmazenado));

    public void Excluir(string arquivoArmazenado)
    {
        var caminho = CaminhoSeguro(arquivoArmazenado);
        if (File.Exists(caminho)) File.Delete(caminho);
    }

    // Defesa em profundidade: o nome vem do banco, mas nunca deixa sair da pasta de documentos.
    private string CaminhoSeguro(string nome)
    {
        var raiz = Path.GetFullPath(pasta);
        var caminho = Path.GetFullPath(Path.Combine(raiz, nome));
        if (!caminho.StartsWith(raiz + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidOperationException("Caminho de documento inválido.");
        return caminho;
    }
}
