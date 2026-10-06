using Escola.Domain.Entities;
using Escola.Infrastructure.Data;
using Escola.Infrastructure.Financeiro;
using Escola.Infrastructure.Tempo;

namespace Escola.Tests;

/// <summary>"Bloqueado" é derivado (nunca gravado): atraso MAIOR que N dias em mensalidade em aberto de aluno ativo.</summary>
public class BloqueioAlunoServiceTests : IDisposable
{
    private readonly BancoDeTeste _banco = new();
    private readonly EscolaDbContext _db;
    private readonly Turma _turma;
    private readonly DateOnly _hoje;

    public BloqueioAlunoServiceTests()
    {
        _db = _banco.Contexto(BancoDeTeste.ClienteA);
        _turma = BancoDeTeste.NovaTurma(_db);
        _db.SaveChanges();
        _hoje = new RelogioEscola(_db).HojeAsync().GetAwaiter().GetResult();
    }

    private void ConfigurarDias(int? dias)
    {
        _db.ConfiguracoesFinanceiras.Add(new ConfiguracaoFinanceira { Id = Guid.NewGuid(), DiasParaBloqueio = dias, AtualizadoEm = DateTime.UtcNow });
        _db.SaveChanges();
    }

    private Aluno NovoAluno(bool ativo = true)
    {
        var aluno = new Aluno { Id = Guid.NewGuid(), Nome = "Atleta", TurmaId = _turma.Id, Ativo = ativo };
        _db.Alunos.Add(aluno);
        _db.SaveChanges();
        return aluno;
    }

    private void NovaCobranca(Aluno aluno, int diasDeAtraso, bool paga = false, bool cancelada = false)
    {
        _db.Cobrancas.Add(new Cobranca
        {
            Id = Guid.NewGuid(), AlunoId = aluno.Id, Descricao = "Mensalidade", Valor = 100m,
            Vencimento = _hoje.AddDays(-diasDeAtraso), Paga = paga, Cancelada = cancelada, RegistradoEm = DateTime.UtcNow
        });
        _db.SaveChanges();
    }

    private BloqueioAlunoService Servico() => new(_db, new RelogioEscola(_db));

    [Fact]
    public async Task SemConfiguracaoNinguemEhBloqueado()
    {
        var aluno = NovoAluno();
        NovaCobranca(aluno, 400);
        Assert.Empty(await Servico().ObterBloqueadosAsync());
    }

    [Fact]
    public async Task ExatamenteNDiasDeAtrasoAindaNaoBloqueiaEMaisDeNBloqueia()
    {
        ConfigurarDias(30);
        var noLimite = NovoAluno(); NovaCobranca(noLimite, 30);
        var passou = NovoAluno(); NovaCobranca(passou, 31);

        var bloqueados = await Servico().ObterBloqueadosAsync();

        Assert.DoesNotContain(noLimite.Id, bloqueados);
        Assert.Contains(passou.Id, bloqueados);
    }

    [Fact]
    public async Task CobrancaPagaOuCanceladaNaoBloqueia()
    {
        ConfigurarDias(10);
        var paga = NovoAluno(); NovaCobranca(paga, 90, paga: true);
        var cancelada = NovoAluno(); NovaCobranca(cancelada, 90, cancelada: true);

        Assert.Empty(await Servico().ObterBloqueadosAsync());
    }

    [Fact]
    public async Task AlunoInativoNaoApareceComoBloqueado()
    {
        ConfigurarDias(10);
        var inativo = NovoAluno(ativo: false);
        NovaCobranca(inativo, 90);

        Assert.Empty(await Servico().ObterBloqueadosAsync());
    }

    [Fact]
    public async Task PagarAMensalidadeAtrasadaDesbloqueiaNaProximaConsulta()
    {
        ConfigurarDias(10);
        var aluno = NovoAluno();
        NovaCobranca(aluno, 20);
        Assert.Contains(aluno.Id, await Servico().ObterBloqueadosAsync());

        var cobranca = _db.Cobrancas.Single(c => c.AlunoId == aluno.Id);
        cobranca.Paga = true;
        _db.SaveChanges();

        Assert.Empty(await Servico().ObterBloqueadosAsync());
    }

    [Fact]
    public async Task FiltraPelosIdsPedidos()
    {
        ConfigurarDias(10);
        var a = NovoAluno(); NovaCobranca(a, 20);
        var b = NovoAluno(); NovaCobranca(b, 20);

        var resultado = await Servico().ObterBloqueadosAsync([a.Id]);

        Assert.Equal([a.Id], resultado);
    }

    [Fact]
    public async Task NaoConsideraCobrancasDeOutroCliente()
    {
        using var dbB = _banco.Contexto(BancoDeTeste.ClienteB);
        var turmaB = BancoDeTeste.NovaTurma(dbB);
        var alunoB = new Aluno { Id = Guid.NewGuid(), Nome = "Do B", TurmaId = turmaB.Id };
        dbB.Alunos.Add(alunoB);
        dbB.Cobrancas.Add(new Cobranca { Id = Guid.NewGuid(), AlunoId = alunoB.Id, Descricao = "M", Valor = 1, Vencimento = _hoje.AddDays(-100), RegistradoEm = DateTime.UtcNow });
        dbB.SaveChanges();

        ConfigurarDias(10);
        Assert.Empty(await Servico().ObterBloqueadosAsync());
    }

    public void Dispose()
    {
        _db.Dispose();
        _banco.Dispose();
    }
}
