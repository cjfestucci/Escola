namespace Escola.Infrastructure.Pagamentos;

/// <summary>Um Pix efetivamente recebido numa cobrança.</summary>
public sealed record PixRecebido(string EndToEndId, decimal Valor, DateTime HorarioUtc);

/// <param name="Status">Normalizado em maiúsculas: ATIVA, CONCLUIDA, REMOVIDA_PELO_USUARIO_RECEBEDOR, REMOVIDA_PELO_PSP.</param>
public sealed record CobrancaPixConsultada(string Status, IReadOnlyList<PixRecebido> Pix)
{
    public bool Concluida => Status == "CONCLUIDA";
    public bool Removida => Status.StartsWith("REMOVIDA", StringComparison.Ordinal);
}

public sealed record CobrancaPixCriada(string PixCopiaECola);

/// <summary>Falha ao falar com o banco (rede, credencial, regra recusada…). A mensagem é segura de mostrar/logar: nunca leva segredo.</summary>
public class PixProvedorException(string mensagem, int? statusHttp = null, Exception? inner = null) : Exception(mensagem, inner)
{
    public int? StatusHttp { get; } = statusHttp;
}

/// <summary>Cobrança Pix dinâmica num banco/PSP, pra dar baixa automática. Hoje só existe a implementação do Banco do Brasil.</summary>
public interface IProvedorPix
{
    bool Configurado { get; }

    Task<CobrancaPixCriada> CriarCobrancaAsync(string txid, string chavePix, decimal valor, int expiracaoSegundos, string? solicitacaoPagador, CancellationToken ct = default);

    /// <summary>Nulo se o banco não conhece essa cobrança.</summary>
    Task<CobrancaPixConsultada?> ConsultarCobrancaAsync(string txid, CancellationToken ct = default);

    Task RegistrarWebhookAsync(string chavePix, string urlWebhook, CancellationToken ct = default);

    /// <summary>Só obtém um token — prova que credenciais, certificado e endereços estão certos, sem criar nada.</summary>
    Task TestarConexaoAsync(CancellationToken ct = default);
}
