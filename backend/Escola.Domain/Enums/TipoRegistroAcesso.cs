namespace Escola.Domain.Enums;

/// <summary>Gravado como inteiro: <b>não reordenar</b>.</summary>
public enum TipoRegistroAcesso
{
    /// <summary>Entrou com e-mail e senha (e código, no Suporte).</summary>
    Login = 0,
    /// <summary>Primeiro uso do dia com um login já aberto (o token dura dias: sem isto, só o dia do login ficaria registrado).</summary>
    UsoDiario = 1,
    /// <summary>Criou ou trocou a senha pelo link do e-mail (convite ou "esqueci a senha").</summary>
    SenhaPeloLink = 2
}
