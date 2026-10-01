using Escola.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Escola.Infrastructure.Tempo;

/// <summary>"Hoje" e "início do dia" sempre no fuso da escola (<see cref="Domain.Entities.ConfiguracaoEscola"/>),
/// nunca em UTC — senão, no Brasil, o dia vira às 21h (UTC-3). Timestamps continuam gravados em UTC;
/// só o corte de dia depende do fuso.</summary>
public interface IRelogioEscola
{
    Task<DateOnly> HojeAsync();

    /// <summary>Intervalo [início, fim) em UTC correspondente ao dia inteiro no fuso da escola —
    /// pra filtrar colunas `timestamptz` por dia.</summary>
    Task<(DateTime InicioUtc, DateTime FimUtc)> IntervaloUtcDoDiaAsync(DateOnly dia);

    /// <summary>Caminho inverso de <see cref="IntervaloUtcDoDiaAsync"/>: dado um instante UTC qualquer
    /// (ex.: `RegistradoEm` de um registro), devolve o dia correspondente no fuso da escola.</summary>
    Task<DateOnly> DataLocalAsync(DateTime instanteUtc);
}

public class RelogioEscola(EscolaDbContext db) : IRelogioEscola
{
    public const string FusoPadrao = "America/Sao_Paulo";

    private TimeZoneInfo? fuso;

    public async Task<DateOnly> HojeAsync() =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, await FusoAsync()));

    public async Task<(DateTime InicioUtc, DateTime FimUtc)> IntervaloUtcDoDiaAsync(DateOnly dia)
    {
        var tz = await FusoAsync();
        return (InicioDoDiaUtc(dia, tz), InicioDoDiaUtc(dia.AddDays(1), tz));
    }

    public async Task<DateOnly> DataLocalAsync(DateTime instanteUtc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(instanteUtc, DateTimeKind.Utc), await FusoAsync()));

    public static bool FusoValido(string? id) => !string.IsNullOrWhiteSpace(id) && TimeZoneInfo.TryFindSystemTimeZoneById(id, out _);

    private async Task<TimeZoneInfo> FusoAsync()
    {
        if (fuso is not null) return fuso;
        var id = await db.ConfiguracoesEscola.Select(c => c.FusoHorario).FirstOrDefaultAsync();
        fuso = TimeZoneInfo.TryFindSystemTimeZoneById(id ?? FusoPadrao, out var tz) ? tz : TimeZoneInfo.FindSystemTimeZoneById(FusoPadrao);
        return fuso;
    }

    /// <summary>Em fusos com horário de verão que começa à meia-noite (o Brasil até 2019), 00:00 não existe
    /// no dia da virada — nesse caso o dia começa na primeira hora válida.</summary>
    private static DateTime InicioDoDiaUtc(DateOnly dia, TimeZoneInfo tz)
    {
        var local = dia.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        while (tz.IsInvalidTime(local)) local = local.AddMinutes(30);
        return TimeZoneInfo.ConvertTimeToUtc(local, tz);
    }
}
