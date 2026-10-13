namespace Escola.Domain.Enums;

/// <summary>Pix recebido que a baixa automática não resolve sozinha: a escola precisa conferir (e, se for o caso, devolver o valor).
/// Gravado como inteiro — não reordenar.</summary>
public enum PendenciaPix
{
    /// <summary>O Pix caiu numa cobrança que a escola tinha cancelado.</summary>
    CobrancaCancelada = 1,

    /// <summary>O Pix caiu numa cobrança que já estava paga (possível duplicidade).</summary>
    CobrancaJaPaga = 2
}
