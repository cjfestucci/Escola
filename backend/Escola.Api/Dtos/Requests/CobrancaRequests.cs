using Escola.Domain.Enums;

namespace Escola.Api.Dtos.Requests;

public record CriarCobrancaRequest(Guid AlunoId, string Descricao, decimal Valor, DateOnly Vencimento);

public record EditarCobrancaRequest(string Descricao, decimal Valor, DateOnly Vencimento);

public record EditarConfiguracaoFinanceiraRequest(
    string? PixChave,
    string? PixNomeRecebedor,
    string? PixCidade,
    TipoChavePix? PixTipoChave = null,
    int? DiasParaBloqueio = null,
    int? DiaVencimentoMensalidade = null,
    decimal? MultaAtrasoPercentual = null,
    decimal? JurosMensaisPercentual = null,
    bool? PagamentoPixAtivo = null,
    bool? PagamentoBoletoAtivo = null,
    bool? PagamentoPresencialAtivo = null,
    string? InstrucoesPagamentoPresencial = null);

/// <summary>Mensalidades de um mês, pra todas as turmas ou só uma. Usada tanto na pré-visualização quanto na geração.</summary>
public record GerarMensalidadesRequest(int Ano, int Mes, Guid? TurmaId = null);
