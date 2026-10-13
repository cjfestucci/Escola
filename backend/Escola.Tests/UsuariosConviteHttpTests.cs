using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Escola.Domain.Enums;

namespace Escola.Tests;

/// <summary>Conta da equipe criada em Usuários: nasce sem senha, a pessoa recebe o convite e cadastra a própria senha. A Gestão nunca vê
/// nem define senha de ninguém.</summary>
public partial class UsuariosConviteHttpTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task ContaNovaRecebeConviteENaoEntraAteCadastrarASenha()
    {
        var admin = await api.ClienteLogadoAsync(PapelUsuario.Admin);
        var email = $"prof.{Guid.NewGuid():N}@escola.test";

        var resposta = await admin.PostAsJsonAsync("/api/usuarios/contas", new { nome = "Prof Novo", email, papel = "Educador" });
        resposta.EnsureSuccessStatusCode();
        var envio = await resposta.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.True(envio.GetProperty("emailEnviado").GetBoolean());
        Assert.Equal(JsonValueKind.Null, envio.GetProperty("link").ValueKind); // e-mail entregue: o link não volta pra tela
        Assert.True(envio.GetProperty("conta").GetProperty("convitePendente").GetBoolean());
        Assert.False(envio.TryGetProperty("senha", out _));

        var conviteEmail = api.Email.Enviados.Last(e => e.Para == email);
        Assert.Contains("professor/treinador", conviteEmail.Corpo);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.EntrarAsync(api.CreateClient(), email, "qualquer-senha-1")).StatusCode);

        // Reenviar o convite invalida o anterior; vale o último.
        var id = envio.GetProperty("conta").GetProperty("id").GetGuid();
        (await admin.PostAsync($"/api/usuarios/contas/{id}/enviar-link", null)).EnsureSuccessStatusCode();
        var token = TokenDoLink().Match(api.Email.Enviados.Last(e => e.Para == email).Corpo).Groups[1].Value;
        (await api.CreateClient().PostAsJsonAsync("/api/auth/redefinir-senha", new { token, novaSenha = "Senha-Do-Prof-1" })).EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.OK, (await api.EntrarAsync(api.CreateClient(), email, "Senha-Do-Prof-1")).StatusCode);
        var contas = await admin.GetFromJsonAsync<JsonElement>("/api/usuarios/contas", Json);
        Assert.False(contas.EnumerateArray().Single(c => c.GetProperty("email").GetString() == email).GetProperty("convitePendente").GetBoolean());
    }

    [Fact]
    public async Task EducadorNaoMandaLinkDeSenha()
    {
        var educador = await api.ClienteLogadoAsync(PapelUsuario.Educador);
        var outro = api.CriarUsuario(PapelUsuario.Educador, $"outro.{Guid.NewGuid():N}@escola.test");
        Assert.Equal(HttpStatusCode.Forbidden, (await educador.PostAsync($"/api/usuarios/contas/{outro.Id}/enviar-link", null)).StatusCode);
    }

    [GeneratedRegex(@"token=([A-Za-z0-9_\-]+)")]
    private static partial Regex TokenDoLink();
}
