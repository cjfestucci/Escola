using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Escola.Domain.Enums;

namespace Escola.Tests;

/// <summary>Painel do Suporte com todos os clientes: atravessa clientes de propósito, mas só com contagens e a assinatura.</summary>
public class PlataformaClientesHttpTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task OSuporteVeTodosOsClientesComAAssinaturaEContagens()
    {
        // O login do Suporte é feito antes do cadastro: depois dele o Suporte também existe no clube novo e o login pediria a escolha da escola.
        var suporte = await api.SuporteLogadoAsync();

        var email = $"dono.{Guid.NewGuid():N}@clube.test";
        (await api.CreateClient().PostAsJsonAsync("/api/assinaturas", new
        {
            nomeClube = "Clube Do Painel", cpfCnpj = "11222333000181", cidade = "Campinas", atletas = 20,
            nomeAdmin = "Dono", email, celular = "(19) 99999-8888", aceiteTermos = true
        })).EnsureSuccessStatusCode();

        var clientes = await suporte.GetFromJsonAsync<JsonElement>("/api/plataforma/clientes", Json);
        var lista = clientes.EnumerateArray().ToList();

        var doSite = lista.Single(c => c.GetProperty("nome").GetString() == "Clube Do Painel");
        Assert.Equal("EmTeste", doSite.GetProperty("situacao").GetString());
        Assert.Equal(159m, doSite.GetProperty("valorMensal").GetDecimal()); // 99 + 3 × 20
        Assert.Equal(email, doSite.GetProperty("emailCobranca").GetString());
        Assert.Equal(1, doSite.GetProperty("contasEquipe").GetInt32()); // o Admin (o Suporte não conta)

        var manual = lista.Single(c => c.GetProperty("id").GetGuid() == ApiFactory.ClienteId);
        Assert.Equal(JsonValueKind.Null, manual.GetProperty("situacao").ValueKind); // criado à mão: sem assinatura

        // Nada de dado de aluno no resumo.
        Assert.False(doSite.TryGetProperty("alunos", out _));
    }

    [Fact]
    public async Task SoOSuporteVeOPainel()
    {
        var admin = await api.ClienteLogadoAsync(PapelUsuario.Admin);
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.GetAsync("/api/plataforma/clientes")).StatusCode);
    }
}
