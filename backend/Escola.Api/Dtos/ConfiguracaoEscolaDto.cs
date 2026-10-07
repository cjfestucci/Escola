using Escola.Domain.Enums;

namespace Escola.Api.Dtos;

/// <summary>`Segmento` é só leitura aqui (vem do cliente). `NomeEscola` é o nome do cliente (nulo sem login: a tela de login é de todos).</summary>
public record ConfiguracaoEscolaDto(Guid? Id, string FusoHorario, string? CorPrincipal, SegmentoCliente Segmento, string? LogoUrl = null, string? NomeEscola = null);
