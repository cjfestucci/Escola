using Escola.Api.Auth;
using Xunit.Abstractions;

namespace Escola.Tests;

/// <summary>Trava a matriz de permissões: se alguém abrir um endpoint sem querer (ou esquecer o [Authorize] num controller novo),
/// um destes testes quebra e obriga a decisão a ser consciente.</summary>
public class PermissoesDosEndpointsTests(ITestOutputHelper saida)
{
    private static readonly IReadOnlyList<MapaDePermissoes.Endpoint> Endpoints = MapaDePermissoes.Todos();

    [Fact]
    public void OMapaEncontraOsEndpointsDoApp() => Assert.True(Endpoints.Count > 100, $"Só {Endpoints.Count} endpoints encontrados.");

    [Fact]
    public void OsUnicosEndpointsAnonimosSaoLoginRecuperacaoDeSenhaConfiguracaoPublicaEWebhookDoPix()
    {
        var anonimos = Endpoints.Where(e => e.Anonimo).Select(e => e.Titulo).OrderBy(t => t).ToList();
        foreach (var a in anonimos) saida.WriteLine(a);

        Assert.Equal(
        [
            "GET /api/configuracao/escola",
            "POST /api/auth/entrar",
            "POST /api/auth/esqueci-senha",
            "POST /api/auth/redefinir-senha",
            "POST /api/pix/webhook/{segredo}",
            "POST /api/pix/webhook/{segredo}/pix",
        ], anonimos);
    }

    [Fact]
    public void TodoEndpointNaoAnonimoExigeLogin()
    {
        var abertos = Endpoints.Where(e => !e.Anonimo && !e.ExigeLogin).Select(e => $"{e.Controller}.{e.Acao}").ToList();
        Assert.True(abertos.Count == 0, "Endpoints sem [Authorize]: " + string.Join(", ", abertos));
    }

    [Fact]
    public void SuporteNaoEstaEmNenhumGrupoDaEquipeDoCliente()
    {
        foreach (var grupo in new[] { GruposDePapeis.Equipe, GruposDePapeis.Gestao, GruposDePapeis.Financeiro, GruposDePapeis.Responsavel })
            Assert.DoesNotContain("Suporte", grupo.Split(','));
    }

    [Fact]
    public void OsGruposComSuporteSaoOsDeSempreMaisSuporte()
    {
        Assert.Equal(GruposDePapeis.Gestao + ",Suporte", GruposDePapeis.GestaoOuSuporte);
        Assert.Equal(GruposDePapeis.Financeiro + ",Suporte", GruposDePapeis.FinanceiroOuSuporte);
        Assert.Equal(GruposDePapeis.Equipe + ",Suporte", GruposDePapeis.EquipeOuSuporte);
    }

    [Fact]
    public void TudoDeApiPlataformaEhExclusivoDoSuporte()
    {
        var plataforma = Endpoints.Where(e => e.Rota.StartsWith("/api/plataforma")).ToList();
        Assert.NotEmpty(plataforma);
        foreach (var e in plataforma)
            Assert.Equal(["Suporte"], e.Papeis.OrderBy(p => p));
    }

    /// <summary>A lista fechada do que o Suporte alcança. Mudou? Atualize junto com a seção "Suporte" do CLAUDE.md,
    /// depois de decidir de propósito que ele deve (ou não) ver aquilo — por padrão, não deve.</summary>
    [Fact]
    public void OSuporteAlcancaSoAConfiguracaoDoAmbiente()
    {
        var doSuporte = Endpoints.Where(e => e.Permite("Suporte") && !e.Anonimo).Select(e => e.Titulo).OrderBy(t => t).ToList();
        // Só [Authorize] puro: quem barra o Suporte é a checagem dentro da ação (exige ser Equipe ou o próprio responsável) —
        // coberto por teste HTTP em PermissoesHttpTests.
        doSuporte.Remove("GET /api/responsaveis/{id:guid}/alunos");
        foreach (var t in doSuporte) saida.WriteLine(t);

        var foraDaConfiguracao = doSuporte.Where(t =>
            !t.Contains("/api/plataforma") && !t.Contains("/api/configuracao/escola") && !t.Contains("/api/financeiro/configuracao")
            && !t.Contains("/api/pix/") && !t.Contains("/api/logs")).ToList();

        Assert.True(foraDaConfiguracao.Count == 0, "O Suporte alcança dado do cliente: " + string.Join("; ", foraDaConfiguracao));
    }

    [Theory]
    [InlineData("/api/alunos")]
    [InlineData("/api/turmas")]
    [InlineData("/api/financeiro/cobrancas")]
    [InlineData("/api/financeiro/contas-pagar")]
    [InlineData("/api/usuarios")]
    [InlineData("/api/jogos")]
    [InlineData("/api/estoque/produtos")]
    [InlineData("/api/fornecedores")]
    [InlineData("/api/dashboard/resumo")]
    public void SuporteNaoListaDadosDoCliente(string rota)
    {
        var lista = Endpoints.Single(e => e.Metodo == "GET" && e.Rota == rota);
        Assert.False(lista.Permite("Suporte"));
    }

    [Theory]
    [InlineData("/api/financeiro/cobrancas")]
    [InlineData("/api/financeiro/contas-pagar")]
    [InlineData("/api/usuarios")]
    [InlineData("/api/usuarios/contas")]
    public void EducadorNaoEnxergaFinanceiroNemUsuarios(string rota)
    {
        var lista = Endpoints.Single(e => e.Metodo == "GET" && e.Rota == rota);
        Assert.False(lista.Permite("Educador"));
    }

    [Fact]
    public void ResponsavelNaoEntraEmNadaDaEquipe()
    {
        // Rotas por aluno (ficha, documentos, jogos, frequência…) liberam o Responsável de propósito e checam a posse do filho
        // dentro da ação; todo o resto que ele alcança tem que ser só o portal.
        var alcance = Endpoints.Where(e => !e.Anonimo && e.Permite("Responsavel")).Select(e => e.Rota).ToList();
        var forasDoPortal = alcance.Where(r => !r.StartsWith("/api/alunos/") && !r.StartsWith("/api/responsaveis/")
            && !r.StartsWith("/api/turmas/") && !r.StartsWith("/api/financeiro/cobrancas/") && !r.StartsWith("/api/logs")).ToList();
        foreach (var r in forasDoPortal) saida.WriteLine(r);

        Assert.True(forasDoPortal.Count == 0, "Responsável alcança: " + string.Join("; ", forasDoPortal));
    }
}
