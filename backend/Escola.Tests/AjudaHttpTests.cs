using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Escola.Domain.Entities;
using Escola.Domain.Enums;

namespace Escola.Tests;

/// <summary>Fale conosco (equipe → suporte do produto, com contexto e Reply-To) e "Fale com o clube" do portal (pais → contato do clube).</summary>
public class AjudaHttpTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task FaleConoscoChegaAoSuporteComContextoEReplyToEQuemEscreveuRecebeCopia()
    {
        var email = $"treinador.{Guid.NewGuid():N}@teste.com";
        var treinador = await api.ClienteLogadoAsync(PapelUsuario.Educador, email);

        var resposta = await treinador.PostAsJsonAsync("/api/ajuda/fale-conosco",
            new { assunto = "Problema ou erro", mensagem = "A chamada não salva <b>de jeito nenhum</b>.\nJá tentei duas vezes.", pagina = "/chamada" });
        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);

        var paraSuporte = api.Email.Enviados.Last(e => e.Para == "suporte@gestortatico.com.br");
        Assert.StartsWith("[Fale conosco] Problema ou erro — ", paraSuporte.Assunto);
        Assert.Contains("/chamada", paraSuporte.Corpo);
        Assert.Contains(email, paraSuporte.Corpo);
        Assert.Contains("Professor", paraSuporte.Corpo);
        Assert.Contains("&lt;b&gt;de jeito nenhum&lt;/b&gt;", paraSuporte.Corpo); // texto livre nunca vira HTML
        Assert.Contains("<br>", paraSuporte.Corpo);
        Assert.Contains(("suporte@gestortatico.com.br", email), api.Email.Respostas);

        var copia = api.Email.Enviados.Last(e => e.Para == email);
        Assert.StartsWith("Recebemos sua mensagem", copia.Assunto);
    }

    [Fact]
    public async Task ValidaAssuntoEMensagemEFreiaExcesso()
    {
        var admin = await api.ClienteLogadoAsync(PapelUsuario.Admin);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/ajuda/fale-conosco", new { assunto = "Qualquer", mensagem = "Mensagem longa o bastante" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/ajuda/fale-conosco", new { assunto = "Sugestão", mensagem = "curta" })).StatusCode);

        for (var i = 0; i < 5; i++)
            Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsJsonAsync("/api/ajuda/fale-conosco", new { assunto = "Sugestão", mensagem = $"Sugestão número {i} para o sistema" })).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await admin.PostAsJsonAsync("/api/ajuda/fale-conosco", new { assunto = "Sugestão", mensagem = "Mais uma sugestão para o sistema" })).StatusCode);
    }

    [Fact]
    public async Task PaisNaoFalamComOSuporteMasVeemOContatoDoClube()
    {
        using (var db = api.Contexto(ApiFactory.ClienteId))
        {
            db.Unidades.Add(new Unidade { Id = Guid.NewGuid(), Nome = "Unidade Contato Teste", Telefone = "(16) 3333-4444", WhatsApp = "(16) 99123-4567", Email = "contato@unidade.test", Endereco = "Rua A, 10", Ativa = true });
            db.Unidades.Add(new Unidade { Id = Guid.NewGuid(), Nome = "Unidade Fechada Teste", Telefone = "(16) 0000-0000", Ativa = false });
            await db.SaveChangesAsync();
        }

        var pai = await api.ClienteLogadoAsync(PapelUsuario.Responsavel);
        Assert.Equal(HttpStatusCode.Forbidden, (await pai.PostAsJsonAsync("/api/ajuda/fale-conosco", new { assunto = "Outro", mensagem = "Mensagem longa o bastante" })).StatusCode);

        var contato = await pai.GetFromJsonAsync<JsonElement>("/api/portal/contato-clube", Json);
        Assert.False(string.IsNullOrWhiteSpace(contato.GetProperty("nomeClube").GetString()));
        var unidades = contato.GetProperty("unidades").EnumerateArray().Select(u => u.GetProperty("nome").GetString()).ToList();
        Assert.Contains("Unidade Contato Teste", unidades);
        Assert.DoesNotContain("Unidade Fechada Teste", unidades);
        Assert.Equal(JsonValueKind.Array, contato.GetProperty("atletas").ValueKind); // atletas do pai, pra mensagem inicial do WhatsApp
        Assert.Equal("(16) 99123-4567", contato.GetProperty("unidades").EnumerateArray()
            .Single(u => u.GetProperty("nome").GetString() == "Unidade Contato Teste").GetProperty("whatsApp").GetString());
        Assert.Equal("contato@unidade.test", contato.GetProperty("unidades").EnumerateArray()
            .Single(u => u.GetProperty("nome").GetString() == "Unidade Contato Teste").GetProperty("email").GetString());

        var admin = await api.ClienteLogadoAsync(PapelUsuario.Admin);
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.GetAsync("/api/portal/contato-clube")).StatusCode);
    }
}

/// <summary>WhatsApp da unidade: opcional, mas, se informado, precisa ter DDD + número (o link wa.me não funciona com número incompleto).</summary>
public class UnidadeWhatsAppHttpTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task WhatsAppPrecisaDeDddEmailPrecisaSerValidoEVaoProHistorico()
    {
        var admin = await api.ClienteLogadoAsync(PapelUsuario.Admin);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await admin.PostAsJsonAsync("/api/unidades", new { nome = "Sede WhatsApp", ativa = true, whatsApp = "99123-4567" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await admin.PostAsJsonAsync("/api/unidades", new { nome = "Sede WhatsApp", ativa = true, email = "sem-arroba" })).StatusCode);

        var criada = await admin.PostAsJsonAsync("/api/unidades", new { nome = "Sede WhatsApp", ativa = true, whatsApp = "(16) 99123-4567" });
        criada.EnsureSuccessStatusCode();
        var id = (await criada.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var editada = await admin.PutAsJsonAsync($"/api/unidades/{id}", new { nome = "Sede WhatsApp", ativa = true, whatsApp = "(16) 98888-7777" });
        editada.EnsureSuccessStatusCode();
        var logs = await admin.GetFromJsonAsync<JsonElement>($"/api/logs?entidadeTipo=Unidade&entidadeId={id}");
        Assert.Contains(logs.EnumerateArray(), l => l.GetProperty("detalhe").GetString()?.Contains("WhatsApp alterado de \"(16) 99123-4567\" para \"(16) 98888-7777\"") == true);
    }
}

/// <summary>Pai manda mensagem pro clube pelo portal: só pra unidade com e-mail, chega lá com Reply-To no pai, e a equipe não usa essa rota.</summary>
public class MensagemAoClubeHttpTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task PaiEscreveProEmailDaUnidadeComReplyToNele()
    {
        var comEmail = Guid.NewGuid();
        var semEmail = Guid.NewGuid();
        using (var db = api.Contexto(ApiFactory.ClienteId))
        {
            db.Unidades.Add(new Unidade { Id = comEmail, Nome = "Sede Com Email", Email = "secretaria@clube.test", Ativa = true });
            db.Unidades.Add(new Unidade { Id = semEmail, Nome = "Sede Sem Email", Telefone = "(16) 3333-4444", Ativa = true });
            await db.SaveChangesAsync();
        }

        var emailPai = $"pai.{Guid.NewGuid():N}@teste.com";
        var pai = await api.ClienteLogadoAsync(PapelUsuario.Responsavel, emailPai);
        Assert.Equal(HttpStatusCode.BadRequest, (await pai.PostAsJsonAsync("/api/portal/contato-clube/mensagem",
            new { unidadeId = semEmail, assunto = "Outro", mensagem = "Mensagem longa o bastante" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await pai.PostAsJsonAsync("/api/portal/contato-clube/mensagem",
            new { unidadeId = comEmail, assunto = "Assunto inventado", mensagem = "Mensagem longa o bastante" })).StatusCode);

        var resposta = await pai.PostAsJsonAsync("/api/portal/contato-clube/mensagem",
            new { unidadeId = comEmail, assunto = "Mensalidade e pagamento", mensagem = "Paguei ontem e ainda aparece em aberto." });
        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        var paraClube = api.Email.Enviados.Last(e => e.Para == "secretaria@clube.test");
        Assert.StartsWith("[Portal] Mensalidade e pagamento — ", paraClube.Assunto);
        Assert.Contains("Paguei ontem", paraClube.Corpo);
        Assert.Contains(("secretaria@clube.test", emailPai), api.Email.Respostas);
        Assert.Contains(api.Email.Enviados, e => e.Para == emailPai && e.Assunto.StartsWith("Sua mensagem foi enviada"));

        var admin = await api.ClienteLogadoAsync(PapelUsuario.Admin);
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.PostAsJsonAsync("/api/portal/contato-clube/mensagem",
            new { unidadeId = comEmail, assunto = "Outro", mensagem = "Mensagem longa o bastante" })).StatusCode);
    }
}
