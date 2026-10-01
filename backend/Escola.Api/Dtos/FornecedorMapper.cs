using Escola.Domain.Entities;

namespace Escola.Api.Dtos;

public static class FornecedorMapper
{
    public static FornecedorDto ToDto(this Fornecedor f) => new(
        f.Id,
        f.Nome,
        f.Documento,
        f.Telefone,
        f.Email,
        f.Ativo);
}
