using Escola.Api.Servicos;
using Escola.Domain.Entities;

namespace Escola.Api.Dtos;

public static class RegistroDiarioClasseMapper
{
    public static RegistroDiarioClasseDto ToDto(this RegistroDiarioClasse r) => new(
        r.Id,
        r.TurmaId,
        r.Titulo,
        r.Descricao,
        r.Fotos.OrderBy(f => f.Ordem).Select(f => UrlArquivo.Assinar(f.Url)!).ToList(),
        r.RegistradoEm,
        r.CriadoPor?.Nome ?? string.Empty);
}
