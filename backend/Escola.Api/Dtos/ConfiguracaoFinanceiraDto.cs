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
    bool Configurado);

public record PixCobrancaDto(string CodigoCopiaECola);
