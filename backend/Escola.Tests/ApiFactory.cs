using System.Net.Http.Headers;
using System.Net.Http.Json;
using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Escola.Infrastructure.Auth;
using Escola.Infrastructure.Clientes;
using Escola.Infrastructure.Data;
using Escola.Infrastructure.Email;
using Escola.Infrastructure.Pagamentos.Asaas;
using Escola.Infrastructure.Seguranca;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Escola.Tests;

/// <summary>E-mail falso: guarda o que "foi enviado" (pra ler o link do convite) e finge estar configurado.</summary>
public sealed class EmailFalso : IEmailSender
{
    public bool Configurado => true;
    public List<(string Para, string Assunto, string Corpo)> Enviados { get; } = [];

    public Task EnviarAsync(string destinatarioEmail, string destinatarioNome, string assunto, string corpoHtml, CancellationToken ct = default)
    {
        lock (Enviados) Enviados.Add((destinatarioEmail, assunto, corpoHtml));
        return Task.CompletedTask;
    }
}

/// <summary>A API de verdade (controllers, autenticação JWT, autorização, filtro por cliente) sobre um SQLite em memória —
/// o único trecho trocado é o banco e o envio de e-mail. Cada factory = um banco novo.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public static readonly Guid ClienteId = Guid.Parse("00000000-0000-0000-0000-0000000000c1");
    public const string JwtChave = "chave-so-pra-teste-com-mais-de-32-caracteres!!";
    public const string SuporteEmail = "suporte@rotinaescola.test";
    public const string SuporteSenha = "SenhaDoSuporte-2026!";
    public static readonly string SuporteTotp = Totp.GerarSegredo();

    private readonly SqliteConnection _conexao = new("DataSource=:memory:");
    private readonly DbContextOptions<EscolaDbContext> _opcoesBanco;
    public EmailFalso Email { get; } = new();
    public AsaasFalso Asaas { get; } = new();
    public const string AsaasWebhookToken = "token-do-webhook-de-teste";
    private static readonly string ChaveSegredos = CofreSegredos.GerarChave();

    public ApiFactory()
    {
        _conexao.Open();
        _opcoesBanco = new DbContextOptionsBuilder<EscolaDbContext>().UseSqlite(_conexao)
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning))
            .Options;
        using var db = Contexto(ClienteId);
        db.Database.EnsureCreated();
        db.Clientes.Add(new Cliente { Id = ClienteId, Nome = "Cliente de Teste", CriadoEm = DateTime.UtcNow });
        db.SaveChanges();
    }

    public EscolaDbContext Contexto(Guid clienteId) => new(_opcoesBanco, new ClienteAtual(clienteId));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing"); // fora de Development: nada de seed automático nem OpenAPI

        // UseSetting (e não ConfigureAppConfiguration): o Program lê Jwt:Chave logo no começo do Main, antes de a configuração
        // adicionada depois chegar ao WebApplicationBuilder.
        var config = new Dictionary<string, string>
        {
            ["Jwt:Chave"] = JwtChave,
            ["Jwt:Emissor"] = "RotinaEscola",
            ["Cliente:Id"] = ClienteId.ToString(),
            ["Cliente:Nome"] = "Cliente de Teste",
            ["App:UrlBase"] = "https://escola.teste",
            ["Suporte:Email"] = SuporteEmail,
            ["Suporte:Nome"] = "Suporte Teste",
            ["Suporte:SenhaHash"] = SenhaHasher.Hash(SuporteSenha),
            ["Suporte:TotpSegredo"] = SuporteTotp,
            ["Segredos:Chave"] = ChaveSegredos,
            ["Asaas:ApiKey"] = "$aact_hmlg_conta_raiz_de_teste",
            ["Asaas:WebhookToken"] = AsaasWebhookToken,
            ["Asaas:WebhookUrlBase"] = "https://escola.teste",
        };
        foreach (var (chave, valor) in config) builder.UseSetting(chave, valor);

        builder.ConfigureTestServices(services =>
        {
            foreach (var d in services.Where(d => d.ServiceType == typeof(DbContextOptions<EscolaDbContext>)
                         || d.ServiceType.IsGenericType && d.ServiceType.GetGenericArguments().Contains(typeof(EscolaDbContext))).ToList())
                services.Remove(d);

            services.AddDbContext<EscolaDbContext>(o => o.UseSqlite(_conexao)
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning)));

            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Email);

            // Nada sai pra rede: o HttpClient do Asaas fala com o falso.
            services.AddHttpClient<AsaasApi>().ConfigurePrimaryHttpMessageHandler(() => Asaas.NovoHandler());
        });
    }

    // ----- ajudantes dos testes -----

    public Usuario CriarUsuario(PapelUsuario papel, string email, string senha = "Senha-de-Teste-1", bool ativo = true, Guid? clienteId = null)
    {
        using var db = Contexto(clienteId ?? ClienteId);
        var usuario = new Usuario { Id = Guid.NewGuid(), Nome = $"{papel} Teste", Email = email, SenhaHash = SenhaHasher.Hash(senha), Papel = papel, Ativo = ativo };
        db.Usuarios.Add(usuario);
        db.SaveChanges();
        return usuario;
    }

    public async Task<HttpResponseMessage> EntrarAsync(HttpClient http, string email, string senha, string? codigo = null) =>
        await http.PostAsJsonAsync("/api/auth/entrar", new { email, senha, codigo });

    /// <summary>Cliente HTTP já autenticado como um usuário comum (cria a conta e faz o login de verdade).</summary>
    public async Task<HttpClient> ClienteLogadoAsync(PapelUsuario papel, string? email = null)
    {
        email ??= $"{papel.ToString().ToLowerInvariant()}-{Guid.NewGuid():N}@teste.com";
        CriarUsuario(papel, email);
        var http = CreateClient();
        var resposta = await EntrarAsync(http, email, "Senha-de-Teste-1");
        resposta.EnsureSuccessStatusCode();
        var corpo = await resposta.Content.ReadFromJsonAsync<LoginResposta>();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", corpo!.Token);
        return http;
    }

    private long _ultimoPassoSuporte;

    /// <summary>Código TOTP do Suporte que nunca repete um passo já usado (o app recusa reuso). Cada passo é de 30 s e o app aceita ±1.</summary>
    public string ProximoCodigoSuporte()
    {
        lock (this)
        {
            var passo = Math.Max(Totp.PassoAtual(DateTime.UtcNow), _ultimoPassoSuporte + 1);
            _ultimoPassoSuporte = passo;
            return Totp.Codigo(SuporteTotp, passo);
        }
    }

    private string? _tokenSuporte;
    private readonly SemaphoreSlim _trava = new(1, 1);

    /// <summary>Cliente HTTP logado como Suporte. O login (senha + 2FA) é feito uma vez por fábrica e reaproveitado: cada login gasta
    /// um passo TOTP e o app aceita só ±1 passo, então vários logins seguidos no mesmo teste estourariam a janela.</summary>
    public async Task<HttpClient> SuporteLogadoAsync()
    {
        await _trava.WaitAsync();
        try
        {
            if (_tokenSuporte is null)
            {
                var resposta = await EntrarAsync(CreateClient(), SuporteEmail, SuporteSenha, ProximoCodigoSuporte());
                resposta.EnsureSuccessStatusCode();
                _tokenSuporte = (await resposta.Content.ReadFromJsonAsync<LoginResposta>())!.Token;
            }
        }
        finally { _trava.Release(); }

        var http = CreateClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _tokenSuporte);
        return http;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _conexao.Dispose();
    }

    public sealed record LoginResposta(string Token, Guid UsuarioId, string Nome, string Papel, Guid? ResponsavelId, bool RequerSegundoFator);
}
