namespace Escola.Domain.Enums;

/// <summary>Mando de campo do jogo, do ponto de vista do nosso time.</summary>
public enum LocalJogo
{
    Casa,
    Fora,
    Neutro
}

public static class LocalJogoExtensions
{
    public static string Rotulo(this LocalJogo local) => local switch
    {
        LocalJogo.Casa => "Casa",
        LocalJogo.Fora => "Fora",
        LocalJogo.Neutro => "Campo neutro",
        _ => local.ToString()
    };
}
