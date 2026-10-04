using System.Text;
using System.Text.Json.Serialization;
using Escola.Domain.Entities;
using Escola.Infrastructure.Auditoria;
using Escola.Infrastructure.Clientes;
using Escola.Infrastructure.Competicoes;
using Escola.Infrastructure.Data;
using Escola.Infrastructure.Email;
using Escola.Infrastructure.Financeiro;
using Escola.Infrastructure.Storage;
using Escola.Infrastructure.Tempo;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;

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

        // Um token emitido pelo site de outro cliente (mesmo com a mesma chave Jwt) não vale aqui.
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = contexto =>
            {
                var doToken = contexto.Principal?.FindFirst("clienteId")?.Value;
                if (doToken != clienteId.ToString()) contexto.Fail("Token de outro cliente.");
                return Task.CompletedTask;
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
builder.Services.AddScoped<IRelogioEscola, RelogioEscola>();
builder.Services.AddScoped<IBloqueioAlunoService, BloqueioAlunoService>();
builder.Services.AddScoped<IDisciplinaService, DisciplinaService>();

var app = builder.Build();

// Garante a linha do cliente desta instalação (idempotente) — é o que permite provisionar um cliente novo só
// configurando Cliente:Id/Cliente:Nome no deploy. Todas as demais tabelas apontam pra ela.
using (var escopoCliente = app.Services.CreateScope())
{
    var dbCliente = escopoCliente.ServiceProvider.GetRequiredService<EscolaDbContext>();
    if (!await dbCliente.Clientes.AnyAsync(c => c.Id == clienteId))
    {
        dbCliente.Clientes.Add(new Cliente { Id = clienteId, Nome = clienteNome, CriadoEm = DateTime.UtcNow });
        await dbCliente.SaveChangesAsync();
    }
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

app.UseHttpsRedirection();

app.UseCors(FrontendCorsPolicy);

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
