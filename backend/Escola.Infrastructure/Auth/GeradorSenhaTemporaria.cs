using System.Security.Cryptography;

namespace Escola.Infrastructure.Auth;

/// <summary>Gera senhas temporárias legíveis (sem 0/O/1/l/I, pra evitar confusão ao digitar/ditar por telefone).</summary>
public static class GeradorSenhaTemporaria
{
    private const string Alfabeto = "23456789ABCDEFGHJKMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz";

    public static string Gerar(int tamanho = 8)
    {
        Span<char> senha = stackalloc char[tamanho];
        for (var i = 0; i < tamanho; i++)
            senha[i] = Alfabeto[RandomNumberGenerator.GetInt32(Alfabeto.Length)];

        return new string(senha);
    }
}
