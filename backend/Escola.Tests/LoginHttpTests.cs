using System.Net;
using System.Net.Http.Json;
using Escola.Domain.Enums;
using Escola.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;

namespace Escola.Tests;

public class LoginHttpTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _api;
    private readonly HttpClient _http;

    public LoginHttpTests(ApiFactory api)
    {
        _api = api;
        _http = api.CreateClient();
    }

    private static string Email(string prefixo) => $"{prefixo}-{Guid.NewGuid():N}@teste.com";

    [Fact]
    public async Task SenhaCertaDevolveTokenEPapel()
    {
        var email = Email("admin");
        _api.CriarUsuario(PapelUsuario.Admin, email);

        var resposta = await _api.EntrarAsync(_http, email, "Senha-de-Teste-1");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = (await resposta.Content.ReadFromJsonAsync<ApiFactory.LoginResposta>())!;
        Assert.False(string.IsNullOrEmpty(corpo.Token));
        Assert.Equal("Admin", corpo.Papel);
    }

    [Fact]
    public async Task SenhaErradaOuEmailInexistenteDaMesmaMensagem()
    {
        var email = Email("a");
        _api.CriarUsuario(PapelUsuario.Admin, email);

        var errada = await _api.EntrarAsync(_http, email, "errada");
        var inexistente = await _api.EntrarAsync(_http, Email("nao-existe"), "qualquer");

        Assert.Equal(HttpStatusCode.Unauthorized, errada.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, inexistente.StatusCode);
        Assert.Equal(await errada.Content.ReadAsStringAsync(), await inexistente.Content.ReadAsStringAsync()); // não revela quais e-mails existem
    }

    [Fact]
    public async Task EmailNaoDiferenciaMaiusculas()
    {
        var email = Email("maria");
        _api.CriarUsuario(PapelUsuario.Educador, email);
        Assert.Equal(HttpStatusCode.OK, (await _api.EntrarAsync(_http, email.ToUpperInvariant(), "Senha-de-Teste-1")).StatusCode);
    }

    [Fact]
    public async Task ContaDesativadaNaoEntra()
    {
        var email = Email("inativo");
        _api.CriarUsuario(PapelUsuario.Educador, email, ativo: false);

        var resposta = await _api.EntrarAsync(_http, email, "Senha-de-Teste-1");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        Assert.Contains("desativada", await resposta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ContaCriadaPorConviteSemSenhaNaoEntraComNenhumaSenha()
    {
        var email = Email("convidado");
        using (var db = _api.Contexto(ApiFactory.ClienteId))
        {
            db.Usuarios.Add(new Escola.Domain.Entities.Usuario { Id = Guid.NewGuid(), Nome = "C", Email = email, SenhaHash = SenhaHasher.ConvitePendente, Papel = PapelUsuario.Admin });
            await db.SaveChangesAsync();
        }

        foreach (var senha in new[] { "", SenhaHasher.ConvitePendente, "qualquer-senha-1" })
            Assert.Equal(HttpStatusCode.Unauthorized, (await _api.EntrarAsync(_http, email, senha)).StatusCode);
    }

    [Fact]
    public async Task OitoFalhasBloqueiamAContaMesmoComASenhaCertaDepois()
    {
        var email = Email("alvo");
        _api.CriarUsuario(PapelUsuario.Admin, email);

        for (var i = 0; i < 8; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await _api.EntrarAsync(_http, email, "errada")).StatusCode);

        var bloqueada = await _api.EntrarAsync(_http, email, "Senha-de-Teste-1");
        Assert.Equal((HttpStatusCode)429, bloqueada.StatusCode);
        Assert.Contains("Muitas tentativas", await bloqueada.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ClienteSuspensoBarraLoginDeTodoMundoMenosOSuporte()
    {
        var f = new ApiFactory(); // banco próprio: suspender o cliente atrapalharia os outros testes
        var email = Email("equipe");
        f.CriarUsuario(PapelUsuario.Admin, email);
        using (var db = f.Contexto(ApiFactory.ClienteId))
        {
            (await db.Clientes.IgnoreQueryFilters().SingleAsync(c => c.Id == ApiFactory.ClienteId)).Ativo = false;
            await db.SaveChangesAsync();
        }
        var http = f.CreateClient();

        var admin = await f.EntrarAsync(http, email, "Senha-de-Teste-1");
        Assert.Equal(HttpStatusCode.Unauthorized, admin.StatusCode);
        Assert.Contains("suspenso", await admin.Content.ReadAsStringAsync());

        // Suporte entra (pede o código do 2º fator, ou seja: passou pela checagem de suspensão).
        var suporte = await f.EntrarAsync(http, ApiFactory.SuporteEmail, ApiFactory.SuporteSenha);
        Assert.Equal(HttpStatusCode.OK, suporte.StatusCode);
        Assert.True((await suporte.Content.ReadFromJsonAsync<ApiFactory.LoginResposta>())!.RequerSegundoFator);
    }
}
