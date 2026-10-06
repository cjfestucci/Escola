namespace Escola.Api.Auth;

/// <summary>Grupos de papéis reutilizados em [Authorize(Roles = ...)] pelos controllers.</summary>
public static class GruposDePapeis
{
    public const string Equipe = "Admin,Coordenador,Educador,Financeiro";
    public const string Gestao = "Admin,Coordenador";
    public const string Financeiro = "Admin,Coordenador,Financeiro";
    public const string Responsavel = "Responsavel";

    /// <summary>Equipe do produto. <b>Fora</b> de Equipe/Gestao/Financeiro de propósito: o suporte configura o ambiente, mas não vê
    /// dado de aluno, saúde nem financeiro — cada endpoint de configuração o libera explicitamente com um dos grupos abaixo.</summary>
    public const string Suporte = "Suporte";
    public const string GestaoOuSuporte = "Admin,Coordenador,Suporte";
    public const string FinanceiroOuSuporte = "Admin,Coordenador,Financeiro,Suporte";
    public const string EquipeOuSuporte = "Admin,Coordenador,Educador,Financeiro,Suporte";
}
