using Escola.Domain.Entities;

namespace Escola.Api.Dtos;

public static class UnidadeMapper
{
    public static UnidadeDto ToDto(this Unidade unidade) => new(
        unidade.Id,
        unidade.Nome,
        unidade.Endereco,
        unidade.Telefone,
        unidade.Ativa);
}
