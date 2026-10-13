namespace Escola.Infrastructure.Storage;

/// <summary>
/// Guarda uma foto enviada e devolve a URL para acessá-la depois. A implementação local
/// (disco) serve para desenvolvimento; produção troca por um provedor de blob (Azure/S3/R2)
/// sem mexer em quem chama isso.
/// </summary>
public interface IFotoStorage
{
    Task<string> SalvarAsync(Stream conteudo, string nomeArquivo, string contentType, CancellationToken ct = default);

    /// <summary>Apaga a foto de uma URL devolvida por <see cref="SalvarAsync"/>. URL desconhecida ou que não é deste armazenamento: ignora.</summary>
    void Excluir(string url);
}
