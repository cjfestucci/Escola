using System.Reflection;
using Escola.Domain.Entities;
using Escola.Infrastructure.Data;
using Escola.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Escola.Api.Servicos;

/// <summary>Apaga <b>todos</b> os dados de um cliente: cada tabela isolada por cliente (o modelo inteiro, descoberto pelo EF — tabela nova
/// entra sozinha), mais as fotos e os documentos de saúde em disco. Irreversível.
/// <para>Ficam: os <see cref="RegistroAcesso"/> (o Marco Civil exige 6 meses; são limpos pela idade) e a linha do <see cref="Cliente"/>,
/// anonimizada e desativada (os registros de acesso apontam pra ela).</para></summary>
public sealed class ExclusaoDadosCliente(
    EscolaDbContext db,
    IFotoStorage fotos,
    IDocumentoStorage documentos,
    ILogger<ExclusaoDadosCliente> logger)
{
    public const string NomeClienteExcluido = "Cliente excluído";

    private static readonly MethodInfo ApagarDoTipo =
        typeof(ExclusaoDadosCliente).GetMethod(nameof(ApagarDoTipoAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <returns>Quantas linhas foram apagadas.</returns>
    public async Task<int> ApagarAsync(Guid clienteId, CancellationToken ct = default)
    {
        // Os arquivos saem depois do banco: se a transação falhar, nada some.
        var urlsFotos = new List<string>();
        urlsFotos.AddRange(await Do<Aluno>().Where(a => a.FotoUrl != null).Select(a => a.FotoUrl!).ToListAsync(ct));
        urlsFotos.AddRange(await Do<FotoRegistro>().Select(f => f.Url).ToListAsync(ct));
        urlsFotos.AddRange(await Do<FotoDiarioClasse>().Select(f => f.Url).ToListAsync(ct));
        urlsFotos.AddRange(await Do<ConfiguracaoEscola>().Where(c => c.LogoUrl != null).Select(c => c.LogoUrl!).ToListAsync(ct));
        var arquivosDocumentos = await Do<DocumentoSaude>().Select(d => d.ArquivoArmazenado).ToListAsync(ct);

        var total = 0;
        await using (var transacao = await db.Database.BeginTransactionAsync(ct))
        {
            foreach (var tipo in OrdemDeExclusao(db.Model))
                total += await (Task<int>)ApagarDoTipo.MakeGenericMethod(tipo).Invoke(null, [db, clienteId, ct])!;

            await db.Clientes.Where(c => c.Id == clienteId)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.Nome, NomeClienteExcluido).SetProperty(c => c.Ativo, false), ct);
            await transacao.CommitAsync(ct);
        }

        foreach (var url in urlsFotos.Distinct()) Tentar(() => fotos.Excluir(url), url);
        foreach (var arquivo in arquivosDocumentos) Tentar(() => documentos.Excluir(arquivo), arquivo);

        logger.LogWarning("Dados do cliente {ClienteId} excluídos: {Linhas} linhas, {Fotos} fotos e {Documentos} documentos.",
            clienteId, total, urlsFotos.Count, arquivosDocumentos.Count);
        return total;

        IQueryable<T> Do<T>() where T : class =>
            db.Set<T>().IgnoreQueryFilters().Where(e => EF.Property<Guid>(e, EscolaDbContext.ColunaCliente) == clienteId);
    }

    /// <summary>As entidades isoladas por cliente (raízes de hierarquia), dependentes antes das principais — senão as chaves estrangeiras
    /// (quase todas Restrict) barram a exclusão. Fica de fora o <see cref="RegistroAcesso"/>.</summary>
    public static IReadOnlyList<Type> OrdemDeExclusao(IModel modelo)
    {
        var raizes = modelo.GetEntityTypes()
            .Where(e => e.BaseType is null && !e.IsOwned() && e.ClrType != typeof(Cliente) && e.ClrType != typeof(RegistroAcesso))
            .ToList();

        // principal ← dependentes: cada tipo só sai depois de todos que apontam pra ele.
        var dependentes = raizes.ToDictionary(e => e, _ => new HashSet<IEntityType>());
        foreach (var tipo in raizes)
            foreach (var fk in tipo.GetDerivedTypesInclusive().SelectMany(t => t.GetDeclaredForeignKeys()))
            {
                var principal = fk.PrincipalEntityType.GetRootType();
                if (principal != tipo && dependentes.TryGetValue(principal, out var lista)) lista.Add(tipo);
            }

        var ordem = new List<Type>();
        var visitados = new HashSet<IEntityType>();
        var emAndamento = new HashSet<IEntityType>();
        foreach (var tipo in raizes.OrderBy(e => e.Name)) Visitar(tipo);
        return ordem;

        void Visitar(IEntityType tipo)
        {
            if (visitados.Contains(tipo)) return;
            if (!emAndamento.Add(tipo))
                throw new InvalidOperationException($"Ciclo de chaves estrangeiras envolvendo {tipo.ClrType.Name}: a exclusão precisa de ordem explícita.");
            foreach (var dependente in dependentes[tipo].OrderBy(e => e.Name)) Visitar(dependente);
            emAndamento.Remove(tipo);
            visitados.Add(tipo);
            ordem.Add(tipo.ClrType);
        }
    }

    private static Task<int> ApagarDoTipoAsync<T>(EscolaDbContext db, Guid clienteId, CancellationToken ct) where T : class =>
        db.Set<T>().IgnoreQueryFilters().Where(e => EF.Property<Guid>(e, EscolaDbContext.ColunaCliente) == clienteId).ExecuteDeleteAsync(ct);

    private void Tentar(Action acao, string arquivo)
    {
        try { acao(); }
        catch (Exception ex) { logger.LogWarning(ex, "Não foi possível apagar o arquivo {Arquivo} na exclusão de dados do cliente.", arquivo); }
    }
}
