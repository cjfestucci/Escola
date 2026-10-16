namespace Escola.Infrastructure.Documentos;

/// <summary>CPF e CNPJ, inclusive o <b>CNPJ alfanumérico</b> da Receita (IN RFB 2.229/2024, emitido desde julho de 2026): 14 posições, as 12
/// primeiras com letras ou números e as 2 últimas (dígitos verificadores) só números. A conta do dígito é a mesma de sempre, com cada
/// caractere valendo "código ASCII − 48" ('0'–'9' = 0–9, 'A' = 17 … 'Z' = 42) — então o CNPJ só de números continua valendo igual.
/// CPF segue só números.</summary>
public static class DocumentoBr
{
    /// <summary>Tira pontuação e espaços e põe as letras em maiúsculas: "12.abc.345/01de-35" → "12ABC34501DE35". Pra guardar e comparar.</summary>
    public static string Normalizar(string? texto) =>
        new((texto ?? string.Empty).ToUpperInvariant().Where(c => c is >= '0' and <= '9' or >= 'A' and <= 'Z').ToArray());

    /// <summary>CPF (11 números) ou CNPJ (14, alfanumérico) válido, já normalizado (<see cref="Normalizar"/>).</summary>
    public static bool CpfOuCnpjValido(string documento) =>
        documento.Length == 11 ? CpfValido(documento) : CnpjValido(documento);

    public static bool EhCnpj(string documento) => documento.Length == 14;

    /// <summary>CPF com dígitos verificadores corretos (só números, 11 caracteres).</summary>
    public static bool CpfValido(string cpf)
    {
        if (cpf.Length != 11 || !cpf.All(char.IsAsciiDigit) || cpf.Distinct().Count() == 1) return false;

        for (var tamanho = 9; tamanho <= 10; tamanho++)
        {
            var soma = 0;
            for (var i = 0; i < tamanho; i++) soma += (cpf[i] - '0') * (tamanho + 1 - i);
            var digito = soma * 10 % 11 % 10;
            if (cpf[tamanho] - '0' != digito) return false;
        }
        return true;
    }

    /// <summary>CNPJ (numérico ou alfanumérico) com dígitos verificadores corretos, já normalizado.</summary>
    public static bool CnpjValido(string cnpj)
    {
        if (cnpj.Length != 14) return false;
        if (!cnpj[..12].All(c => c is >= '0' and <= '9' or >= 'A' and <= 'Z')) return false;
        if (!char.IsAsciiDigit(cnpj[12]) || !char.IsAsciiDigit(cnpj[13])) return false;
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
