using Escola.Infrastructure.Data;
using Escola.Infrastructure.Tempo;
using Microsoft.EntityFrameworkCore;

namespace Escola.Infrastructure.Financeiro;

public interface IBloqueioAlunoService
{
    /// <summary>Ids (entre <paramref name="alunoIds"/>, ou de todos se nulo) dos alunos hoje bloqueados por atraso.</summary>
    Task<HashSet<Guid>> ObterBloqueadosAsync(IReadOnlyCollection<Guid>? alunoIds = null);
}

/// <summary>"Bloqueado" não é um campo gravado — é sempre derivado: com <c>DiasParaBloqueio</c> configurado, é o aluno
/// ativo que tem alguma mensalidade em aberto (não paga e não cancelada) vencida há MAIS que esse número de dias.
/// Derivar (como o "Atrasado" das cobranças) evita um job pra manter o status em dia: pagar, cancelar ou reabrir uma
/// cobrança, ou mudar a configuração, já reflete na próxima consulta.</summary>
public class BloqueioAlunoService(EscolaDbContext db, IRelogioEscola relogio) : IBloqueioAlunoService
{
    public async Task<HashSet<Guid>> ObterBloqueadosAsync(IReadOnlyCollection<Guid>? alunoIds = null)
    {
        var dias = await db.ConfiguracoesFinanceiras.Select(c => c.DiasParaBloqueio).FirstOrDefaultAsync();
        if (dias is not { } limite) return [];

        // Atraso = hoje − vencimento; "ultrapassou N dias" ⇔ vencimento anterior a (hoje − N).
        var venceuAntesDe = (await relogio.HojeAsync()).AddDays(-limite);

        var query = db.Cobrancas.Where(c => !c.Paga && !c.Cancelada && c.Vencimento < venceuAntesDe && c.Aluno.Ativo);
        if (alunoIds is not null) query = query.Where(c => alunoIds.Contains(c.AlunoId));

        var bloqueados = await query.Select(c => c.AlunoId).Distinct().ToListAsync();
        return bloqueados.ToHashSet();
    }
}
