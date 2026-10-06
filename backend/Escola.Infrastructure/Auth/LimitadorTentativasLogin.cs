using System.Collections.Concurrent;

namespace Escola.Infrastructure.Auth;

/// <summary>Freio de tentativas de login por conta: depois de N falhas (senha ou código do segundo fator) numa janela de 15 minutos,
/// a conta fica bloqueada até as falhas "esfriarem". Também guarda o último passo TOTP aceito de cada conta (anti-reuso do código).
/// <b>Em memória</b>: vale pra uma instância só — com várias instâncias do app atrás de um balanceador, cada uma conta as suas
/// (o freio fica mais frouxo, não deixa de existir) e reiniciar o app zera os contadores. Singleton.</summary>
public sealed class LimitadorTentativasLogin
{
    public static readonly TimeSpan Janela = TimeSpan.FromMinutes(15);

    private readonly ConcurrentDictionary<string, List<DateTime>> _falhas = new();
    private readonly ConcurrentDictionary<string, long> _ultimoPasso = new();

    public bool EstaBloqueado(string chave, int limite, out TimeSpan restante)
    {
        restante = TimeSpan.Zero;
        if (!_falhas.TryGetValue(chave, out var lista)) return false;

        lock (lista)
        {
            Podar(lista);
            if (lista.Count < limite) return false;

            // Libera quando a falha mais antiga que ainda conta sair da janela.
            restante = lista[lista.Count - limite] + Janela - DateTime.UtcNow;
            return restante > TimeSpan.Zero;
        }
    }

    public void RegistrarFalha(string chave)
    {
        var lista = _falhas.GetOrAdd(chave, _ => []);
        lock (lista)
        {
            Podar(lista);
            lista.Add(DateTime.UtcNow);
        }
    }

    public void Limpar(string chave) => _falhas.TryRemove(chave, out _);

    public long? UltimoPasso(string chave) => _ultimoPasso.TryGetValue(chave, out var passo) ? passo : null;

    public void RegistrarPasso(string chave, long passo) => _ultimoPasso[chave] = passo;

    private static void Podar(List<DateTime> lista) => lista.RemoveAll(d => d < DateTime.UtcNow - Janela);
}
