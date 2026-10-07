using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Escola.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Escola.Tests;

/// <summary>O fuso horário saiu de Configurações → Geral: só o Suporte muda, pela Plataforma.</summary>
public class FusoHorarioHttpTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    private async Task<string?> FusoNoBancoAsync()
    {
        using var db = api.Contexto(ApiFactory.ClienteId);
        return await db.ConfiguracoesEscola.Select(c => c.FusoHorario).FirstOrDefaultAsync();
    }

    [Fact]
    public async Task GestaoNaoMudaOFusoPelaConfiguracaoGeral()
    {
        var suporte = await api.SuporteLogadoAsync();
        (await suporte.PutAsJsonAsync("/api/plataforma/fuso", new { fusoHorario = "America/Sao_Paulo" })).EnsureSuccessStatusCode();

        var gestao = await api.ClienteLogadoAsync(PapelUsuario.Admin);
        var resposta = await gestao.PutAsJsonAsync("/api/configuracao/escola", new { fusoHorario = "America/Manaus", corPrincipal = "#2563EB" });
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        Assert.Equal("America/Sao_Paulo", await FusoNoBancoAsync()); // o campo enviado foi ignorado
        Assert.Equal(HttpStatusCode.Forbidden, (await gestao.PutAsJsonAsync("/api/plataforma/fuso", new { fusoHorario = "America/Manaus" })).StatusCode);
    }

    [Fact]
    public async Task SuporteMudaOFusoPelaPlataformaComHistorico()
    {
        var suporte = await api.SuporteLogadoAsync();

        Assert.Equal(HttpStatusCode.BadRequest, (await suporte.PutAsJsonAsync("/api/plataforma/fuso", new { fusoHorario = "Marte/Olympus" })).StatusCode);

        var resposta = await suporte.PutAsJsonAsync("/api/plataforma/fuso", new { fusoHorario = "America/Manaus" });
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>(new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal("America/Manaus", corpo.GetProperty("fusoHorario").GetString());
        Assert.Equal("America/Manaus", await FusoNoBancoAsync());

        using var db = api.Contexto(ApiFactory.ClienteId);
        Assert.True(await db.LogsAuditoria.AnyAsync(l => l.EntidadeTipo == "ConfiguracaoEscola" && l.Detalhe != null && l.Detalhe.Contains("America/Manaus")));

        // Volta ao padrão pra não influenciar outros testes da mesma fábrica.
        (await suporte.PutAsJsonAsync("/api/plataforma/fuso", new { fusoHorario = "America/Sao_Paulo" })).EnsureSuccessStatusCode();
    }
}
