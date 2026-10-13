namespace Escola.Api.Auth;

/// <summary>Grupos de papéis reutilizados em [Authorize(Roles = ...)] pelos controllers.</summary>
public static class GruposDePapeis
{
    /// <summary>Toda a equipe da escola. Ver é liberado pra todos; gravar coisas ligadas a uma turma (chamada, jogo, rotina, diário) passa
    /// ainda por <see cref="ControllerBaseExtensions.PodeLancarNaTurmaAsync"/> (professor só nas dele; Secretaria em nenhuma).</summary>
    public const string Equipe = "Admin,Coordenador,Educador,Financeiro,Secretaria";
    public const string Gestao = "Admin,Coordenador";
    public const string Financeiro = "Admin,Coordenador,Financeiro";
    public const string Responsavel = "Responsavel";

    /// <summary>Cadastro de atletas (matrícula, responsáveis, ficha de saúde, documentos): Gestão + Secretaria.</summary>
    public const string Cadastro = "Admin,Coordenador,Secretaria";
    /// <summary>Balcão: listar mensalidades, lançar cobrança avulsa, marcar como paga, Pix/e-mail e estoque — Financeiro + Secretaria.
    /// Editar valor, desconto, cancelar/reabrir, desmarcar pagamento e gerar em lote continuam só no <see cref="Financeiro"/>.</summary>
    public const string Balcao = "Admin,Coordenador,Financeiro,Secretaria";

    /// <summary>Equipe do produto. <b>Fora</b> de Equipe/Gestao/Financeiro de propósito: o suporte configura o ambiente, mas não vê
    /// dado de aluno, saúde nem financeiro — cada endpoint de configuração o libera explicitamente com um dos grupos abaixo.</summary>
    public const string Suporte = "Suporte";
    /// <summary>Identidade da escola (nome, logo, fuso): o dono da conta — o Admin — e o Suporte. Nem o Coordenador.</summary>
    public const string AdminOuSuporte = "Admin,Suporte";
    public const string GestaoOuSuporte = "Admin,Coordenador,Suporte";
    public const string FinanceiroOuSuporte = "Admin,Coordenador,Financeiro,Suporte";
    public const string EquipeOuSuporte = "Admin,Coordenador,Educador,Financeiro,Secretaria,Suporte";
}
