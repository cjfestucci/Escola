using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Escola.Domain.Enums;
using Escola.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;

namespace Escola.Tests;

/// <summary>Conta de Suporte (equipe do produto): 2FA obrigatório, privilégio mínimo e o convite do Admin da escola.</summary>
public class SuporteHttpTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _api;

    public SuporteHttpTests(ApiFactory api) => _api = api;

    private Task<HttpClient> SuporteLogadoAsync() => _api.SuporteLogadoAsync();

    // ----- 2FA -----

    [Fact]
    public async Task SenhaCertaSemCodigoNaoEmiteTokenEPedeOSegundoFator()
    {
        var resposta = await _api.EntrarAsync(_api.CreateClient(), ApiFactory.SuporteEmail, ApiFactory.SuporteSenha);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = (await resposta.Content.ReadFromJsonAsync<ApiFactory.LoginResposta>())!;
        Assert.True(corpo.RequerSegundoFator);
        Assert.True(string.IsNullOrEmpty(corpo.Token));
    }

    [Fact]
    public async Task CodigoCertoEmiteToken()
    {
        var http = await SuporteLogadoAsync();
        Assert.Equal(HttpStatusCode.OK, (await http.GetAsync("/api/plataforma/cliente")).StatusCode);
    }

    [Fact]
    public async Task CodigoJaUsadoNaoEntraDeNovo()
    {
        var f = new ApiFactory(); // limitador próprio: este teste gasta tentativas
        var http = f.CreateClient();
        var codigo = f.ProximoCodigoSuporte();

        Assert.Equal(HttpStatusCode.OK, (await f.EntrarAsync(http, ApiFactory.SuporteEmail, ApiFactory.SuporteSenha, codigo)).StatusCode);
        var repetido = await f.EntrarAsync(http, ApiFactory.SuporteEmail, ApiFactory.SuporteSenha, codigo);

        Assert.Equal(HttpStatusCode.Unauthorized, repetido.StatusCode);
    }

    [Fact]
    public async Task CodigoErradoOuSenhaErradaNaoEntram()
    {
        var f = new ApiFactory();
        var http = f.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await f.EntrarAsync(http, ApiFactory.SuporteEmail, ApiFactory.SuporteSenha, "000000")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await f.EntrarAsync(http, ApiFactory.SuporteEmail, "senha-errada", f.ProximoCodigoSuporte())).StatusCode);
    }

    [Fact]
    public async Task CincoFalhasBloqueiamOSuporte()
    {
        var f = new ApiFactory();
        var http = f.CreateClient();
        for (var i = 0; i < 5; i++) await f.EntrarAsync(http, ApiFactory.SuporteEmail, ApiFactory.SuporteSenha, "000000");

        var bloqueado = await f.EntrarAsync(http, ApiFactory.SuporteEmail, ApiFactory.SuporteSenha, f.ProximoCodigoSuporte());

        Assert.Equal((HttpStatusCode)429, bloqueado.StatusCode);
    }

    // ----- privilégio mínimo -----

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
    public async Task SuporteNaoLeDadosDoCliente(string rota)
    {
        var http = await SuporteLogadoAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await http.GetAsync(rota)).StatusCode);
    }

    [Fact]
    public async Task SuporteNaoEnxergaAsFichasDeUmResponsavel()
    {
        var http = await SuporteLogadoAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await http.GetAsync($"/api/responsaveis/{Guid.NewGuid()}/alunos")).StatusCode);
    }

    [Fact]
    public async Task SuporteNaoApareceNaListaDeUsuariosDoAdmin()
    {
        var admin = await _api.ClienteLogadoAsync(PapelUsuario.Admin);
        var texto = await admin.GetStringAsync("/api/usuarios/contas");
        Assert.DoesNotContain(ApiFactory.SuporteEmail, texto);
    }

    [Fact]
    public async Task AdminNaoMexeNaContaDoSuporte()
    {
        Guid idSuporte;
        using (var db = _api.Contexto(ApiFactory.ClienteId))
            idSuporte = (await db.Usuarios.SingleAsync(u => u.Email == ApiFactory.SuporteEmail)).Id;

        var admin = await _api.ClienteLogadoAsync(PapelUsuario.Admin);

        Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsync($"/api/usuarios/contas/{idSuporte}/desativar", null)).StatusCode);
    }

    [Theory]
    [InlineData(PapelUsuario.Admin)]
    [InlineData(PapelUsuario.Coordenador)]
    [InlineData(PapelUsuario.Educador)]
    [InlineData(PapelUsuario.Financeiro)]
    [InlineData(PapelUsuario.Responsavel)]
    public async Task NenhumPapelDoClienteAcessaAPlataforma(PapelUsuario papel)
    {
        var http = await _api.ClienteLogadoAsync(papel);
        Assert.Equal(HttpStatusCode.Forbidden, (await http.GetAsync("/api/plataforma/cliente")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await http.GetAsync("/api/plataforma/admins")).StatusCode);
    }

    // ----- convite do Admin da escola -----

    private static string TokenDoEmail(string corpoHtml)
    {
        var m = Regex.Match(corpoHtml, @"redefinir-senha\?token=([A-Za-z0-9_\-]+)&(?:amp;)?convite=1");
        Assert.True(m.Success, "O e-mail de convite não trouxe o link esperado.");
        return m.Groups[1].Value;
    }

    [Fact]
    public async Task ConviteDeAdminFluxoCompletoDoEmailAteOLogin()
    {
        var f = new ApiFactory();
        var suporte = await f.SuporteLogadoAsync();

        // 1) cria o admin: nasce pendente e o e-mail sai pro endereço informado
        var criado = await suporte.PostAsJsonAsync("/api/plataforma/admins", new { nome = "Diretora Teste", email = "diretora@escola.teste" });
        Assert.Equal(HttpStatusCode.OK, criado.StatusCode);
        var enviado = Assert.Single(f.Email.Enviados);
        Assert.Equal("diretora@escola.teste", enviado.Para);
        var token = TokenDoEmail(enviado.Corpo);

        // 2) enquanto o convite não for aceito, não entra com nenhuma senha
        var http = f.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await f.EntrarAsync(http, "diretora@escola.teste", "Qualquer-Senha-1")).StatusCode);

        // 3) o token fica guardado só como hash (ler o banco não permite aceitar o convite de ninguém)
        using (var db = f.Contexto(ApiFactory.ClienteId))
            Assert.DoesNotContain(await db.RedefinicoesSenha.Select(r => r.TokenHash).ToListAsync(), h => h == token);

        // 4) aceita o convite: senha curta é recusada, senha boa cadastra
        Assert.Equal(HttpStatusCode.BadRequest, (await http.PostAsJsonAsync("/api/auth/redefinir-senha", new { token, novaSenha = "curta" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await http.PostAsJsonAsync("/api/auth/redefinir-senha", new { token, novaSenha = "Senha-Da-Diretora-2026" })).StatusCode);

        // 5) o mesmo link não vale duas vezes
        Assert.Equal(HttpStatusCode.BadRequest, (await http.PostAsJsonAsync("/api/auth/redefinir-senha", new { token, novaSenha = "Outra-Senha-2026" })).StatusCode);

        // 6) agora entra, como Admin
        var entrou = await f.EntrarAsync(http, "diretora@escola.teste", "Senha-Da-Diretora-2026");
        Assert.Equal(HttpStatusCode.OK, entrou.StatusCode);
        Assert.Equal("Admin", (await entrou.Content.ReadFromJsonAsync<ApiFactory.LoginResposta>())!.Papel);

        // 7) e o histórico mostra quem criou (Suporte) e que o convite foi aceito
        using var dbLogs = f.Contexto(ApiFactory.ClienteId);
        var detalhes = await dbLogs.LogsAuditoria.Select(l => l.Detalhe).ToListAsync();
        Assert.Contains(detalhes, d => d != null && d.StartsWith("Administrador criado por convite"));
        Assert.Contains(detalhes, d => d != null && d.StartsWith("Convite aceito"));
    }

    [Fact]
    public async Task ConviteCanceladoInvalidaOLinkEBloqueiaAConta()
    {
        var f = new ApiFactory();
        var suporte = await f.SuporteLogadoAsync();

        var criado = await suporte.PostAsJsonAsync("/api/plataforma/admins", new { nome = "Cancelado", email = "cancelado@escola.teste" });
        var id = (await criado.Content.ReadFromJsonAsync<ResultadoEnvio>())!.Admin.Id;
        var token = TokenDoEmail(f.Email.Enviados.Single().Corpo);

        Assert.Equal(HttpStatusCode.OK, (await suporte.PostAsync($"/api/plataforma/admins/{id}/cancelar-convite", null)).StatusCode);

        var http = f.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await http.PostAsJsonAsync("/api/auth/redefinir-senha", new { token, novaSenha = "Senha-Qualquer-2026" })).StatusCode);
    }

    [Fact]
    public async Task NaoCriaAdminComEmailJaUsadoNemComDadosInvalidos()
    {
        var suporte = await SuporteLogadoAsync();
        _api.CriarUsuario(PapelUsuario.Educador, "ocupado@escola.teste");

        Assert.Equal(HttpStatusCode.BadRequest, (await suporte.PostAsJsonAsync("/api/plataforma/admins", new { nome = "X", email = "ocupado@escola.teste" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await suporte.PostAsJsonAsync("/api/plataforma/admins", new { nome = "", email = "ok@escola.teste" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await suporte.PostAsJsonAsync("/api/plataforma/admins", new { nome = "X", email = "isso-nao-e-email" })).StatusCode);
    }

    [Fact]
    public async Task SoListaContasDeAdminNaPlataforma()
    {
        _api.CriarUsuario(PapelUsuario.Educador, "educador-invisivel@escola.teste");
        _api.CriarUsuario(PapelUsuario.Admin, "admin-visivel@escola.teste");
        var suporte = await SuporteLogadoAsync();

        var texto = await suporte.GetStringAsync("/api/plataforma/admins");

        Assert.Contains("admin-visivel@escola.teste", texto);
        Assert.DoesNotContain("educador-invisivel@escola.teste", texto);
        Assert.DoesNotContain(ApiFactory.SuporteEmail, texto);
    }

    private sealed record AdminDto(Guid Id, string Nome, string Email, bool Ativo, bool Pendente);
    private sealed record ResultadoEnvio(AdminDto Admin, bool EmailEnviado, string? Aviso);
}
