namespace Escola.Api.Dtos.Requests;

public record SalvarFichaSaudeRequest(
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
    DateOnly? AtestadoValidoAte = null);
