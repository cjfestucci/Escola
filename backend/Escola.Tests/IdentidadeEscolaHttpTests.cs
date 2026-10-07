using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Escola.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Escola.Tests;

/// <summary>Nome, fuso e logo da escola: o Admin configura em Configurações → Geral (o Coordenador só mexe na cor).</summary>
public class IdentidadeEscolaHttpTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // PNG mínimo (assinatura + bytes): o tipo é validado pelo conteúdo.
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0x0D, 0x49, 0x48, 0x44, 0x52];

    [Fact]
    public async Task AdminMudaNomeEFusoComHistorico()
    {
        var admin = await api.ClienteLogadoAsync(PapelUsuario.Admin);

        var resposta = await admin.PutAsJsonAsync("/api/configuracao/escola/dados", new { nomeEscola = "Escola Renomeada FC", fusoHorario = "America/Manaus" });
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal("Escola Renomeada FC", corpo.GetProperty("nomeEscola").GetString());
        Assert.Equal("America/Manaus", corpo.GetProperty("fusoHorario").GetString());

        using (var db = api.Contexto(ApiFactory.ClienteId))
        {
            Assert.Equal("Escola Renomeada FC", (await db.Clientes.FirstAsync(c => c.Id == ApiFactory.ClienteId)).Nome);
            Assert.True(await db.LogsAuditoria.AnyAsync(l => l.EntidadeTipo == "ConfiguracaoEscola" && l.Detalhe != null && l.Detalhe.Contains("Nome da escola alterado")));
        }

        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync("/api/configuracao/escola/dados", new { nomeEscola = " ", fusoHorario = "America/Manaus" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync("/api/configuracao/escola/dados", new { nomeEscola = "X", fusoHorario = "Marte/Olympus" })).StatusCode);

        // Volta ao padrão pros outros testes da mesma fábrica.
        (await admin.PutAsJsonAsync("/api/configuracao/escola/dados", new { nomeEscola = "Cliente de Teste", fusoHorario = "America/Sao_Paulo" })).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CoordenadorNaoMudaNomeFusoNemLogoMasMudaACor()
    {
        var coordenador = await api.ClienteLogadoAsync(PapelUsuario.Coordenador);

        Assert.Equal(HttpStatusCode.Forbidden, (await coordenador.PutAsJsonAsync("/api/configuracao/escola/dados", new { nomeEscola = "Tentativa", fusoHorario = "America/Manaus" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await coordenador.DeleteAsync("/api/configuracao/escola/logo")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await coordenador.PutAsJsonAsync("/api/configuracao/escola", new { corPrincipal = (string?)null })).StatusCode);
    }

    [Fact]
    public async Task AdminEnviaERemoveALogoValidadaPeloConteudo()
    {
        var admin = await api.ClienteLogadoAsync(PapelUsuario.Admin);

        var html = new MultipartFormDataContent { { new ByteArrayContent("<html>oi</html>"u8.ToArray()) { Headers = { ContentType = new MediaTypeHeaderValue("image/png") } }, "arquivo", "logo.png" } };
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsync("/api/configuracao/escola/logo", html)).StatusCode);

        var png = new MultipartFormDataContent { { new ByteArrayContent(Png) { Headers = { ContentType = new MediaTypeHeaderValue("image/png") } }, "arquivo", "logo.png" } };
        var enviada = await admin.PutAsync("/api/configuracao/escola/logo", png);
        Assert.Equal(HttpStatusCode.OK, enviada.StatusCode);
        Assert.False(string.IsNullOrEmpty((await enviada.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("logoUrl").GetString()));

        Assert.Equal(HttpStatusCode.OK, (await admin.DeleteAsync("/api/configuracao/escola/logo")).StatusCode);
    }
}
