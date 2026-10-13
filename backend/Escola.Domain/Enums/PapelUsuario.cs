namespace Escola.Domain.Enums;

public enum PapelUsuario
{
    Admin,
    Educador,
    Coordenador,
    Financeiro,
    Responsavel,
    /// <summary>Conta da equipe do produto (não do cliente): só configura o ambiente. Nunca aparece nas listas do cliente e não é
    /// criada pela tela de Usuários — vem da configuração do deploy (<c>Suporte:*</c>). <b>Manter por último</b> (valor 5): o enum é gravado como inteiro.</summary>
    Suporte,
    /// <summary>Recepção/secretaria do clube (desde 2026-10-13): matrícula, recebimento de mensalidade no balcão e estoque — sem
    /// configurações, contas da equipe, contas a pagar/receber, desconto nem cancelamento de cobrança. Valor 6 (depois do Suporte):
    /// <b>não reordenar</b>.</summary>
    Secretaria
}
