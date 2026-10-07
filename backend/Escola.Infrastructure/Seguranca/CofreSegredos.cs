using System.Security.Cryptography;
using System.Text;

namespace Escola.Infrastructure.Seguranca;

/// <summary>Criptografa segredos que precisam ficar no banco (ex.: a chave de API da subconta Asaas de cada escola) com AES-256-GCM.
/// A chave vem da configuração do servidor (<c>Segredos:Chave</c>, 32 bytes em base64) — nunca do banco: quem só lê o banco (ou um
/// backup dele) não recupera os segredos. <b>Perder a chave = perder os segredos</b> (as escolas teriam de reconectar a conta), então
/// ela entra no cofre de senhas junto com a <c>Jwt:Chave</c>.</summary>
public sealed class CofreSegredos
{
    private const string Prefixo = "v1:";
    private const int TamanhoNonce = 12;
    private const int TamanhoTag = 16;

    private readonly byte[]? _chave;

    /// <param name="chaveBase64">32 bytes em base64. Nulo/inválido = cofre indisponível (quem precisa dele avisa e não grava nada).</param>
    public CofreSegredos(string? chaveBase64)
    {
        if (string.IsNullOrWhiteSpace(chaveBase64)) return;
        try
        {
            var bytes = Convert.FromBase64String(chaveBase64.Trim());
            if (bytes.Length == 32) _chave = bytes;
        }
        catch (FormatException)
        {
            // fica indisponível
        }
    }

    public bool Disponivel => _chave is not null;

    public static string GerarChave() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    public string Criptografar(string texto)
    {
        var chave = _chave ?? throw new InvalidOperationException("Segredos:Chave não configurada (32 bytes em base64).");
        var nonce = RandomNumberGenerator.GetBytes(TamanhoNonce);
        var claro = Encoding.UTF8.GetBytes(texto);
        var cifrado = new byte[claro.Length];
        var tag = new byte[TamanhoTag];
        using (var aes = new AesGcm(chave, TamanhoTag))
            aes.Encrypt(nonce, claro, cifrado, tag);

        return Prefixo + Convert.ToBase64String([.. nonce, .. tag, .. cifrado]);
    }

    public string Descriptografar(string guardado)
    {
        var chave = _chave ?? throw new InvalidOperationException("Segredos:Chave não configurada (32 bytes em base64).");
        if (!guardado.StartsWith(Prefixo, StringComparison.Ordinal))
            throw new CryptographicException("Formato de segredo desconhecido.");

        var bytes = Convert.FromBase64String(guardado[Prefixo.Length..]);
        var nonce = bytes.AsSpan(0, TamanhoNonce);
        var tag = bytes.AsSpan(TamanhoNonce, TamanhoTag);
        var cifrado = bytes.AsSpan(TamanhoNonce + TamanhoTag);
        var claro = new byte[cifrado.Length];
        using (var aes = new AesGcm(chave, TamanhoTag))
            aes.Decrypt(nonce, cifrado, tag, claro);
        return Encoding.UTF8.GetString(claro);
    }
}
