using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Escola.Tests;

/// <summary>Uma tela de login pra todas as escolas (desde 2026-10-07): o cliente vem da conta que entrou, e cada requisição só
/// enxerga os dados da escola do token.</summary>
public class LoginUnicoHttpTests : IClassFixture<ApiFactory>
{
    private static readonly Guid EscolaB = Guid.Parse("00000000-0000-0000-0000-0000000000b2");
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly ApiFactory _api;

    public LoginUnicoHttpTests(ApiFactory api)
    {
        _api = api;
        using var db = api.Contexto(ApiFactory.ClienteId);
        if (!db.Clientes.IgnoreQueryFilters().Any(c => c.Id == EscolaB))
        {
            db.Clientes.Add(new Cliente { Id = EscolaB, Nome = "Escola B", CriadoEm = DateTime.UtcNow });
            db.SaveChanges();
        }
    }

    private static string Email(string prefixo) => $"{prefixo}-{Guid.NewGuid():N}@teste.com";

    private static Guid ClienteDoToken(string token) =>
        Guid.Parse(new JwtSecurityTokenHandler().ReadJwtToken(token).Claims.First(c => c.Type == "clienteId").Value);

    [Fact]
    public async Task ContaDeOutraEscolaEntraPelaMesmaTelaEOTokenEhDaquelaEscola()
    {
        var email = Email("treinador");
        _api.CriarUsuario(PapelUsuario.Admin, email, clienteId: EscolaB);

        var resposta = await _api.EntrarAsync(_api.CreateClient(), email, "Senha-de-Teste-1");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = (await resposta.Content.ReadFromJsonAsync<ApiFactory.LoginResposta>())!;
        Assert.Equal(EscolaB, ClienteDoToken(corpo.Token));
    }

    [Fact]
    public async Task MesmaSenhaEmDuasEscolasPedeEscolhaEDepoisEntraNaEscolhida()
    {
        var email = Email("duas-escolas");
        _api.CriarUsuario(PapelUsuario.Coordenador, email);
        _api.CriarUsuario(PapelUsuario.Educador, email, clienteId: EscolaB);
        var http = _api.CreateClient();

        var primeira = await (await _api.EntrarAsync(http, email, "Senha-de-Teste-1")).Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal(string.Empty, primeira.GetProperty("token").GetString()); // sem token ainda
        var opcoes = primeira.GetProperty("escolherCliente").EnumerateArray().Select(o => o.GetProperty("id").GetGuid()).ToList();
        Assert.Equal(2, opcoes.Count);
        Assert.Contains(EscolaB, opcoes);

        var escolhida = await http.PostAsJsonAsync("/api/auth/entrar", new { email, senha = "Senha-de-Teste-1", clienteId = EscolaB });
        var corpo = (await escolhida.Content.ReadFromJsonAsync<ApiFactory.LoginResposta>())!;
        Assert.Equal(EscolaB, ClienteDoToken(corpo.Token));
        Assert.Equal("Educador", corpo.Papel);

        // Escolher uma escola em que a senha não confere não entra.
        var outra = Guid.NewGuid();
        Assert.Equal(HttpStatusCode.Unauthorized, (await http.PostAsJsonAsync("/api/auth/entrar", new { email, senha = "Senha-de-Teste-1", clienteId = outra })).StatusCode);
    }

    [Fact]
    public async Task SenhasDiferentesEntraDiretoNaEscolaDaSenhaDigitada()
    {
        var email = Email("senhas");
        _api.CriarUsuario(PapelUsuario.Admin, email, senha: "Senha-da-Escola-A");
        _api.CriarUsuario(PapelUsuario.Admin, email, senha: "Senha-da-Escola-B", clienteId: EscolaB);

        var corpo = (await (await _api.EntrarAsync(_api.CreateClient(), email, "Senha-da-Escola-B")).Content.ReadFromJsonAsync<ApiFactory.LoginResposta>())!;
        Assert.Equal(EscolaB, ClienteDoToken(corpo.Token));
    }

    [Fact]
    public async Task CadaTokenSoEnxergaOsDadosDaPropriaEscola()
    {
        using (var db = _api.Contexto(EscolaB))
        {
            db.Fornecedores.Add(new Fornecedor { Id = Guid.NewGuid(), Nome = "Fornecedor-Exclusivo-Da-B" });
            await db.SaveChangesAsync();
        }

        var email = Email("adm-b");
        _api.CriarUsuario(PapelUsuario.Admin, email, clienteId: EscolaB);
        var http = _api.CreateClient();
        var login = (await (await _api.EntrarAsync(http, email, "Senha-de-Teste-1")).Content.ReadFromJsonAsync<ApiFactory.LoginResposta>())!;
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
        Assert.Contains("Fornecedor-Exclusivo-Da-B", await http.GetStringAsync("/api/fornecedores"));

        var adminA = await _api.ClienteLogadoAsync(PapelUsuario.Admin);
        Assert.DoesNotContain("Fornecedor-Exclusivo-Da-B", await adminA.GetStringAsync("/api/fornecedores"));
    }

    [Fact]
    public async Task SemLoginNaoHaEscolaAConfiguracaoPublicaEhOPadraoDoProduto()
    {
        using (var db = _api.Contexto(EscolaB))
        {
            db.ConfiguracoesEscola.Add(new ConfiguracaoEscola { Id = Guid.NewGuid(), FusoHorario = "America/Manaus", CorPrincipal = "#15803D", LogoUrl = "/uploads/logo-b.png" });
            await db.SaveChangesAsync();
        }

        var anonimo = await _api.CreateClient().GetFromJsonAsync<JsonElement>("/api/configuracao/escola", Json);
        Assert.Equal("America/Sao_Paulo", anonimo.GetProperty("fusoHorario").GetString());
        Assert.Equal(JsonValueKind.Null, anonimo.GetProperty("logoUrl").ValueKind);

        var email = Email("cfg-b");
        _api.CriarUsuario(PapelUsuario.Admin, email, clienteId: EscolaB);
        var http = _api.CreateClient();
        var login = (await (await _api.EntrarAsync(http, email, "Senha-de-Teste-1")).Content.ReadFromJsonAsync<ApiFactory.LoginResposta>())!;
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
        var logado = await http.GetFromJsonAsync<JsonElement>("/api/configuracao/escola", Json);
        Assert.Equal("America/Manaus", logado.GetProperty("fusoHorario").GetString());
        Assert.Equal("/uploads/logo-b.png", logado.GetProperty("logoUrl").GetString());
    }

    [Fact]
    public async Task EsqueciASenhaMandaUmLinkPorEscolaEORedefinirValeNaEscolaDoLink()
    {
        var email = Email("esqueci");
        _api.CriarUsuario(PapelUsuario.Admin, email);
        _api.CriarUsuario(PapelUsuario.Admin, email, clienteId: EscolaB);
        var http = _api.CreateClient();

        Assert.Equal(HttpStatusCode.NoContent, (await http.PostAsJsonAsync("/api/auth/esqueci-senha", new { email })).StatusCode);
        var enviados = _api.Email.Enviados.Where(e => e.Para == email).ToList();
        Assert.Equal(2, enviados.Count);
        var daB = enviados.Single(e => e.Assunto.Contains("Escola B"));

        var token = System.Text.RegularExpressions.Regex.Match(daB.Corpo, @"token=([A-Za-z0-9_\-]+)").Groups[1].Value;
        (await http.PostAsJsonAsync("/api/auth/redefinir-senha", new { token, novaSenha = "Nova-Senha-Da-B" })).EnsureSuccessStatusCode();

        // A senha nova vale só na escola B; a da escola A continua a mesma.
        var corpoB = (await (await _api.EntrarAsync(http, email, "Nova-Senha-Da-B")).Content.ReadFromJsonAsync<ApiFactory.LoginResposta>())!;
        Assert.Equal(EscolaB, ClienteDoToken(corpoB.Token));
        var corpoA = (await (await _api.EntrarAsync(http, email, "Senha-de-Teste-1")).Content.ReadFromJsonAsync<ApiFactory.LoginResposta>())!;
        Assert.Equal(ApiFactory.ClienteId, ClienteDoToken(corpoA.Token));
    }
}
