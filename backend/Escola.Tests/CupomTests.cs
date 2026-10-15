using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Escola.Infrastructure.Assinaturas;
using Escola.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Escola.Api.Servicos;

namespace Escola.Tests;

public class CupomRegrasTests
{
    private static readonly DateOnly Hoje = new(2026, 10, 20);

    [Fact]
    public void DescontoValeNasPrimeirasMensalidadesOuSempre()
    {
        Assert.True(CupomRegras.DescontoVigente(20, 3, 0));
        Assert.True(CupomRegras.DescontoVigente(20, 3, 2));
        Assert.False(CupomRegras.DescontoVigente(20, 3, 3));
        Assert.True(CupomRegras.DescontoVigente(20, null, 50));
        Assert.False(CupomRegras.DescontoVigente(null, null, 0));
        Assert.Equal(175.20m, CupomRegras.AplicarDesconto(219m, 20));
        Assert.Equal(0m, CupomRegras.AplicarDesconto(219m, 100));
    }

    [Fact]
    public void CupomInvalidoExpiradoOuEsgotado()
    {
        var cupom = new Cupom { Codigo = "X", PercentualDesconto = 10, Ativo = true, ValidoAte = Hoje, UsosMaximos = 2 };
        Assert.Null(CupomRegras.MotivoInvalido(cupom, Hoje, 1));
        Assert.NotNull(CupomRegras.MotivoInvalido(cupom, Hoje.AddDays(1), 1)); // expirou
        Assert.NotNull(CupomRegras.MotivoInvalido(cupom, Hoje, 2)); // esgotou
        Assert.NotNull(CupomRegras.MotivoInvalido(new Cupom { Ativo = false, PercentualDesconto = 10 }, Hoje, 0));
        Assert.NotNull(CupomRegras.MotivoInvalido(null, Hoje, 0));
        Assert.Equal("LANCAMENTO-20", CupomRegras.Normalizar(" lancamento-20 "));
    }

    [Fact]
    public void MesesGratisEstendemOTesteEAparecemNaDescricao()
    {
        Assert.Equal(new DateOnly(2027, 1, 20), CupomRegras.FimDoTeste(Hoje, 7, 3));
        Assert.Equal(Hoje.AddDays(7), CupomRegras.FimDoTeste(Hoje, 7, null));
        Assert.Equal(new DateOnly(2026, 11, 20), CupomRegras.FimDoTeste(Hoje, 0, 1)); // mesmo sem teste no plano
        Assert.Null(CupomRegras.FimDoTeste(Hoje, 0, null));

        Assert.Equal("3 meses grátis", CupomRegras.Descricao(0, null, 3));
        Assert.Equal("1 mês grátis", CupomRegras.Descricao(0, null, 1));
        Assert.Equal("3 meses grátis e depois 50% de desconto nas 3 primeiras mensalidades", CupomRegras.Descricao(50, 3, 3));
        Assert.Equal("20% de desconto na 1ª mensalidade", CupomRegras.Descricao(20, 1));
    }
}

/// <summary>Cupom de meses grátis (clube piloto): o teste dura os meses do cupom e a 1ª fatura, cheia, só vence no fim deles.</summary>
public class CupomMesesGratisHttpTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task TresMesesGratisViramTesteDeTresMesesSemFaturaDeZero()
    {
        var suporte = await api.SuporteLogadoAsync();

        // 100% é recusado (fatura de R$ 0 o Asaas não aceita); cupom sem desconto e sem meses grátis também.
        Assert.Equal(HttpStatusCode.BadRequest, (await suporte.PostAsJsonAsync("/api/plataforma/cupons", new { codigo = "TUDO100", percentualDesconto = 100 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await suporte.PostAsJsonAsync("/api/plataforma/cupons", new { codigo = "NADA", percentualDesconto = 0 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await suporte.PostAsJsonAsync("/api/plataforma/cupons", new { codigo = "GRATIS13", percentualDesconto = 0, mesesGratis = 13 })).StatusCode);

        var criacao = await suporte.PostAsJsonAsync("/api/plataforma/cupons", new { codigo = "piloto", percentualDesconto = 0, mesesGratis = 3, meses = 5, usosMaximos = 1 });
        criacao.EnsureSuccessStatusCode();
        var criado = await criacao.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal("3 meses grátis", criado.GetProperty("descricao").GetString());
        Assert.Equal(JsonValueKind.Null, criado.GetProperty("meses").ValueKind); // sem desconto, "por quantas mensalidades" não vale

        var anonimo = api.CreateClient();
        var conferido = await anonimo.GetFromJsonAsync<JsonElement>("/api/assinaturas/cupom/PILOTO", Json);
        Assert.Equal(3, conferido.GetProperty("mesesGratis").GetInt32());

        var email = $"piloto.{Guid.NewGuid():N}@clube.test";
        var resposta = await anonimo.PostAsJsonAsync("/api/assinaturas", new
        {
            nomeClube = "Clube Piloto", cpfCnpj = "11222333000181", cidade = "Campinas", atletas = 40,
            nomeAdmin = "Dono", email, celular = "(19) 99999-8888", aceiteTermos = true, cupom = "piloto"
        });
        resposta.EnsureSuccessStatusCode();
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal(140m, corpo.GetProperty("valorMensal").GetDecimal()); // valor cheio: 60 + 4 × (40 − 20)

        using var db = api.Contexto(ApiFactory.ClienteId);
        var a = await db.Assinaturas.IgnoreQueryFilters().SingleAsync(x => x.EmailCobranca == email);
        var hoje = DateOnly.Parse(corpo.GetProperty("testeAte").GetString()!).AddMonths(-3);
        Assert.Equal(SituacaoAssinatura.EmTeste, a.Situacao);
        Assert.Equal(hoje.AddMonths(3), a.TesteAte);
        Assert.InRange(hoje.DayNumber, DateOnly.FromDateTime(DateTime.Today).DayNumber - 1, DateOnly.FromDateTime(DateTime.Today).DayNumber + 1);
        Assert.Equal(3, a.CupomMesesGratis);

        // A 1ª fatura do gateway vence no fim dos 3 meses, com o valor cheio.
        var noGateway = api.Asaas.Chamadas.Last(c => c.Metodo == "POST" && c.Caminho == "/subscriptions");
        Assert.Equal(a.TesteAte!.Value.ToString("yyyy-MM-dd"), noGateway.Corpo!["nextDueDate"]!.GetValue<string>());
        Assert.Equal(140m, noGateway.Corpo!["value"]!.GetValue<decimal>());
    }
}

/// <summary>Cupom de ponta a ponta: o Suporte cria, o visitante confere e assina com desconto, e o desconto acaba depois dos meses do cupom.</summary>
public class CupomHttpTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static object Cadastro(string email, string? cupom) => new
    {
        nomeClube = "Clube Do Cupom", cpfCnpj = "11222333000181", cidade = "Campinas", atletas = 40,
        nomeAdmin = "Dono", email, celular = "(19) 99999-8888", aceiteTermos = true, cupom
    };

    [Fact]
    public async Task CupomDaDescontoNasPrimeirasMensalidadesENaoSeUsaAlemDoLimite()
    {
        var suporte = await api.SuporteLogadoAsync(); // antes de qualquer cadastro (depois, o login do Suporte pediria a escola)
        var admin = await api.ClienteLogadoAsync(PapelUsuario.Admin);
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.PostAsJsonAsync("/api/plataforma/cupons", new { codigo = "X", percentualDesconto = 10 })).StatusCode);

        var criacao = await suporte.PostAsJsonAsync("/api/plataforma/cupons", new { codigo = "lancamento20", percentualDesconto = 20, meses = 1, usosMaximos = 1 });
        criacao.EnsureSuccessStatusCode();
        Assert.Equal("LANCAMENTO20", (await criacao.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("codigo").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await suporte.PostAsJsonAsync("/api/plataforma/cupons", new { codigo = "LANCAMENTO20", percentualDesconto = 5 })).StatusCode);

        var anonimo = api.CreateClient();
        var conferido = await anonimo.GetFromJsonAsync<JsonElement>("/api/assinaturas/cupom/lancamento20", Json);
        Assert.Equal(20, conferido.GetProperty("percentualDesconto").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound, (await anonimo.GetAsync("/api/assinaturas/cupom/NAOEXISTE")).StatusCode);

        // Assina com o cupom: 60 + 4 × (40 − 20) = 140, com 20% = 112 — também na assinatura do gateway.
        var email = $"dono.{Guid.NewGuid():N}@clube.test";
        var resposta = await anonimo.PostAsJsonAsync("/api/assinaturas", Cadastro(email, "lancamento20"));
        resposta.EnsureSuccessStatusCode();
        Assert.Equal(112m, (await resposta.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("valorMensal").GetDecimal());
        var criadaNoGateway = api.Asaas.Chamadas.Last(c => c.Metodo == "POST" && c.Caminho == "/subscriptions");
        Assert.Equal(112m, criadaNoGateway.Corpo!["value"]!.GetValue<decimal>());

        // Limite de 1 uso: o próximo cadastro com o cupom é recusado.
        Assert.Equal(HttpStatusCode.BadRequest, (await anonimo.PostAsJsonAsync("/api/assinaturas", Cadastro($"outro.{Guid.NewGuid():N}@clube.test", "LANCAMENTO20"))).StatusCode);
        var cupons = await suporte.GetFromJsonAsync<JsonElement>("/api/plataforma/cupons", Json);
        Assert.Equal(1, cupons.EnumerateArray().Single(c => c.GetProperty("codigo").GetString() == "LANCAMENTO20").GetProperty("usos").GetInt32());

        // Pagou a 1ª (o cupom vale 1 mês): a próxima volta ao valor cheio (R$ 60: nenhum atleta ativo, dentro dos 20 incluídos).
        Guid clienteId;
        string idAssinatura;
        using (var db = api.Contexto(ApiFactory.ClienteId))
        {
            var a = await db.Assinaturas.IgnoreQueryFilters().SingleAsync(x => x.EmailCobranca == email);
            clienteId = db.Entry(a).Property<Guid>(EscolaDbContext.ColunaCliente).CurrentValue;
            idAssinatura = a.IdAssinaturaGateway!;
            Assert.Equal("LANCAMENTO20", a.CupomCodigo);
        }
        api.Asaas.Pagar(api.Asaas.Assinaturas[idAssinatura][0].PagamentoId);
        var proxima = api.Asaas.GerarFatura(idAssinatura, DateOnly.FromDateTime(DateTime.Today).AddDays(20), 112m);
        using (var escopo = api.Services.CreateScope())
            await escopo.ServiceProvider.GetRequiredService<IAssinaturaService>().AtualizarAsync(clienteId);
        Assert.Equal(60m, api.Asaas.Pagamentos[proxima].Valor);
    }
}
