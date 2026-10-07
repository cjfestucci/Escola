using Escola.Domain.Enums;

namespace Escola.Infrastructure.Pagamentos;

/// <summary>Valida e normaliza uma chave Pix pro formato que o BR Code exige. Uma chave fora do formato gera
/// um "copia e cola" que o app do banco recusa, então é melhor barrar (ou corrigir, como o +55 do telefone)
/// na hora de cadastrar do que descobrir na hora do pai pagar.</summary>
public static class ChavePix
{
    private const int TamanhoMaximo = 77;

    /// <returns>A chave já no formato final, ou <c>null</c> com <paramref name="erro"/> preenchido.</returns>
    public static string? Normalizar(TipoChavePix tipo, string entrada, out string? erro)
    {
        erro = null;
        var texto = entrada.Trim();

        switch (tipo)
        {
            case TipoChavePix.Cpf:
            {
                var digitos = SoDigitos(texto);
                if (digitos.Length != 11 || !CpfValido(digitos))
                {
                    erro = "CPF inválido. Informe os 11 números do CPF (ex.: 123.456.789-09).";
                    return null;
                }
                return digitos;
            }
            case TipoChavePix.Cnpj:
            {
                var digitos = SoDigitos(texto);
                if (digitos.Length != 14 || !CnpjValido(digitos))
                {
                    erro = "CNPJ inválido. Informe os 14 números do CNPJ (ex.: 12.345.678/0001-95).";
                    return null;
                }
                return digitos;
            }
            case TipoChavePix.Telefone:
            {
                var digitos = SoDigitos(texto);
                // Um "+" explícito com outro código de país (ex.: +1) não é número brasileiro: sem esta checagem o "+" era
                // ignorado e o "55" acrescentado, aceitando um telefone estrangeiro como se fosse do Brasil.
                if (texto.StartsWith('+') && !digitos.StartsWith("55"))
                {
                    erro = "Telefone inválido. Use +55, o DDD e o número (ex.: +5511999998888).";
                    return null;
                }
                // Sem o código do país (10 ou 11 dígitos: DDD + número) acrescenta o 55; com ele (12 ou 13) mantém.
                // Um DDD 55 (RS) sem o código do país tem 11 dígitos, então não se confunde com "55" + número.
                if (digitos.Length is 10 or 11) digitos = "55" + digitos;
                if (digitos.Length is not (12 or 13) || !digitos.StartsWith("55"))
                {
                    erro = "Telefone inválido. Use +55, o DDD e o número (ex.: +5511999998888).";
                    return null;
                }
                var ddd = int.Parse(digitos.AsSpan(2, 2));
                if (ddd < 11)
                {
                    erro = "Telefone inválido: confira o DDD (ex.: +5511999998888).";
                    return null;
                }
                return "+" + digitos;
            }
            case TipoChavePix.Email:
            {
                var email = texto.ToLowerInvariant();
                var arroba = email.IndexOf('@');
                if (email.Length > TamanhoMaximo || arroba < 1 || arroba != email.LastIndexOf('@')
                    || !email[(arroba + 1)..].Contains('.') || email.Any(char.IsWhiteSpace))
                {
                    erro = "E-mail inválido (ex.: financeiro@suaescola.com.br).";
                    return null;
                }
                return email;
            }
            case TipoChavePix.Aleatoria:
            {
                if (!Guid.TryParse(texto, out var guid) || texto.Length != 36)
                {
                    erro = "Chave aleatória inválida. Copie do app do banco (ex.: 123e4567-e89b-12d3-a456-426614174000).";
                    return null;
                }
                return guid.ToString("D");
            }
            default:
                erro = "Tipo de chave Pix inválido.";
                return null;
        }
    }

    private static string SoDigitos(string texto) => new(texto.Where(char.IsDigit).ToArray());

    /// <summary>CPF com dígitos verificadores corretos (só dígitos, 11 caracteres).</summary>
    public static bool CpfValido(string cpf)
    {
        if (cpf.Distinct().Count() == 1) return false;

        for (var tamanho = 9; tamanho <= 10; tamanho++)
        {
            var soma = 0;
            for (var i = 0; i < tamanho; i++) soma += (cpf[i] - '0') * (tamanho + 1 - i);
            var digito = soma * 10 % 11 % 10;
            if (cpf[tamanho] - '0' != digito) return false;
        }
        return true;
    }

    /// <summary>CNPJ com dígitos verificadores corretos (só dígitos, 14 caracteres).</summary>
    public static bool CnpjValido(string cnpj)
    {
        if (cnpj.Distinct().Count() == 1) return false;

        int[] pesos1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        int[] pesos2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        return DigitoCnpj(cnpj, pesos1) == cnpj[12] - '0' && DigitoCnpj(cnpj, pesos2) == cnpj[13] - '0';
    }

    private static int DigitoCnpj(string cnpj, int[] pesos)
    {
        var soma = 0;
        for (var i = 0; i < pesos.Length; i++) soma += (cnpj[i] - '0') * pesos[i];
        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }
}
