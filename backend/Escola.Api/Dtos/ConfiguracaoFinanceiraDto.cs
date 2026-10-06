using Escola.Domain.Enums;

namespace Escola.Api.Dtos;

/// <param name="Id">Nulo enquanto a configuração ainda não foi salva nenhuma vez (não há linha pra ancorar o histórico).</param>
/// <param name="Configurado">Só diz respeito ao Pix: chave, nome do recebedor e cidade preenchidos.</param>
public record ConfiguracaoFinanceiraDto(
    Guid? Id,
    string? PixChave,
    TipoChavePix? PixTipoChave,
    string? PixNomeRecebedor,
    string? PixCidade,
    int? DiasParaBloqueio,
    bool Configurado,
    int DiaVencimentoMensalidade = 10,
    decimal? MultaAtrasoPercentual = null,
    decimal? JurosMensaisPercentual = null);

/// <param name="Automatico">O código foi criado no banco: o pagamento é confirmado e baixado sozinho. Falso = Pix estático (baixa manual).</param>
public record PixCobrancaDto(string CodigoCopiaECola, bool Automatico = false);
