using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Escola.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;

namespace Escola.Tests;

/// <summary>Matrícula só vale depois do aceite do termo pelo responsável: convite por e-mail → senha criada → termo no portal.</summary>
public class TermoMatriculaHttpTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private async Task<(Guid AlunoId, string EmailPai, Guid TurmaId)> MatricularAsync(HttpClient gestao, string nomeAluno)
    {
        Guid turmaId;
        using (var db = api.Contexto(ApiFactory.ClienteId))
        {
            var turma = BancoDeTeste.NovaTurma(db, $"Turma {nomeAluno}");
            turma.ValorMensalidade = 300m;
            await db.SaveChangesAsync();
            turmaId = turma.Id;
        }

        var emailPai = $"pai-{Guid.NewGuid():N}@teste.com";
        var resposta = await gestao.PostAsJsonAsync("/api/alunos", new
        {
            nome = nomeAluno,
            dataNascimento = "2016-03-10",
            turmaId,
            fotoUrl = (string?)null,
            responsaveis = new[] { new { id = (Guid?)null, nome = "Pai da Silva", email = emailPai, telefone = (string?)null, responsavelFinanceiro = true } }
        });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.True(corpo.GetProperty("matriculaPendente").GetBoolean());
        var convite = corpo.GetProperty("convites")[0];
        Assert.Equal("Convite", convite.GetProperty("tipo").GetString());
        Assert.True(convite.GetProperty("entregue").GetBoolean());
        Assert.Equal(JsonValueKind.Null, convite.GetProperty("link").ValueKind); // entregue por e-mail: o link não volta pra tela

        return (corpo.GetProperty("id").GetGuid(), emailPai, turmaId);
    }

    private async Task<HttpClient> AceitarConviteEEntrarAsync(string emailPai)
    {
        var http = api.CreateClient();

        // Antes do convite aceito a conta não entra.
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.EntrarAsync(http, emailPai, "qualquer-coisa")).StatusCode);

        var email = api.Email.Enviados.Last(e => e.Para == emailPai);
        Assert.Contains("A matrícula só é efetivada depois desse aceite", email.Corpo);
        var token = Regex.Match(email.Corpo, @"token=([A-Za-z0-9_\-]+)&amp;convite=1").Groups[1].Value;
        Assert.NotEmpty(token);

        var redefinir = await http.PostAsJsonAsync("/api/auth/redefinir-senha", new { token, novaSenha = "Senha-do-Pai-1" });
        Assert.True(redefinir.IsSuccessStatusCode, await redefinir.Content.ReadAsStringAsync());

        var login = await api.EntrarAsync(http, emailPai, "Senha-do-Pai-1");
        login.EnsureSuccessStatusCode();
        var corpo = await login.Content.ReadFromJsonAsync<ApiFactory.LoginResposta>();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", corpo!.Token);
        return http;
    }

    [Fact]
    public async Task MatriculaSoEfetivaDepoisDoAceiteDoTermo()
    {
        var gestao = await api.ClienteLogadoAsync(PapelUsuario.Coordenador);
        var (alunoId, emailPai, _) = await MatricularAsync(gestao, "Joãozinho Termo");
        var pai = await AceitarConviteEEntrarAsync(emailPai);

        var termo = await pai.GetFromJsonAsync<JsonElement>("/api/portal/termo", Json);
        var versao = termo.GetProperty("versao").GetString();
        var pendente = termo.GetProperty("alunos").EnumerateArray().Single();
        Assert.Equal(alunoId, pendente.GetProperty("alunoId").GetGuid());
        Assert.True(pendente.GetProperty("matriculaPendente").GetBoolean());
        Assert.Contains("Joãozinho Termo", termo.GetProperty("paragrafos")[0].GetString());

        // Lista diferente da mostrada (aqui, vazia) é recusada: o texto guardado tem que ser o que a pessoa leu.
        Assert.Equal(HttpStatusCode.BadRequest, (await pai.PostAsJsonAsync("/api/portal/termo/aceitar", new { versao, alunoIds = Array.Empty<Guid>() })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await pai.PostAsJsonAsync("/api/portal/termo/aceitar", new { versao = "antiga", alunoIds = new[] { alunoId } })).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await pai.PostAsJsonAsync("/api/portal/termo/aceitar", new { versao, alunoIds = new[] { alunoId } })).StatusCode);

        using (var db = api.Contexto(ApiFactory.ClienteId))
        {
            Assert.NotNull((await db.Alunos.FirstAsync(a => a.Id == alunoId)).MatriculaConfirmadaEm);
            var aceite = await db.TermosAceite.SingleAsync(t => t.AlunoId == alunoId);
            Assert.Contains("Joãozinho Termo", aceite.TextoAceito);
            Assert.Equal(versao, aceite.Versao);
            Assert.Contains(await db.LogsAuditoria.Where(l => l.EntidadeId == alunoId).Select(l => l.Detalhe).ToListAsync(),
                d => d != null && d.StartsWith("Matrícula confirmada"));
        }

        var depois = await pai.GetFromJsonAsync<JsonElement>("/api/portal/termo", Json);
        Assert.Equal(0, depois.GetProperty("alunos").GetArrayLength());

        // A equipe vê o aceite; o responsável não usa esse endpoint.
        var aceites = await gestao.GetFromJsonAsync<JsonElement>($"/api/alunos/{alunoId}/termos", Json);
        Assert.Equal(1, aceites.GetArrayLength());
        Assert.Equal(HttpStatusCode.Forbidden, (await pai.GetAsync($"/api/alunos/{alunoId}/termos")).StatusCode);
    }

    [Fact]
    public async Task MatriculaPendenteNaoGeraMensalidadeEmLote()
    {
        var gestao = await api.ClienteLogadoAsync(PapelUsuario.Admin);
        var (alunoId, _, turmaId) = await MatricularAsync(gestao, "Pendente Mensalidade");

        var previa = await (await gestao.PostAsJsonAsync("/api/financeiro/mensalidades/previa", new { ano = 2026, mes = 11, turmaId }))
            .Content.ReadFromJsonAsync<JsonElement>(Json);
        var item = previa.GetProperty("itens").EnumerateArray().Single(i => i.GetProperty("alunoId").GetGuid() == alunoId);
        Assert.Equal("MatriculaPendente", item.GetProperty("situacao").GetString());
        Assert.Equal(0, previa.GetProperty("aGerar").GetInt32());
    }

    [Fact]
    public async Task ExportacaoDeDadosEhSoDaGestaoEFicaNoHistorico()
    {
        var gestao = await api.ClienteLogadoAsync(PapelUsuario.Admin);
        var (alunoId, _, _) = await MatricularAsync(gestao, "Maria Exportação");

        var educador = await api.ClienteLogadoAsync(PapelUsuario.Educador);
        Assert.Equal(HttpStatusCode.Forbidden, (await educador.GetAsync($"/api/alunos/{alunoId}/dados-pessoais/exportar")).StatusCode);

        var resposta = await gestao.GetAsync($"/api/alunos/{alunoId}/dados-pessoais/exportar");
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal("application/zip", resposta.Content.Headers.ContentType?.MediaType);

        using var zip = new ZipArchive(await resposta.Content.ReadAsStreamAsync());
        using var leitor = new StreamReader(zip.GetEntry("dados.json")!.Open());
        var dados = JsonDocument.Parse(await leitor.ReadToEndAsync()).RootElement;
        Assert.Equal("Maria Exportação", dados.GetProperty("Aluno").GetProperty("Nome").GetString());
        Assert.Equal("Pai da Silva", dados.GetProperty("Responsaveis")[0].GetProperty("Nome").GetString());

        using var db = api.Contexto(ApiFactory.ClienteId);
        Assert.True(await db.LogsAuditoria.AnyAsync(l => l.EntidadeId == alunoId && l.Detalhe == "Dados pessoais exportados (LGPD)"));
    }

    [Fact]
    public async Task ResponsavelSemContaAtivaNaoTemSenhaGeradaNemEntra()
    {
        var gestao = await api.ClienteLogadoAsync(PapelUsuario.Admin);
        var (_, emailPai, _) = await MatricularAsync(gestao, "Sem Senha");

        using var db = api.Contexto(ApiFactory.ClienteId);
        var conta = await db.Usuarios.SingleAsync(u => u.Email == emailPai);
        Assert.Equal(PapelUsuario.Responsavel, conta.Papel);
        Assert.Equal(SenhaHasher.ConvitePendente, conta.SenhaHash);
    }
}
