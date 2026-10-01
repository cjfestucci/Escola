namespace Escola.Infrastructure.Auditoria;

/// <summary>Monta o texto "Campo alterado de X para Y" usado como `detalhe` de um log de Edição —
/// chamado com os pares (rótulo, valor antes, valor depois) que o controller já tem em mãos antes/depois
/// de aplicar a mutação. Campos que não mudaram não entram no texto. Cada alteração fica numa linha
/// separada (`\n`) — o frontend quebra por linha pra listar uma alteração por linha no modal, em vez
/// de "; " (que colidiria se algum valor de campo já contivesse ponto-e-vírgula).</summary>
public static class AuditoriaDetalhe
{
    public static string? MontarAlteracoes(params (string Campo, object? Antes, object? Depois)[] campos)
    {
        var mudancas = campos
            .Where(c => !Equals(c.Antes, c.Depois))
            .Select(c => $"{c.Campo} alterado de \"{Formatar(c.Antes)}\" para \"{Formatar(c.Depois)}\"")
            .ToList();

        return mudancas.Count == 0 ? null : string.Join("\n", mudancas);
    }

    private static string Formatar(object? valor) => valor switch
    {
        null => "vazio",
        string s when string.IsNullOrWhiteSpace(s) => "vazio",
        bool b => b ? "Sim" : "Não",
        DateOnly d => d.ToString("dd/MM/yyyy"),
        DateTime d => d.ToString("dd/MM/yyyy"),
        TimeOnly t => t.ToString("HH:mm"),
        decimal m => m.ToString("F2"),
        _ => valor.ToString() ?? "vazio"
    };
}
