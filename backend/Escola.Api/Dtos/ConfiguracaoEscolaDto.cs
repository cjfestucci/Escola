using Escola.Domain.Enums;

namespace Escola.Api.Dtos;

/// <summary>`Segmento` é só leitura aqui (vem do cliente, não da configuração editável).</summary>
public record ConfiguracaoEscolaDto(Guid? Id, string FusoHorario, string? CorPrincipal, SegmentoCliente Segmento, string? LogoUrl = null);
