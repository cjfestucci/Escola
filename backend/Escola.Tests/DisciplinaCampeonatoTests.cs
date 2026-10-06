using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Escola.Infrastructure.Competicoes;

namespace Escola.Tests;

/// <summary>Suspensos e pendurados são derivados dos cartões da súmula (nunca gravados) — as regras ficam aqui.</summary>
public class DisciplinaCampeonatoTests
{
    private static readonly Guid TurmaA = Guid.NewGuid();
    private static readonly Guid TurmaB = Guid.NewGuid();

    private static Aluno Atleta(string nome, Guid turma, bool ativo = true) =>
        new() { Id = Guid.NewGuid(), Nome = nome, TurmaId = turma, Ativo = ativo };

    private static Jogo NovoJogo(Guid turma, int dia, StatusJogo status = StatusJogo.Realizado) => new()
    {
        Id = Guid.NewGuid(), TurmaId = turma, Adversario = $"Time {dia}", Data = new DateOnly(2026, 10, dia),
        Hora = new TimeOnly(10, 0), Status = status
    };

    private static JogoAtleta Cartao(Jogo jogo, Aluno aluno, int amarelos = 0, bool vermelho = false) => new()
    {
        Id = Guid.NewGuid(), JogoId = jogo.Id, AlunoId = aluno.Id, Aluno = aluno, CartoesAmarelos = amarelos, CartaoVermelho = vermelho
    };

    [Fact]
    public void VermelhoSuspendeNoProximoJogoDaMesmaTurma()
    {
        var fulano = Atleta("Fulano", TurmaA);
        var j1 = NovoJogo(TurmaA, 1);
        var jOutraTurma = NovoJogo(TurmaB, 2); // não conta: é de outra turma
        var j3 = NovoJogo(TurmaA, 3, StatusJogo.Agendado);
        var d = new DisciplinaCampeonato(3, [j1, jOutraTurma, j3], [Cartao(j1, fulano, vermelho: true)]);

        Assert.Empty(d.ParaJogo(j1.Id));
        Assert.Empty(d.ParaJogo(jOutraTurma.Id));
        var alerta = Assert.Single(d.ParaJogo(j3.Id));
        Assert.Equal(TipoAlertaDisciplinar.Suspenso, alerta.Tipo);
        Assert.Equal("Fulano", alerta.AlunoNome);
    }

    [Fact]
    public void TresAmarelosAcumuladosSuspendemEZeramOAcumulo()
    {
        var f = Atleta("Fulano", TurmaA);
        var j1 = NovoJogo(TurmaA, 1); var j2 = NovoJogo(TurmaA, 2); var j3 = NovoJogo(TurmaA, 3);
        var j4 = NovoJogo(TurmaA, 4, StatusJogo.Agendado); var j5 = NovoJogo(TurmaA, 5, StatusJogo.Agendado);
        var d = new DisciplinaCampeonato(3, [j1, j2, j3, j4, j5], [Cartao(j1, f, 1), Cartao(j2, f, 1), Cartao(j3, f, 1)]);

        Assert.Equal(TipoAlertaDisciplinar.Suspenso, Assert.Single(d.ParaJogo(j4.Id)).Tipo);
        Assert.Empty(d.ParaJogo(j5.Id)); // acumulo zerou: nem suspenso nem pendurado
    }

    [Fact]
    public void ADoisAmarelosDoLimiteFicaPendurado()
    {
        var f = Atleta("Fulano", TurmaA);
        var j1 = NovoJogo(TurmaA, 1); var j2 = NovoJogo(TurmaA, 2); var j3 = NovoJogo(TurmaA, 3, StatusJogo.Agendado);
        var d = new DisciplinaCampeonato(3, [j1, j2, j3], [Cartao(j1, f, 1), Cartao(j2, f, 1)]);

        var alerta = Assert.Single(d.ParaJogo(j3.Id));
        Assert.Equal(TipoAlertaDisciplinar.Pendurado, alerta.Tipo);
        Assert.Equal(2, alerta.AmarelosAcumulados);
    }

    [Fact]
    public void SegundoAmareloQueViraVermelhoContaComoUmAmareloMaisAsuspensao()
    {
        var f = Atleta("Fulano", TurmaA);
        var j1 = NovoJogo(TurmaA, 1); var j2 = NovoJogo(TurmaA, 2, StatusJogo.Agendado); var j3 = NovoJogo(TurmaA, 3, StatusJogo.Agendado);
        var d = new DisciplinaCampeonato(3, [j1, j2, j3], [Cartao(j1, f, amarelos: 2, vermelho: true)]);

        Assert.Equal(TipoAlertaDisciplinar.Suspenso, Assert.Single(d.ParaJogo(j2.Id)).Tipo);
        // sobrou 1 amarelo (não 2): com limite 3 ainda não está pendurado
        Assert.Empty(d.ParaJogo(j3.Id));
    }

    [Fact]
    public void LimiteUmNuncaGeraPendurado()
    {
        var f = Atleta("Fulano", TurmaA);
        var j1 = NovoJogo(TurmaA, 1); var j2 = NovoJogo(TurmaA, 2, StatusJogo.Agendado);
        var d = new DisciplinaCampeonato(1, [j1, j2], []);
        Assert.Empty(d.ParaJogo(j2.Id));
        Assert.Empty(d.Atual());
    }

    [Fact]
    public void AtletaInativoNaoGeraAlerta()
    {
        var f = Atleta("Fulano", TurmaA, ativo: false);
        var j1 = NovoJogo(TurmaA, 1); var j2 = NovoJogo(TurmaA, 2, StatusJogo.Agendado);
        var d = new DisciplinaCampeonato(3, [j1, j2], [Cartao(j1, f, vermelho: true)]);
        Assert.Empty(d.ParaJogo(j2.Id));
        Assert.Empty(d.Atual());
    }

    [Fact]
    public void SuspensaoCumpridaSaiDaSituacaoAtual()
    {
        var f = Atleta("Fulano", TurmaA);
        var j1 = NovoJogo(TurmaA, 1); var j2 = NovoJogo(TurmaA, 2); // j2 já realizado: cumpriu
        var d = new DisciplinaCampeonato(3, [j1, j2], [Cartao(j1, f, vermelho: true)]);
        Assert.Empty(d.Atual());
    }

    [Fact]
    public void SuspensaoSemProximoJogoAgendadoApareceComoPendente()
    {
        var f = Atleta("Fulano", TurmaA);
        var j1 = NovoJogo(TurmaA, 1);
        var d = new DisciplinaCampeonato(3, [j1], [Cartao(j1, f, vermelho: true)]);

        var alerta = Assert.Single(d.Atual());
        Assert.Equal(TipoAlertaDisciplinar.Suspenso, alerta.Tipo);
        Assert.Contains("ainda não agendado", alerta.Detalhe);
    }

    [Fact]
    public void SemControleNaoGeraNada()
    {
        var j = NovoJogo(TurmaA, 1);
        Assert.Empty(DisciplinaCampeonato.SemControle.ParaJogo(j.Id));
        Assert.Empty(DisciplinaCampeonato.SemControle.Atual());
    }
}
