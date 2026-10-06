namespace Escola.Domain.Entities;

/// <summary>Configuração geral da escola — linha única. Guarda o fuso horário, que define
/// o que é "hoje" pra todo o app (corte de dia da rotina/diário, data de pagamento, cobrança atrasada).</summary>
public class ConfiguracaoEscola
{
    public Guid Id { get; set; }

    /// <summary>Identificador IANA (ex.: "America/Sao_Paulo").</summary>
    public string FusoHorario { get; set; } = "America/Sao_Paulo";

    /// <summary>Cor principal do tema, em "#RRGGBB" maiúsculo. Nulo = cor padrão do produto.</summary>
    public string? CorPrincipal { get; set; }

    /// <summary>Logo da empresa/clube (caminho público, ex.: "/uploads/xyz.png"), mostrada ao lado do nome do app. Só o Suporte define.
    /// Nulo = sem logo (volta o ícone padrão do segmento).</summary>
    public string? LogoUrl { get; set; }

    public DateTime AtualizadoEm { get; set; }
}
