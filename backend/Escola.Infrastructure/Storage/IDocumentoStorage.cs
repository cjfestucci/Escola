namespace Escola.Infrastructure.Storage;

/// <summary>Armazenamento de documentos sensíveis (ex.: saúde). Ao contrário de <see cref="IFotoStorage"/>,
/// nada aqui é servido por URL pública — o conteúdo só sai por um endpoint que checa autorização.</summary>
public interface IDocumentoStorage
{
    /// <summary>Grava o conteúdo e devolve o nome com que ficou armazenado.</summary>
    Task<string> SalvarAsync(Stream conteudo, string extensao, CancellationToken ct = default);

    Stream Abrir(string arquivoArmazenado);

    void Excluir(string arquivoArmazenado);
}
