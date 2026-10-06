using System.Security.Cryptography;

namespace Escola.Infrastructure.Auth;

/// <summary>Hash de senha com PBKDF2 (salt aleatório por senha, sem dependências externas).</summary>
public static class SenhaHasher
{
    /// <summary>Valor guardado no lugar do hash enquanto a conta foi criada por convite e a pessoa ainda não cadastrou a senha. Não é um
    /// hash válido (sem ".", fora do formato salt.hash), então <see cref="Verificar"/> nunca aceita nenhuma senha pra ela.</summary>
    public const string ConvitePendente = "!convite-pendente";

    private const int TamanhoSalt = 16;
    private const int TamanhoHash = 32;
    private const int Iteracoes = 100_000;

    public static string Hash(string senha)
    {
        var salt = RandomNumberGenerator.GetBytes(TamanhoSalt);
        var hash = Rfc2898DeriveBytes.Pbkdf2(senha, salt, Iteracoes, HashAlgorithmName.SHA256, TamanhoHash);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verificar(string senha, string senhaHash)
    {
        var partes = senhaHash.Split('.');
        if (partes.Length != 2) return false;

        var salt = Convert.FromBase64String(partes[0]);
        var hashEsperado = Convert.FromBase64String(partes[1]);
        var hashCalculado = Rfc2898DeriveBytes.Pbkdf2(senha, salt, Iteracoes, HashAlgorithmName.SHA256, TamanhoHash);

        return CryptographicOperations.FixedTimeEquals(hashCalculado, hashEsperado);
    }
}
