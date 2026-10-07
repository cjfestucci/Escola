using Escola.Domain.Enums;

namespace Escola.Infrastructure.Pagamentos;

/// <summary>Um Pix efetivamente recebido numa cobrança.</summary>
public sealed record PixRecebido(string EndToEndId, decimal Valor, DateTime HorarioUtc);

/// <param name="Status">Normalizado em maiúsculas: ATIVA, CONCLUIDA, REMOVIDA_PELO_USUARIO_RECEBEDOR, REMOVIDA_PELO_PSP.</param>
public sealed record CobrancaPixConsultada(string Status, IReadOnlyList<PixRecebido> Pix)
{
    public bool Concluida => Status == "CONCLUIDA";
    public bool Removida => Status.StartsWith("REMOVIDA", StringComparison.Ordinal);
}

/// <summary>Quem paga — o gateway Asaas exige CPF/CNPJ do pagador (o BB não usa).</summary>
/// <param name="IdClienteExterno">Id do pagador já cadastrado no provedor, se houver (evita duplicar o cliente).</param>
public sealed record PagadorPix(Guid ResponsavelId, string Nome, string CpfCnpj, string? Email, string? IdClienteExterno);

public sealed record DadosNovaCobrancaPix(
    Guid CobrancaId,
    string ChavePix,
    decimal Valor,
    DateOnly Vencimento,
    int ExpiracaoSegundos,
    string Descricao,
    PagadorPix? Pagador);

/// <param name="IdExterno">Identificador da cobrança no provedor (txid no BB, id do pagamento no Asaas) — é por ele que se confere depois.</param>
/// <param name="IdClienteExternoCriado">Preenchido quando o provedor cadastrou o pagador agora (pra gravar e reaproveitar).</param>
public sealed record CobrancaPixCriada(string IdExterno, string PixCopiaECola, string? IdClienteExternoCriado = null);

/// <summary>Falha ao falar com o banco (rede, credencial, regra recusada…). A mensagem é segura de mostrar/logar: nunca leva segredo.</summary>
public class PixProvedorException(string mensagem, int? statusHttp = null, Exception? inner = null) : Exception(mensagem, inner)
{
    public int? StatusHttp { get; } = statusHttp;
}

/// <summary>Cobrança Pix dinâmica num banco/gateway, pra dar baixa automática: Banco do Brasil (API Pix do Banco Central) ou Asaas
/// (subconta da escola). Quem escolhe qual vale pra escola atual é o <see cref="IProvedorPixResolver"/>.</summary>
public interface IProvedorPix
{
    ProvedorPagamento Tipo { get; }

    bool Configurado { get; }

    /// <summary>O provedor precisa do pagador (CPF/CNPJ) pra criar a cobrança — sem ele, fica o Pix estático.</summary>
    bool ExigePagador { get; }

    Task<CobrancaPixCriada> CriarCobrancaAsync(DadosNovaCobrancaPix dados, CancellationToken ct = default);

    /// <summary>Nulo se o provedor não conhece essa cobrança (ou ela foi excluída).</summary>
    Task<CobrancaPixConsultada?> ConsultarCobrancaAsync(string idExterno, CancellationToken ct = default);

    Task RegistrarWebhookAsync(string chavePix, string urlWebhook, CancellationToken ct = default);

    /// <summary>Só obtém um token — prova que credenciais, certificado e endereços estão certos, sem criar nada.</summary>
    Task TestarConexaoAsync(CancellationToken ct = default);
}
