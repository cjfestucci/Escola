namespace Escola.Api.Dtos;

public enum SituacaoMensalidade
{
    /// <summary>Será criada.</summary>
    Gerar,
    /// <summary>O aluno já tem a mensalidade desse mês (não duplica).</summary>
    JaExiste,
    /// <summary>A turma não tem valor de mensalidade definido.</summary>
    SemValor,
    /// <summary>Desconto de 100% (bolsa integral): não gera cobrança.</summary>
    Isento,
    /// <summary>A matrícula ainda não foi confirmada pelo responsável (termo não aceito): não gera cobrança.</summary>
    MatriculaPendente
}

public record MensalidadeItemDto(
    Guid AlunoId,
    string AlunoNome,
    string TurmaNome,
    decimal ValorBase,
    decimal DescontoPercentual,
    decimal Valor,
    SituacaoMensalidade Situacao);

public record PreviaMensalidadesDto(
    int Ano,
    int Mes,
    string Descricao,
    DateOnly Vencimento,
    IReadOnlyList<MensalidadeItemDto> Itens,
    int AGerar,
    int JaExistem,
    int SemValor,
    int Isentos,
    decimal TotalAGerar,
    int MatriculasPendentes = 0);

public record GeracaoMensalidadesDto(int Geradas, int JaExistiam, int SemValor, int Isentos, decimal Total);
