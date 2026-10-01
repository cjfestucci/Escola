using Escola.Domain.Entities;

namespace Escola.Api.Dtos;

public static class FichaSaudeMapper
{
    public static FichaSaudeDto ToDto(this FichaSaude ficha) => new(
        ficha.Id,
        ficha.TipoSanguineo,
        ficha.Alergias,
        ficha.RestricoesAlimentares,
        ficha.MedicamentosEmUso,
        ficha.CondicoesSaude,
        ficha.PlanoSaude,
        ficha.PediatraNome,
        ficha.PediatraTelefone,
        ficha.ContatoEmergenciaNome,
        ficha.ContatoEmergenciaTelefone,
        ficha.VacinacaoEmDia,
        ficha.AutorizaUsoImagem,
        ficha.AtualizadoEm);

    public static readonly FichaSaudeDto Vazia = new(
        null, null, null, null, null, null, null, null, null, null, null, false, false, null);
}
