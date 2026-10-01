namespace Escola.Api.Dtos;

public record FichaSaudeDto(
    Guid? Id,
    string? TipoSanguineo,
    string? Alergias,
    string? RestricoesAlimentares,
    string? MedicamentosEmUso,
    string? CondicoesSaude,
    string? PlanoSaude,
    string? PediatraNome,
    string? PediatraTelefone,
    string? ContatoEmergenciaNome,
    string? ContatoEmergenciaTelefone,
    bool VacinacaoEmDia,
    bool AutorizaUsoImagem,
    DateTime? AtualizadoEm);
