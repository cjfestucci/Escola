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

    /// <summary>O código vai no assunto do e-mail ("123456 é seu código…"): pega o do último e-mail mandado pra essa conta.</summary>
    private string UltimoCodigoPorEmail(string email, int enviadosAntes)
    {
        lock (api.Email.Enviados)
        {
            var envios = api.Email.Enviados.Where(e => e.Para == email).ToList();
            Assert.True(envios.Count > enviadosAntes, "Nenhum código novo foi enviado.");
            return envios[^1].Assunto[..6];
        }
    }

    private int EnviosPara(string email)
    {
        lock (api.Email.Enviados) return api.Email.Enviados.Count(e => e.Para == email);
    }

    [Fact]
    public async Task CodigoPorEmailAtivaPedeOCodigoNoLoginVaseUmaVezENaoReenviaNaHora()
    {
        var email = $"coordenacao.{Guid.NewGuid():N}@escola.test";
        api.CriarUsuario(PapelUsuario.Coordenador, email);
        var http = await LogadoAsync(email);

        // Ativar: manda um código pro próprio e-mail e confirma com ele.
        var antes = EnviosPara(email);
        var inicio = await (await http.PostAsync("/api/minha-conta/dois-fatores/email/iniciar", null)).Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.StartsWith("c***", inicio.GetProperty("emailMascarado").GetString());
        var codigo = UltimoCodigoPorEmail(email, antes);
        Assert.Equal(HttpStatusCode.BadRequest, (await http.PostAsJsonAsync("/api/minha-conta/dois-fatores/email/confirmar",
            new { codigo = codigo == "000000" ? "111111" : "000000" })).StatusCode);
        var confirmacao = await http.PostAsJsonAsync("/api/minha-conta/dois-fatores/email/confirmar", new { codigo });
        confirmacao.EnsureSuccessStatusCode();
        var recuperacao = (await confirmacao.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("codigos").EnumerateArray().Select(c => c.GetString()!).ToList();
        var estado = await http.GetFromJsonAsync<JsonElement>("/api/minha-conta/dois-fatores", Json);
        Assert.Equal("Email", estado.GetProperty("metodo").GetString());

        // Login: senha certa sem código manda o código por e-mail e não emite token.
        antes = EnviosPara(email);
        var semCodigo = await (await api.EntrarAsync(api.CreateClient(), email, Senha)).Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.True(semCodigo.GetProperty("requerSegundoFator").GetBoolean());
        Assert.Equal("Email", semCodigo.GetProperty("metodoSegundoFator").GetString());
        Assert.True(string.IsNullOrEmpty(semCodigo.GetProperty("token").GetString()));
        var codigoLogin = UltimoCodigoPorEmail(email, antes);

        // Pedir de novo logo em seguida não manda outro e-mail (o anterior continua valendo).
        var deNovo = await (await api.EntrarAsync(api.CreateClient(), email, Senha)).Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Contains("aguarde", deNovo.GetProperty("aviso").GetString());
        Assert.Equal(antes + 1, EnviosPara(email));

        Assert.Equal(HttpStatusCode.Unauthorized, (await api.EntrarAsync(api.CreateClient(), email, Senha, codigoLogin == "123456" ? "654321" : "123456")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await api.EntrarAsync(api.CreateClient(), email, Senha, codigoLogin)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.EntrarAsync(api.CreateClient(), email, Senha, codigoLogin)).StatusCode); // uso único

        // Código de recuperação também entra.
        Assert.Equal(HttpStatusCode.OK, (await api.EntrarAsync(api.CreateClient(), email, Senha, recuperacao[0])).StatusCode);

        // Desativar: pede um código novo e confirma com senha + código.
        antes = EnviosPara(email);
        (await http.PostAsync("/api/minha-conta/dois-fatores/email/enviar-codigo", null)).EnsureSuccessStatusCode();
        var codigoDesativar = UltimoCodigoPorEmail(email, antes);
        var desativada = await http.PostAsJsonAsync("/api/minha-conta/dois-fatores/desativar", new { senha = Senha, codigo = codigoDesativar });
        desativada.EnsureSuccessStatusCode();
        Assert.False((await desativada.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("ativo").GetBoolean());
        Assert.NotNull((await (await api.EntrarAsync(api.CreateClient(), email, Senha)).Content.ReadFromJsonAsync<ApiFactory.LoginResposta>())!.Token);
    }

    private async Task<JsonElement> EntrarComAparelhoAsync(string email, string senha, string? codigo = null, string[]? tokens = null, bool lembrar = false)
    {
        var resposta = await api.CreateClient().PostAsJsonAsync("/api/auth/entrar",
            new { email, senha, codigo, tokensAparelho = tokens, lembrarAparelho = lembrar });
        return resposta.StatusCode == HttpStatusCode.OK
            ? await resposta.Content.ReadFromJsonAsync<JsonElement>(Json)
            : JsonSerializer.SerializeToElement(new { status = (int)resposta.StatusCode });
    }

    private static bool Entrou(JsonElement r) => r.TryGetProperty("token", out var t) && !string.IsNullOrEmpty(t.GetString());

    [Fact]
    public async Task LembrarOAparelhoPulaOCodigoPor30DiasMasNuncaASenha()
    {
        var (email, id, segredo, recuperacao) = await ContaComDoisFatoresAsync(PapelUsuario.Coordenador);

        // Sem marcar "lembrar": nada de token de aparelho.
        var semLembrar = await EntrarComAparelhoAsync(email, Senha, recuperacao[0]);
        Assert.True(Entrou(semLembrar));
        Assert.True(semLembrar.GetProperty("tokenAparelho").ValueKind == JsonValueKind.Null);

        var lembrado = await EntrarComAparelhoAsync(email, Senha, CodigoAgora(segredo), lembrar: true);
        var tokenAparelho = lembrado.GetProperty("tokenAparelho").GetString()!;
        Assert.False(string.IsNullOrEmpty(tokenAparelho));

        // Com o token: entra só com a senha. Senha errada continua barrada; token inventado não pula nada.
        Assert.True(Entrou(await EntrarComAparelhoAsync(email, Senha, tokens: [tokenAparelho])));
        Assert.Equal(401, (await EntrarComAparelhoAsync(email, "senha-errada", tokens: [tokenAparelho])).GetProperty("status").GetInt32());
        Assert.True((await EntrarComAparelhoAsync(email, Senha, tokens: ["token-inventado"])).GetProperty("requerSegundoFator").GetBoolean());

        // O token de uma conta não serve pra outra.
        var (outroEmail, _, _, _) = await ContaComDoisFatoresAsync(PapelUsuario.Educador);
        Assert.True((await EntrarComAparelhoAsync(outroEmail, Senha, tokens: [tokenAparelho])).GetProperty("requerSegundoFator").GetBoolean());

        var http = await LogadoAsync(email, recuperacao[1]);
        Assert.Equal(1, (await http.GetFromJsonAsync<JsonElement>("/api/minha-conta/dois-fatores", Json)).GetProperty("aparelhosLembrados").GetInt32());

        // "Encerrar sessões" (Gestão) também esquece os aparelhos.
        var admin = await api.ClienteLogadoAsync(PapelUsuario.Admin);
        (await admin.PostAsync($"/api/usuarios/contas/{id}/encerrar-sessoes", null)).EnsureSuccessStatusCode();
        Assert.True((await EntrarComAparelhoAsync(email, Senha, tokens: [tokenAparelho])).GetProperty("requerSegundoFator").GetBoolean());

        // Lembrado de novo, e esquecido pela própria pessoa em Minha conta.
        var novo = (await EntrarComAparelhoAsync(email, Senha, recuperacao[2], lembrar: true)).GetProperty("tokenAparelho").GetString()!;
        Assert.True(Entrou(await EntrarComAparelhoAsync(email, Senha, tokens: [novo])));
        http = await LogadoAsync(email, recuperacao[3]);
        var depois = await (await http.PostAsync("/api/minha-conta/dois-fatores/esquecer-aparelhos", null)).Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal(0, depois.GetProperty("aparelhosLembrados").GetInt32());
        Assert.True((await EntrarComAparelhoAsync(email, Senha, tokens: [novo])).GetProperty("requerSegundoFator").GetBoolean());
    }

    [Fact]
    public async Task ErrarOCodigoMuitasVezesEmMinhaContaBloqueia()
    {
        var email = $"financeiro.{Guid.NewGuid():N}@escola.test";
        api.CriarUsuario(PapelUsuario.Financeiro, email);
        var http = await LogadoAsync(email);
        (await http.PostAsync("/api/minha-conta/dois-fatores/email/iniciar", null)).EnsureSuccessStatusCode();
        var codigo = UltimoCodigoPorEmail(email, 0);
        var errado = codigo == "000000" ? "111111" : "000000";

        for (var i = 0; i < 8; i++)
            Assert.Equal(HttpStatusCode.BadRequest, (await http.PostAsJsonAsync("/api/minha-conta/dois-fatores/email/confirmar", new { codigo = errado })).StatusCode);
        // Bloqueado: nem o código certo passa até o tempo acabar.
        Assert.Equal(HttpStatusCode.TooManyRequests, (await http.PostAsJsonAsync("/api/minha-conta/dois-fatores/email/confirmar", new { codigo })).StatusCode);
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
