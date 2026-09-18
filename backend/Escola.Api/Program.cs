using System.Text.Json.Serialization;
using Escola.Infrastructure.Data;
using Escola.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<EscolaDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<EscolaDbContext>();
    await DbInitializer.SeedAsync(db);
}

app.UseHttpsRedirection();

app.UseCors(FrontendCorsPolicy);

app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.Run();
