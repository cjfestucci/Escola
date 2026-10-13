using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Escola.Domain.Enums;
using Escola.Infrastructure.Auth;

namespace Escola.Tests;

/// <summary>Verificação em duas etapas opcional (Minha conta): ativar com o app, entrar com código ou código de recuperação (uma vez cada),
/// desativar com senha + código, e a Gestão removendo de quem perdeu o celular.</summary>
public class DoisFatoresHttpTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private const string Senha = "Senha-de-Teste-1";

    private static string CodigoAgora(string segredo) => Totp.Codigo(segredo, Totp.PassoAtual(DateTime.UtcNow));

    /// <summary>Cria a conta, entra, ativa a verificação e devolve o segredo e os códigos de recuperação.</summary>
    private async Task<(string Email, Guid Id, string Segredo, List<string> Codigos)> ContaComDoisFatoresAsync(PapelUsuario papel)
    {
        var email = $"{papel.ToString().ToLowerInvariant()}.{Guid.NewGuid():N}@escola.test";
        var usuario = api.CriarUsuario(papel, email);
        var http = await LogadoAsync(email);

        var inicio = await (await http.PostAsync("/api/minha-conta/dois-fatores/iniciar", null)).Content.ReadFromJsonAsync<JsonElement>(Json);
        var segredo = inicio.GetProperty("segredo").GetString()!;
        Assert.StartsWith("otpauth://totp/", inicio.GetProperty("uriOtpAuth").GetString());

        Assert.Equal(HttpStatusCode.BadRequest, (await http.PostAsJsonAsync("/api/minha-conta/dois-fatores/confirmar", new { codigo = "000000" })).StatusCode);
        var confirmacao = await http.PostAsJsonAsync("/api/minha-conta/dois-fatores/confirmar", new { codigo = CodigoAgora(segredo) });
        confirmacao.EnsureSuccessStatusCode();
        var codigos = (await confirmacao.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("codigos")
            .EnumerateArray().Select(c => c.GetString()!).ToList();
        Assert.Equal(CodigosRecuperacao.Quantidade, codigos.Count);
        return (email, usuario.Id, segredo, codigos);
    }

    private async Task<HttpClient> LogadoAsync(string email, string? codigo = null)
    {
        var resposta = await api.EntrarAsync(api.CreateClient(), email, Senha, codigo);
        resposta.EnsureSuccessStatusCode();
        var http = api.CreateClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            (await resposta.Content.ReadFromJsonAsync<ApiFactory.LoginResposta>())!.Token);
        return http;
    }

    [Fact]
    public async Task ComAVerificacaoAtivaOLoginPedeOCodigoEAceitaCodigoDeRecuperacaoUmaVez()
    {
        var (email, _, segredo, codigos) = await ContaComDoisFatoresAsync(PapelUsuario.Coordenador);

        var semCodigo = await (await api.EntrarAsync(api.CreateClient(), email, Senha)).Content.ReadFromJsonAsync<ApiFactory.LoginResposta>();
        Assert.True(semCodigo!.RequerSegundoFator);
        Assert.True(string.IsNullOrEmpty(semCodigo.Token));
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.EntrarAsync(api.CreateClient(), email, Senha, "123456")).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await api.EntrarAsync(api.CreateClient(), email, Senha, CodigoAgora(segredo))).StatusCode);

        // Código de recuperação, com ou sem traço e em minúsculas — e só uma vez.
        Assert.Equal(HttpStatusCode.OK, (await api.EntrarAsync(api.CreateClient(), email, Senha, codigos[0].ToLowerInvariant().Replace("-", ""))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.EntrarAsync(api.CreateClient(), email, Senha, codigos[0])).StatusCode);

        var http = await LogadoAsync(email, codigos[1]);
        var estado = await http.GetFromJsonAsync<JsonElement>("/api/minha-conta/dois-fatores", Json);
        Assert.True(estado.GetProperty("ativo").GetBoolean());
        Assert.Equal(CodigosRecuperacao.Quantidade - 2, estado.GetProperty("codigosRecuperacaoRestantes").GetInt32());

        // Desativar pede a senha certa e um código.
        Assert.Equal(HttpStatusCode.BadRequest, (await http.PostAsJsonAsync("/api/minha-conta/dois-fatores/desativar", new { senha = "errada", codigo = codigos[2] })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await http.PostAsJsonAsync("/api/minha-conta/dois-fatores/desativar", new { senha = Senha, codigo = codigos[2] })).StatusCode);
        Assert.NotNull((await (await api.EntrarAsync(api.CreateClient(), email, Senha)).Content.ReadFromJsonAsync<ApiFactory.LoginResposta>())!.Token);
    }

    [Fact]
    public async Task AGestaoRemoveAVerificacaoDeQuemPerdeuOCelular()
    {
        var (email, id, _, _) = await ContaComDoisFatoresAsync(PapelUsuario.Educador);
        var contas = await (await api.ClienteLogadoAsync(PapelUsuario.Admin)).GetFromJsonAsync<JsonElement>("/api/usuarios/contas", Json);
        Assert.True(contas.EnumerateArray().Single(c => c.GetProperty("id").GetGuid() == id).GetProperty("doisFatores").GetBoolean());

        var educador = await api.ClienteLogadoAsync(PapelUsuario.Educador);
        Assert.Equal(HttpStatusCode.Forbidden, (await educador.PostAsync($"/api/usuarios/contas/{id}/remover-dois-fatores", null)).StatusCode);

        var admin = await api.ClienteLogadoAsync(PapelUsuario.Admin);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/usuarios/contas/{id}/remover-dois-fatores", null)).StatusCode);
        Assert.NotNull((await (await api.EntrarAsync(api.CreateClient(), email, Senha)).Content.ReadFromJsonAsync<ApiFactory.LoginResposta>())!.Token);
    }

    [Fact]
    public void CodigosDeRecuperacaoSaoUnicosEGastam()
    {
        var codigos = CodigosRecuperacao.Gerar();
        Assert.Equal(codigos.Count, codigos.Distinct().Count());
        var guardados = CodigosRecuperacao.Guardar(codigos);
        Assert.DoesNotContain(codigos[0], guardados); // só o hash fica guardado
        Assert.True(CodigosRecuperacao.TentarUsar(guardados, codigos[3], out var restantes));
        Assert.Equal(codigos.Count - 1, CodigosRecuperacao.Restantes(restantes));
        Assert.False(CodigosRecuperacao.TentarUsar(restantes, codigos[3], out _));
        Assert.False(CodigosRecuperacao.PareceCodigoDeRecuperacao("123456"));
    }
}
