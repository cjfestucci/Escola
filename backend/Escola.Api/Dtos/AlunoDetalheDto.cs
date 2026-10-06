using Escola.Api.Servicos;
using Escola.Domain.Enums;

namespace Escola.Api.Dtos;

/// <param name="Convites">Só na resposta de criar/editar: o que aconteceu com o e-mail da matrícula de cada responsável.</param>
/// <param name="MatriculaPendente">Nenhum responsável aceitou o termo ainda — a matrícula não está efetivada.</param>
public record AlunoDetalheDto(
    Guid Id,
    string Nome,
    DateOnly DataNascimento,
    string? FotoUrl,
    Guid TurmaId,
    string TurmaNome,
    bool Ativo,
    IReadOnlyList<ResponsavelResumoDto> Responsaveis,
    IReadOnlyList<ConviteMatriculaDto> Convites,
    PosicaoAtleta? Posicao = null,
    bool Bloqueado = false,
    decimal DescontoMensalidadePercentual = 0,
    string? MotivoDesconto = null,
    bool MatriculaPendente = false,
    DateTime? MatriculaConfirmadaEm = null);
