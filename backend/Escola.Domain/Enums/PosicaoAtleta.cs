namespace Escola.Domain.Enums;

public enum PosicaoAtleta
{
    Goleiro,
    Zagueiro,
    LateralDireito,
    LateralEsquerdo,
    Volante,
    MeioCampo,
    MeiaAtacante,
    PontaDireita,
    PontaEsquerda,
    Centroavante
}

public static class PosicaoAtletaExtensions
{
    public static string Rotulo(this PosicaoAtleta posicao) => posicao switch
    {
        PosicaoAtleta.Goleiro => "Goleiro",
        PosicaoAtleta.Zagueiro => "Zagueiro",
        PosicaoAtleta.LateralDireito => "Lateral direito",
        PosicaoAtleta.LateralEsquerdo => "Lateral esquerdo",
        PosicaoAtleta.Volante => "Volante",
        PosicaoAtleta.MeioCampo => "Meio-campo",
        PosicaoAtleta.MeiaAtacante => "Meia-atacante",
        PosicaoAtleta.PontaDireita => "Ponta direita",
        PosicaoAtleta.PontaEsquerda => "Ponta esquerda",
        PosicaoAtleta.Centroavante => "Centroavante",
        _ => posicao.ToString()
    };
}
