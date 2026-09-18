using Escola.Domain.Entities;

namespace Escola.Api.Dtos;

public static class RegistroRotinaMapper
{
    public static RegistroRotinaDto ToDto(this RegistroRotina registro) => registro switch
    {
        RegistroAlimentacao r => Base(r) with
        {
            Categoria = "Alimentacao",
            Refeicao = r.Refeicao.ToString(),
            StatusAlimentacao = r.Status.ToString()
        },
        RegistroSono r => Base(r) with
        {
            Categoria = "Sono",
            HoraInicio = r.HoraInicio,
            HoraFim = r.HoraFim
        },
        RegistroHigiene r => Base(r) with
        {
            Categoria = "Higiene",
            TipoHigiene = r.Tipo.ToString()
        },
        RegistroHumor r => Base(r) with
        {
            Categoria = "Humor",
            Humor = r.Humor.ToString()
        },
        RegistroMomento r => Base(r) with { Categoria = "Momento" },
        _ => throw new NotSupportedException($"Categoria de registro não mapeada: {registro.GetType().Name}")
    };

    private static RegistroRotinaDto Base(RegistroRotina r) => new(
        r.Id,
        r.AlunoId,
        Categoria: string.Empty,
        r.RegistradoEm,
        r.Observacao,
        r.FotoUrl,
        CriadoPorNome: r.CriadoPor?.Nome ?? string.Empty,
        Refeicao: null,
        StatusAlimentacao: null,
        HoraInicio: null,
        HoraFim: null,
        TipoHigiene: null,
        Humor: null);
}
