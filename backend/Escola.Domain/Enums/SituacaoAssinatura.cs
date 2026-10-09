namespace Escola.Domain.Enums;

/// <summary>Situação da assinatura do cliente com a plataforma. Gravada como inteiro: <b>não reordenar</b>.
/// Sempre recalculada a partir das faturas do gateway e das datas — nunca editada à mão.</summary>
public enum SituacaoAssinatura
{
    /// <summary>Sem teste grátis: o cliente foi cadastrado, mas a 1ª fatura ainda não foi paga. Ninguém entra.</summary>
    AguardandoPagamento = 0,
    /// <summary>Dentro do teste grátis (ou com a 1ª fatura ainda dentro do prazo).</summary>
    EmTeste = 1,
    Ativa = 2,
    /// <summary>Fatura vencida, ainda dentro da tolerância: tudo funciona, com aviso.</summary>
    EmAtraso = 3,
    /// <summary>Fatura vencida além da tolerância: só o Admin entra, e só na tela da assinatura.</summary>
    Suspensa = 4,
    Cancelada = 5
}
