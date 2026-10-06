using Escola.Domain.Enums;

namespace Escola.Infrastructure.Frequencia;

/// <summary>Regras de frequência, sempre <b>derivadas</b> dos registros de presença (nada de percentual gravado).</summary>
public static class FrequenciaCalculo
{
    /// <summary>A partir de quantas faltas seguidas o atleta/aluno entra no alerta de faltoso.</summary>
    public const int FaltasSeguidasParaAlerta = 3;

    /// <summary>Janela padrão (dias corridos) do percentual de frequência.</summary>
    public const int JanelaPadraoDias = 30;

    public readonly record struct Resumo(int Presencas, int Faltas, int Justificadas, decimal? Percentual);

    /// <summary>Frequência = presenças ÷ (presenças + faltas). Falta <b>justificada não entra na conta</b> (não pesa contra
    /// nem a favor). Sem nenhuma presença/falta registrada, o percentual é nulo (não dá pra dizer "0%").</summary>
    public static Resumo Resumir(IEnumerable<StatusPresenca> statuses)
    {
        int presencas = 0, faltas = 0, justificadas = 0;
        foreach (var s in statuses)
        {
            switch (s)
            {
                case StatusPresenca.Presente: presencas++; break;
                case StatusPresenca.Falta: faltas++; break;
                case StatusPresenca.Justificada: justificadas++; break;
            }
        }

        var base_ = presencas + faltas;
        decimal? percentual = base_ == 0 ? null : Math.Round(presencas * 100m / base_, 0, MidpointRounding.AwayFromZero);
        return new Resumo(presencas, faltas, justificadas, percentual);
    }

    /// <summary>Quantas chamadas mais recentes seguidas ele faltou. Percorre do dia mais recente pro mais antigo: <c>Falta</c>
    /// soma, <c>Justificada</c> é ignorada (nem soma nem zera) e a primeira <c>Presente</c> encerra a contagem.</summary>
    public static int FaltasSeguidas(IEnumerable<(DateOnly Data, StatusPresenca Status)> registros)
    {
        var seguidas = 0;
        foreach (var (_, status) in registros.OrderByDescending(r => r.Data))
        {
            if (status == StatusPresenca.Presente) break;
            if (status == StatusPresenca.Falta) seguidas++;
        }

        return seguidas;
    }
}
