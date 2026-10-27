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
    decimal? JurosMensaisPercentual = null,
    bool PagamentoPixAtivo = true,
    bool PagamentoBoletoAtivo = false,
    bool PagamentoPresencialAtivo = false,
    string? InstrucoesPagamentoPresencial = null);

/// <summary>O que a família (e a equipe) podem oferecer na hora de pagar. Sem dado sensível: não inclui a chave Pix.</summary>
public record FormasPagamentoDto(bool Pix, bool Boleto, bool Presencial, string? InstrucoesPresencial);

/// <param name="Automatico">O código foi criado no banco: o pagamento é confirmado e baixado sozinho. Falso = Pix estático (baixa manual).</param>
/// <param name="ProcessadoPeloAsaas">O Pix sai da subconta do Asaas: a tela e o e-mail precisam identificar o Asaas como prestador (Playbook de BaaS).</param>
public record PixCobrancaDto(string CodigoCopiaECola, bool Automatico = false, bool ProcessadoPeloAsaas = false);
