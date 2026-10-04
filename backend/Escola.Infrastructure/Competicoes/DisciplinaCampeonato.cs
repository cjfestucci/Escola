using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Escola.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Escola.Infrastructure.Competicoes;

public enum TipoAlertaDisciplinar
{
    Suspenso,
    Pendurado
}

public record AlertaDisciplinar(Guid AlunoId, string AlunoNome, TipoAlertaDisciplinar Tipo, string Detalhe, int AmarelosAcumulados);

public interface IDisciplinaService
{
    Task<DisciplinaCampeonato> CarregarAsync(Guid campeonatoId);
}

public class DisciplinaService(EscolaDbContext db) : IDisciplinaService
{
    public async Task<DisciplinaCampeonato> CarregarAsync(Guid campeonatoId)
    {
        var campeonato = await db.Campeonatos.Where(c => c.Id == campeonatoId).Select(c => new { c.AmarelosParaSuspensao, c.Ativo }).FirstOrDefaultAsync();

        // Campeonato desativado (ex.: já encerrado) não gera alerta — senão uma suspensão do último jogo ficaria
        // "pendente pro próximo jogo" pra sempre, sem nenhum jogo futuro pra cumpri-la.
        if (campeonato is not { AmarelosParaSuspensao: { } limite, Ativo: true }) return DisciplinaCampeonato.SemControle;

        var jogos = (await db.Jogos
                .Include(j => j.Turma)
                .Where(j => j.CampeonatoId == campeonatoId && j.Status != StatusJogo.Cancelado)
                .ToListAsync())
            .OrderBy(j => j.Data).ThenBy(j => j.Hora).ThenBy(j => j.RegistradoEm)
            .ToList();

        var participacoes = await db.JogoAtletas
            .Include(ja => ja.Aluno)
            .Where(ja => ja.Jogo.CampeonatoId == campeonatoId && ja.Jogo.Status == StatusJogo.Realizado)
            .ToListAsync();

        return new DisciplinaCampeonato(limite, jogos, participacoes);
    }
}

/// <summary>Situação disciplinar derivada, nunca gravada: percorre os jogos do campeonato em ordem cronológica e, pra cada
/// atleta, acumula amarelos e dispara suspensões. Regras: cartão vermelho = 1 jogo de suspensão; atingir o limite de amarelos
/// = 1 jogo de suspensão e o acúmulo zera; um 2º amarelo que vira vermelho conta como 1 amarelo (+ o vermelho). A suspensão vale
/// pro PRÓXIMO jogo (não cancelado) da MESMA turma em que o cartão foi tomado, no mesmo campeonato.</summary>
public class DisciplinaCampeonato
{
    public static readonly DisciplinaCampeonato SemControle = new();

    private readonly int _limite;
    private readonly List<Jogo> _jogos = [];
    private readonly Dictionary<Guid, int> _ordem = [];
    private readonly Dictionary<Guid, List<JogoAtleta>> _porAtleta = [];

    private DisciplinaCampeonato() { }

    public DisciplinaCampeonato(int limite, List<Jogo> jogosEmOrdem, List<JogoAtleta> participacoesRealizadas)
    {
        _limite = limite;
        _jogos = jogosEmOrdem;
        for (var i = 0; i < _jogos.Count; i++) _ordem[_jogos[i].Id] = i;

        foreach (var grupo in participacoesRealizadas.Where(p => _ordem.ContainsKey(p.JogoId)).GroupBy(p => p.AlunoId))
            _porAtleta[grupo.Key] = grupo.OrderBy(p => _ordem[p.JogoId]).ToList();
    }

    private bool Vazio => _limite == 0;

    /// <summary>Alertas pra um jogo (suspensos nele e pendurados antes dele), considerando só o que aconteceu ANTES dele.</summary>
    public IReadOnlyList<AlertaDisciplinar> ParaJogo(Guid jogoId)
    {
        if (Vazio || !_ordem.TryGetValue(jogoId, out var posicao)) return [];
        var jogo = _jogos[posicao];
        var alertas = new List<AlertaDisciplinar>();

        foreach (var (alunoId, registros) in _porAtleta)
        {
            var aluno = registros[0].Aluno;
            if (!aluno.Ativo) continue;

            var (amarelos, gatilhos) = Percorrer(registros, posicao);

            var motivos = gatilhos.Where(g => ProximoJogo(g.Jogo)?.Id == jogoId).Select(g => Descrever(g)).ToList();
            if (motivos.Count > 0)
            {
                alertas.Add(new AlertaDisciplinar(alunoId, aluno.Nome, TipoAlertaDisciplinar.Suspenso, $"Motivo: {string.Join("; ", motivos)}", amarelos));
                continue;
            }

            // Pendurado só interessa ao time que vai jogar: atleta da turma do jogo ou já convocado nele.
            var relevante = aluno.TurmaId == jogo.TurmaId || registros.Any(r => r.JogoId == jogoId);
            if (relevante && _limite >= 2 && amarelos == _limite - 1)
                alertas.Add(new AlertaDisciplinar(alunoId, aluno.Nome, TipoAlertaDisciplinar.Pendurado, DescreverPendurado(amarelos), amarelos));
        }

        return alertas.OrderBy(a => a.Tipo).ThenBy(a => a.AlunoNome).ToList();
    }

    /// <summary>Situação de hoje (depois de todos os jogos realizados): suspensões ainda não cumpridas e pendurados.</summary>
    public IReadOnlyList<AlertaDisciplinar> Atual()
    {
        if (Vazio) return [];
        var alertas = new List<AlertaDisciplinar>();

        foreach (var (alunoId, registros) in _porAtleta)
        {
            var aluno = registros[0].Aluno;
            if (!aluno.Ativo) continue;

            var (amarelos, gatilhos) = Percorrer(registros, _jogos.Count);

            var pendentes = new List<string>();
            foreach (var gatilho in gatilhos)
            {
                var proximo = ProximoJogo(gatilho.Jogo);
                if (proximo is { Status: StatusJogo.Realizado }) continue; // já cumpriu

                var onde = proximo is null
                    ? "no próximo jogo (ainda não agendado)"
                    : $"no jogo contra {proximo.Adversario} em {proximo.Data:dd/MM/yyyy}";
                pendentes.Add($"{Descrever(gatilho)} → suspenso {onde}");
            }

            if (pendentes.Count > 0)
                alertas.Add(new AlertaDisciplinar(alunoId, aluno.Nome, TipoAlertaDisciplinar.Suspenso, string.Join("; ", pendentes), amarelos));
            else if (_limite >= 2 && amarelos == _limite - 1)
                alertas.Add(new AlertaDisciplinar(alunoId, aluno.Nome, TipoAlertaDisciplinar.Pendurado, DescreverPendurado(amarelos), amarelos));
        }

        return alertas.OrderBy(a => a.Tipo).ThenBy(a => a.AlunoNome).ToList();
    }

    private record Gatilho(Jogo Jogo, string Motivo);

    private (int Amarelos, List<Gatilho> Gatilhos) Percorrer(List<JogoAtleta> registros, int ateAntesDe)
    {
        var amarelos = 0;
        var gatilhos = new List<Gatilho>();

        foreach (var r in registros.Where(r => _ordem[r.JogoId] < ateAntesDe))
        {
            var jogo = _jogos[_ordem[r.JogoId]];

            if (r.CartaoVermelho) gatilhos.Add(new Gatilho(jogo, "cartão vermelho"));

            // 2º amarelo que vira vermelho: conta só o primeiro amarelo (o vermelho já gerou a suspensão acima).
            amarelos += r.CartoesAmarelos == 2 && r.CartaoVermelho ? 1 : r.CartoesAmarelos;

            if (amarelos >= _limite)
            {
                gatilhos.Add(new Gatilho(jogo, $"{_limite} amarelos acumulados"));
                amarelos = 0;
            }
        }

        return (amarelos, gatilhos);
    }

    private Jogo? ProximoJogo(Jogo gatilho)
    {
        for (var i = _ordem[gatilho.Id] + 1; i < _jogos.Count; i++)
            if (_jogos[i].TurmaId == gatilho.TurmaId) return _jogos[i];
        return null;
    }

    private static string Descrever(Gatilho g) => $"{g.Motivo} contra {g.Jogo.Adversario} ({g.Jogo.Data:dd/MM})";

    private string DescreverPendurado(int amarelos) =>
        $"Pendurado: {amarelos} de {_limite} amarelos — mais 1 gera suspensão";
}
