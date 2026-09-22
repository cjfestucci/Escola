namespace Escola.Api.Auth;

/// <summary>Grupos de papéis reutilizados em [Authorize(Roles = ...)] pelos controllers.</summary>
public static class GruposDePapeis
{
    public const string Equipe = "Admin,Coordenador,Educador,Financeiro";
    public const string Gestao = "Admin,Coordenador";
    public const string Financeiro = "Admin,Coordenador,Financeiro";
    public const string Responsavel = "Responsavel";
}
