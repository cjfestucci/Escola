using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Escola.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Escola.Tests;

/// <summary>O JWT dura 30 dias e não dá pra "desemitir": a cada requisição o servidor confere se a conta ainda vale.
/// Estes testes provam que desativar, trocar senha ou encerrar sessões derruba o token que já estava na mão da pessoa.</summary>
public class SessoesHttpTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _api;

    public SessoesHttpTests(ApiFactory api) => _api = api;

    private const string RotaQualquer = "/api/turmas"; // Equipe: um educador logado vê (200); revogado leva 401

    private async Task<(HttpClient Http, Usuario Usuario, string Email)> EducadorLogadoAsync()
    {
        var email = $"educador-{Guid.NewGuid():N}@teste.com";
        var usuario = _api.CriarUsuario(PapelUsuario.Educador, email);
        var http = _api.CreateClient();
        var resposta = await _api.EntrarAsync(http, email, "Senha-de-Teste-1");
        resposta.EnsureSuccessStatusCode();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await resposta.Content.ReadFromJsonAsync<ApiFactory.LoginResposta>())!.Token);
        return (http, usuario, email);
    }

    private static async Task<HttpStatusCode> Status(HttpClient http, string rota = RotaQualquer) => (await http.GetAsync(rota)).StatusCode;

    [Fact]
    public async Task DesativarAContaDerrubaOTokenQueJaEstavaAberto()
    {
        var (educador, usuario, email) = await EducadorLogadoAsync();
        var admin = await _api.ClienteLogadoAsync(PapelUsuario.Admin);
        Assert.Equal(HttpStatusCode.OK, await Status(educador));

        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/usuarios/contas/{usuario.Id}/desativar", null)).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, await Status(educador));

        // Reativar NÃO ressuscita a sessão antiga: a pessoa precisa entrar de novo.
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/usuarios/contas/{usuario.Id}/ativar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, await Status(educador));

        var novo = _api.CreateClient();
        var login = await _api.EntrarAsync(novo, email, "Senha-de-Teste-1");
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        novo.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await login.Content.ReadFromJsonAsync<ApiFactory.LoginResposta>())!.Token);
        Assert.Equal(HttpStatusCode.OK, await Status(novo));
    }

    [Fact]
    public async Task EncerrarSessoesDerrubaOsTokensMasMantemAContaAtiva()
    {
        var (educador, usuario, email) = await EducadorLogadoAsync();
        var admin = await _api.ClienteLogadoAsync(PapelUsuario.Admin);

        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/usuarios/contas/{usuario.Id}/encerrar-sessoes", null)).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, await Status(educador));
        Assert.Equal(HttpStatusCode.OK, (await _api.EntrarAsync(_api.CreateClient(), email, "Senha-de-Teste-1")).StatusCode); // a senha continua a mesma
    }

    [Fact]
    public async Task SoQuemEhDaGestaoEncerraSessoesDeOutraConta()
    {
        var (educador, usuario, _) = await EducadorLogadoAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await educador.PostAsync($"/api/usuarios/contas/{usuario.Id}/encerrar-sessoes", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, await Status(educador)); // e a própria sessão segue valendo
    }

    [Fact]
    public async Task RedefinirASenhaPeloAdminDerrubaASessaoDaPessoa()
    {
        var (educador, usuario, _) = await EducadorLogadoAsync();
        var admin = await _api.ClienteLogadoAsync(PapelUsuario.Admin);

        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/usuarios/contas/{usuario.Id}/redefinir-senha", null)).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, await Status(educador));
    }

    [Fact]
    public async Task AdminQueRedefineASenhaDeSiMesmoNaoPerdeASessaoNaMesmaHora()
    {
        var email = $"admin-{Guid.NewGuid():N}@teste.com";
        var usuario = _api.CriarUsuario(PapelUsuario.Admin, email);
        var http = _api.CreateClient();
        var login = await _api.EntrarAsync(http, email, "Senha-de-Teste-1");
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await login.Content.ReadFromJsonAsync<ApiFactory.LoginResposta>())!.Token);

        Assert.Equal(HttpStatusCode.OK, (await http.PostAsync($"/api/usuarios/contas/{usuario.Id}/redefinir-senha", null)).StatusCode);

        Assert.Equal(HttpStatusCode.OK, await Status(http, "/api/usuarios/contas"));
    }

    [Fact]
    public async Task RedefinirASenhaPeloLinkDoEmailDerrubaAsSessoesAbertas()
    {
        var f = new ApiFactory();
        var email = "pessoa@escola.teste";
        var usuario = f.CriarUsuario(PapelUsuario.Educador, email);
        var http = f.CreateClient();
        var login = await f.EntrarAsync(http, email, "Senha-de-Teste-1");
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await login.Content.ReadFromJsonAsync<ApiFactory.LoginResposta>())!.Token);
        Assert.Equal(HttpStatusCode.OK, await Status(http));

        // pede o link ("esqueci minha senha") e usa
        Assert.Equal(HttpStatusCode.NoContent, (await f.CreateClient().PostAsJsonAsync("/api/auth/esqueci-senha", new { email })).StatusCode);
        var corpo = f.Email.Enviados.Single().Corpo;
        var token = System.Text.RegularExpressions.Regex.Match(corpo, @"redefinir-senha\?token=([A-Za-z0-9_\-]+)").Groups[1].Value;
        Assert.Equal(HttpStatusCode.NoContent, (await f.CreateClient().PostAsJsonAsync("/api/auth/redefinir-senha", new { token, novaSenha = "Nova-Senha-2026" })).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, await Status(http)); // quem tinha a senha antiga sai de todas as sessões
        Assert.Equal(HttpStatusCode.OK, (await f.EntrarAsync(f.CreateClient(), email, "Nova-Senha-2026")).StatusCode);
    }

    [Fact]
    public async Task RedefinirASenhaDoResponsavelDerrubaASessaoDele()
    {
        Guid responsavelId;
        var sufixo = Guid.NewGuid().ToString("N");
        var email = $"pai-{sufixo}@teste.com";
        using (var db = _api.Contexto(ApiFactory.ClienteId))
        {
            var responsavel = new Responsavel { Id = Guid.NewGuid(), Nome = "Pai", Email = email };
            db.Responsaveis.Add(responsavel);
            db.Usuarios.Add(new Usuario { Id = Guid.NewGuid(), Nome = "Pai", Email = email, Papel = PapelUsuario.Responsavel, SenhaHash = SenhaHasher.Hash("Senha-de-Teste-1"), ResponsavelId = responsavel.Id });
            await db.SaveChangesAsync();
            responsavelId = responsavel.Id;
        }
        var pai = _api.CreateClient();
        var login = await _api.EntrarAsync(pai, email, "Senha-de-Teste-1");
        pai.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await login.Content.ReadFromJsonAsync<ApiFactory.LoginResposta>())!.Token);
        Assert.Equal(HttpStatusCode.OK, await Status(pai, $"/api/responsaveis/{responsavelId}/alunos"));

        var admin = await _api.ClienteLogadoAsync(PapelUsuario.Admin);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/responsaveis/{responsavelId}/redefinir-senha", null)).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, await Status(pai, $"/api/responsaveis/{responsavelId}/alunos"));
    }

    [Fact]
    public async Task SuspenderOClienteCortaAsSessoesAbertasMasNaoADoSuporte()
    {
        var f = new ApiFactory();
        var (admin, suporte) = (await f.ClienteLogadoAsync(PapelUsuario.Admin), await f.SuporteLogadoAsync());
        Assert.Equal(HttpStatusCode.OK, await Status(admin));

        using (var db = f.Contexto(ApiFactory.ClienteId))
        {
            (await db.Clientes.IgnoreQueryFilters().SingleAsync(c => c.Id == ApiFactory.ClienteId)).Ativo = false;
            await db.SaveChangesAsync();
        }

        Assert.Equal(HttpStatusCode.Unauthorized, await Status(admin));
        Assert.Equal(HttpStatusCode.OK, await Status(suporte, "/api/plataforma/cliente")); // o Suporte precisa conseguir reativar

        using (var db = f.Contexto(ApiFactory.ClienteId))
        {
            (await db.Clientes.IgnoreQueryFilters().SingleAsync(c => c.Id == ApiFactory.ClienteId)).Ativo = true;
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.OK, await Status(admin)); // reativar o cliente devolve as sessões (ninguém foi revogado individualmente)
    }

    [Fact]
    public async Task ContaApagadaDoBancoNaoTemSessao()
    {
        var (educador, usuario, _) = await EducadorLogadoAsync();
        using (var db = _api.Contexto(ApiFactory.ClienteId))
        {
            db.Usuarios.Remove(await db.Usuarios.SingleAsync(u => u.Id == usuario.Id));
            await db.SaveChangesAsync();
        }

        Assert.Equal(HttpStatusCode.Unauthorized, await Status(educador));
    }

    // ----- tokens "antigos" (emitidos antes desta regra existir, sem o instante de emissão) -----

    private HttpClient ComTokenSemInstante(Usuario usuario)
    {
        var credenciais = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ApiFactory.JwtChave)), SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()), new Claim(ClaimTypes.Name, usuario.Nome),
            new Claim(ClaimTypes.Role, usuario.Papel.ToString()), new Claim("clienteId", ApiFactory.ClienteId.ToString())
        };
        var token = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken("RotinaEscola", null, claims, expires: DateTime.UtcNow.AddHours(1), signingCredentials: credenciais));
        var http = _api.CreateClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return http;
    }

    [Fact]
    public async Task TokenAntigoSemInstanteContinuaValendoSeNinguemRevogouNada()
    {
        // Retrocompatibilidade: publicar esta regra não pode deslogar todo mundo de uma vez.
        var usuario = _api.CriarUsuario(PapelUsuario.Educador, $"antigo-{Guid.NewGuid():N}@teste.com");
        Assert.Equal(HttpStatusCode.OK, await Status(ComTokenSemInstante(usuario)));
    }

    [Fact]
    public async Task TokenAntigoSemInstanteNaoVaiDepoisDeUmaRevogacao()
    {
        var usuario = _api.CriarUsuario(PapelUsuario.Educador, $"antigo2-{Guid.NewGuid():N}@teste.com");
        var http = ComTokenSemInstante(usuario);
        Assert.Equal(HttpStatusCode.OK, await Status(http));

        using (var db = _api.Contexto(ApiFactory.ClienteId))
        {
            (await db.Usuarios.SingleAsync(u => u.Id == usuario.Id)).EncerrarSessoes();
            await db.SaveChangesAsync();
        }

        Assert.Equal(HttpStatusCode.Unauthorized, await Status(http));
    }
}
