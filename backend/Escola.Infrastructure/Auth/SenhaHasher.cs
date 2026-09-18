using System.Security.Cryptography;

namespace Escola.Infrastructure.Auth;

/// <summary>Hash de senha com PBKDF2 (salt aleatório por senha, sem dependências externas).</summary>
public static class SenhaHasher
{
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
