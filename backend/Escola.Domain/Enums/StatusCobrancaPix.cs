namespace Escola.Domain.Enums;

public enum StatusCobrancaPix
{
    /// <summary>Gerada no banco e ainda aguardando pagamento.</summary>
    Ativa = 0,
    /// <summary>O banco confirmou o pagamento.</summary>
    Concluida = 1,
    /// <summary>Expirou, foi removida ou substituída — não vale mais pra pagar nem é mais consultada.</summary>
    Encerrada = 2
}
