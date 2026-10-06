using Escola.Api.Auth;
using System.Text;
using System.Text.Json.Serialization;
using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Escola.Infrastructure.Auditoria;
using Escola.Infrastructure.Auth;
using Escola.Infrastructure.Clientes;
using Escola.Infrastructure.Competicoes;
using Escola.Infrastructure.Data;
using Escola.Infrastructure.Email;
using Escola.Infrastructure.Financeiro;
using Escola.Infrastructure.Pagamentos;
using Escola.Api.Servicos;
using System.Security.Cryptography.X509Certificates;
using Escola.Infrastructure.Storage;
using Escola.Infrastructure.Tempo;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;

// Utilitário pra gerar o hash da senha da conta de Suporte (vai em Suporte:SenhaHash, na configuração do ambiente — nunca a senha em si).
// Sem argumento extra a senha é lida do teclado, pra não ficar no histórico do terminal.
if (args.Length >= 1 && args[0] == "--gerar-hash-senha")
{
    var senhaInformada = args.Length >= 2 ? args[1] : null;
    if (senhaInformada is null)
    {
        Console.Error.Write("Senha: ");
        senhaInformada = Console.ReadLine();
    }

    if (string.IsNullOrEmpty(senhaInformada) || senhaInformada.Length < 12)
    {
        Console.Error.WriteLine("Use uma senha de pelo menos 12 caracteres.");
        Environment.ExitCode = 1;
        return;
    }

    Console.WriteLine(SenhaHasher.Hash(senhaInformada));
    return;
}

// Utilitário pra gerar o segredo do segundo fator (TOTP) do Suporte: vai em Suporte:TotpSegredo e é cadastrado no app autenticador
// (Google/Microsoft Authenticator, Authy, 1Password…) digitando a chave ou lendo o link otpauth://. Um segredo serve pra todos os ambientes.
if (args.Length >= 1 && args[0] == "--gerar-segredo-totp")
{
    var conta = args.Length >= 2 ? args[1] : "suporte";
    var segredoNovo = Totp.GerarSegredo();
    Console.WriteLine($"Segredo (Suporte:TotpSegredo): {segredoNovo}");
    Console.WriteLine($"Link para o app autenticador:  {Totp.UriOtpAuth("Rotina Escola", conta, segredoNovo)}");
    Console.WriteLine("Guarde o segredo em local seguro (cofre de senhas): quem o tem gera os códigos.");
    return;
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Cliente (tenant) desta instalação: cada cliente tem o próprio site/login, e o banco é compartilhado. Vem da
// configuração do deploy (Cliente:Id); se não vier, usa o cliente padrão que herdou os dados anteriores ao multi-cliente.
var clienteId = builder.Configuration.GetValue<Guid?>("Cliente:Id") ?? ClientePadrao.Id;
var clienteNome = builder.Configuration["Cliente:Nome"] ?? "Cliente padrão";
// Só vale ao CRIAR a linha do cliente; depois o segmento é o que está no banco (Clientes.Segmento).
var clienteSegmentoTexto = builder.Configuration["Cliente:Segmento"];
if (!Enum.TryParse<SegmentoCliente>(clienteSegmentoTexto, ignoreCase: true, out var clienteSegmento))
{
    if (!string.IsNullOrWhiteSpace(clienteSegmentoTexto))
        throw new InvalidOperationException($"Cliente:Segmento inválido: '{clienteSegmentoTexto}'. Use 'Escola' ou 'Clube'.");
    clienteSegmento = SegmentoCliente.Escola;
}
builder.Services.AddSingleton<IClienteAtual>(new ClienteAtual(clienteId));

builder.Services.AddDbContext<EscolaDbContext>(options => options
    .UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
    // Todas as entidades têm o mesmo filtro por cliente, então o aviso de "extremidade obrigatória filtrada" é só ruído.
    .ConfigureWarnings(w => w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning)));

var jwtChave = builder.Configuration["Jwt:Chave"]
    ?? throw new InvalidOperationException("Configuração Jwt:Chave ausente.");
var jwtEmissor = builder.Configuration["Jwt:Emissor"] ?? "RotinaEscola";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtEmissor,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtChave)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async contexto =>
            {
                // Um token emitido pelo site de outro cliente (mesmo com a mesma chave Jwt) não vale aqui.
                var doToken = contexto.Principal?.FindFirst("clienteId")?.Value;
                if (doToken != clienteId.ToString())
                {
                    contexto.Fail("Token de outro cliente.");
                    return;
                }

                // O JWT dura dias e não dá pra "desemitir": a cada requisição confere no banco se a conta ainda existe, está ativa e não teve as
                // sessões revogadas depois da emissão deste token (desativar conta, trocar senha, "encerrar sessões"), e se o cliente não foi suspenso.
                var db = contexto.HttpContext.RequestServices.GetRequiredService<EscolaDbContext>();
                var motivo = await Autenticacao.MotivoDeRecusaAsync(db, contexto.Principal!, clienteId);
                if (motivo is not null) contexto.Fail(motivo);
            }
        };
    });
builder.Services.AddAuthorization();

const string FrontendCorsPolicy = "FrontendDev";
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var pastaUploads = Path.Combine(
    builder.Environment.WebRootPath ?? Path.Combine(builder.Environment.ContentRootPath, "wwwroot"),
    "uploads");
Directory.CreateDirectory(pastaUploads);
builder.Services.AddSingleton<IFotoStorage>(new LocalFotoStorage(pastaUploads));

// Fora de wwwroot de propósito: documentos de saúde nunca podem ser servidos como arquivo estático.
var pastaDocumentos = Path.Combine(builder.Environment.ContentRootPath, "dados-privados", "documentos-saude");
Directory.CreateDirectory(pastaDocumentos);
builder.Services.AddSingleton<IDocumentoStorage>(new LocalDocumentoStorage(pastaDocumentos));

builder.Services.Configure<ConfiguracaoSmtp>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IAuditoriaService, AuditoriaService>();
builder.Services.AddSingleton<LimitadorTentativasLogin>();
builder.Services.AddScoped<ILinkSenhaService, LinkSenhaService>();
builder.Services.AddScoped<IConviteMatriculaService, ConviteMatriculaService>();
builder.Services.AddScoped<IRelogioEscola, RelogioEscola>();
builder.Services.AddScoped<IBloqueioAlunoService, BloqueioAlunoService>();

// Baixa automática de Pix (API Pix do Banco do Brasil). Sem Pix:Bb:ClientId/ClientSecret/ChaveAplicacao fica desligada e o app
// segue gerando o Pix estático, com baixa manual — nada muda pra quem não configurar.
builder.Services.Configure<OpcoesPixBb>(builder.Configuration.GetSection(OpcoesPixBb.Secao));
var pixBb = builder.Configuration.GetSection(OpcoesPixBb.Secao).Get<OpcoesPixBb>() ?? new OpcoesPixBb();
builder.Services.AddSingleton<CacheTokenBb>();
builder.Services.AddHttpClient<IProvedorPix, BbPixClient>(cliente => cliente.Timeout = TimeSpan.FromSeconds(20))
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new SocketsHttpHandler();
        // O BB exige certificado de cliente (mTLS) em produção.
        if (!string.IsNullOrWhiteSpace(pixBb.CertificadoPfxCaminho))
            handler.SslOptions.ClientCertificates = [X509CertificateLoader.LoadPkcs12FromFile(pixBb.CertificadoPfxCaminho, pixBb.CertificadoPfxSenha)];
        return handler;
    });
builder.Services.AddScoped<IPixAutomaticoService, PixAutomaticoService>();
if (pixBb.Configurado) builder.Services.AddHostedService<ConciliacaoPixWorker>();
builder.Services.AddScoped<IDisciplinaService, DisciplinaService>();

var app = builder.Build();

// Aplica as migrations do banco ao iniciar — só quando pedido (Database:MigrarAoIniciar=true), pensado pro deploy em contêiner.
// Desligado por padrão: em dev as migrations rodam com 'dotnet ef database update'. Com vários clientes no mesmo banco, o EF
// serializa a migration com um lock do próprio banco, então dois deploys subindo juntos não se atropelam.
if (builder.Configuration.GetValue<bool>("Database:MigrarAoIniciar"))
{
    using var escopoMigracao = app.Services.CreateScope();
    await escopoMigracao.ServiceProvider.GetRequiredService<EscolaDbContext>().Database.MigrateAsync();
}

// Garante a linha do cliente desta instalação (idempotente) — é o que permite provisionar um cliente novo só
// configurando Cliente:Id/Cliente:Nome no deploy. Todas as demais tabelas apontam pra ela.
using (var escopoCliente = app.Services.CreateScope())
{
    var dbCliente = escopoCliente.ServiceProvider.GetRequiredService<EscolaDbContext>();
    if (!await dbCliente.Clientes.AnyAsync(c => c.Id == clienteId))
    {
        dbCliente.Clientes.Add(new Cliente { Id = clienteId, Nome = clienteNome, Segmento = clienteSegmento, CriadoEm = DateTime.UtcNow });
        await dbCliente.SaveChangesAsync();
    }
}

// Conta de Suporte (equipe do produto) deste cliente: vem da configuração do deploy; sem ela, fica desativada.
using (var escopoSuporte = app.Services.CreateScope())
{
    var dbSuporte = escopoSuporte.ServiceProvider.GetRequiredService<EscolaDbContext>();
    await SuporteProvisionador.GarantirAsync(dbSuporte, builder.Configuration["Suporte:Email"], builder.Configuration["Suporte:Nome"],
        builder.Configuration["Suporte:SenhaHash"], builder.Configuration["Suporte:TotpSegredo"], app.Services.GetRequiredService<ILogger<Program>>());
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<EscolaDbContext>();
    await DbInitializer.SeedAsync(db);
    await DbInitializer.GarantirAcessosAsync(db);
}

// Atrás de proxy (HTTPS externo → nginx do site → API) o IP do cliente chega no X-Forwarded-For. Só é lido quando a configuração
// diz quantos proxies há na frente (App:ProxiesNaFrente) — sem isso qualquer um forjaria o cabeçalho. O IP vai pra prova do aceite do termo.
var proxiesNaFrente = builder.Configuration.GetValue<int>("App:ProxiesNaFrente");
if (proxiesNaFrente > 0)
{
    var opcoesProxy = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
        ForwardLimit = proxiesNaFrente
    };
    opcoesProxy.KnownNetworks.Clear(); // os proxies estão na rede do Docker/host, cujo endereço varia por instalação
    opcoesProxy.KnownProxies.Clear();
    app.UseForwardedHeaders(opcoesProxy);
}

app.UseHttpsRedirection();

app.UseCors(FrontendCorsPolicy);

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Verificação de saúde pro balanceador/contêiner: anônima e sem detalhe nenhum (só "no ar e falando com o banco", ou não).
app.MapGet("/healthz", async (EscolaDbContext db) =>
    await db.Database.CanConnectAsync() ? Results.Ok(new { status = "ok" }) : Results.StatusCode(StatusCodes.Status503ServiceUnavailable));

app.Run();

// Necessário pra os testes de integração (WebApplicationFactory<Program>) enxergarem o ponto de entrada.
public partial class Program;
