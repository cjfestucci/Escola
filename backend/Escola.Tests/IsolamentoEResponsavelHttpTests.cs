using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Escola.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Escola.Tests;

/// <summary>Os dois riscos de vazamento do sistema: um cliente enxergar o outro (mesmo banco) e um pai enxergar o filho de outro.</summary>
public class IsolamentoEResponsavelHttpTests : IClassFixture<ApiFactory>
{
    private static readonly Guid OutroCliente = Guid.Parse("00000000-0000-0000-0000-0000000000c2");
    private readonly ApiFactory _api;

    public IsolamentoEResponsavelHttpTests(ApiFactory api)
    {
        _api = api;
        using var db = api.Contexto(ApiFactory.ClienteId);
        if (!db.Clientes.IgnoreQueryFilters().Any(c => c.Id == OutroCliente))
        {
            db.Clientes.Add(new Cliente { Id = OutroCliente, Nome = "Outro cliente", CriadoEm = DateTime.UtcNow });
            db.SaveChanges();
        }
    }

    // ----- cliente × cliente -----

    [Fact]
    public async Task ListaNaoTrazDadosDeOutroCliente()
    {
        using (var db = _api.Contexto(OutroCliente))
        {
            db.Fornecedores.Add(new Fornecedor { Id = Guid.NewGuid(), Nome = "Fornecedor-Do-Outro-Cliente" });
            await db.SaveChangesAsync();
        }
        using (var db = _api.Contexto(ApiFactory.ClienteId))
        {
            db.Fornecedores.Add(new Fornecedor { Id = Guid.NewGuid(), Nome = "Fornecedor-Do-Meu-Cliente" });
            await db.SaveChangesAsync();
        }

        var admin = await _api.ClienteLogadoAsync(PapelUsuario.Admin);
        var texto = await admin.GetStringAsync("/api/fornecedores");

        Assert.Contains("Fornecedor-Do-Meu-Cliente", texto);
        Assert.DoesNotContain("Fornecedor-Do-Outro-Cliente", texto);
    }

    [Fact]
    public async Task BuscarPorIdDeOutroClienteDaNaoEncontrado()
    {
        var id = Guid.NewGuid();
        using (var db = _api.Contexto(OutroCliente))
        {
            db.Fornecedores.Add(new Fornecedor { Id = id, Nome = "Secreto" });
            await db.SaveChangesAsync();
        }

        var admin = await _api.ClienteLogadoAsync(PapelUsuario.Admin);

        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/fornecedores/{id}")).StatusCode);
    }

    [Fact]
    public async Task TokenDeOutroClienteNaoValeMesmoAssinadoComAMesmaChave()
    {
        var usuario = _api.CriarUsuario(PapelUsuario.Admin, $"forjado-{Guid.NewGuid():N}@teste.com");

        string Token(Guid cliente)
        {
            var credenciais = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ApiFactory.JwtChave)), SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()), new Claim(ClaimTypes.Name, usuario.Nome),
                new Claim(ClaimTypes.Role, "Admin"), new Claim("clienteId", cliente.ToString())
            };
            return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken("RotinaEscola", null, claims, expires: DateTime.UtcNow.AddHours(1), signingCredentials: credenciais));
        }

        HttpClient Com(string token)
        {
            var http = _api.CreateClient();
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return http;
        }

        Assert.Equal(HttpStatusCode.OK, (await Com(Token(ApiFactory.ClienteId)).GetAsync("/api/fornecedores")).StatusCode); // controle: o forjador acerta o cliente
        Assert.Equal(HttpStatusCode.Unauthorized, (await Com(Token(OutroCliente)).GetAsync("/api/fornecedores")).StatusCode);
    }

    [Fact]
    public async Task TokenAssinadoComOutraChaveOuSemTokenNaoEntra()
    {
        var http = _api.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await http.GetAsync("/api/fornecedores")).StatusCode);

        var credenciais = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes("outra-chave-qualquer-com-mais-de-32-caracteres!!")), SecurityAlgorithms.HmacSha256);
        var claims = new[] { new Claim(ClaimTypes.Role, "Admin"), new Claim("clienteId", ApiFactory.ClienteId.ToString()) };
        var falso = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken("RotinaEscola", null, claims, expires: DateTime.UtcNow.AddHours(1), signingCredentials: credenciais));
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", falso);

        Assert.Equal(HttpStatusCode.Unauthorized, (await http.GetAsync("/api/fornecedores")).StatusCode);
    }

    // ----- pai × pai -----

    private sealed record Familia(Guid FilhoId, Guid ResponsavelId, HttpClient Pai);

    private async Task<Familia> NovaFamiliaAsync()
    {
        var sufixo = Guid.NewGuid().ToString("N");
        Guid filhoId, responsavelId;
        using (var db = _api.Contexto(ApiFactory.ClienteId))
        {
            var turma = BancoDeTeste.NovaTurma(db);
            var filho = new Aluno { Id = Guid.NewGuid(), Nome = $"Filho {sufixo}", TurmaId = turma.Id };
            var responsavel = new Responsavel { Id = Guid.NewGuid(), Nome = $"Pai {sufixo}", Email = $"pai-{sufixo}@teste.com" };
            db.Alunos.Add(filho);
            db.Responsaveis.Add(responsavel);
            db.AlunoResponsaveis.Add(new AlunoResponsavel { AlunoId = filho.Id, ResponsavelId = responsavel.Id, ResponsavelFinanceiro = true });
            db.Usuarios.Add(new Usuario
            {
                Id = Guid.NewGuid(), Nome = responsavel.Nome, Email = responsavel.Email, Papel = PapelUsuario.Responsavel,
                SenhaHash = SenhaHasher.Hash("Senha-de-Teste-1"), ResponsavelId = responsavel.Id
            });
            await db.SaveChangesAsync();
            filhoId = filho.Id; responsavelId = responsavel.Id;
        }

        var http = _api.CreateClient();
        var resposta = await _api.EntrarAsync(http, $"pai-{sufixo}@teste.com", "Senha-de-Teste-1");
        resposta.EnsureSuccessStatusCode();
        var corpo = (await System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<ApiFactory.LoginResposta>(resposta.Content))!;
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", corpo.Token);
        return new Familia(filhoId, responsavelId, http);
    }

    [Theory]
    [InlineData("ficha-saude")]
    [InlineData("cobrancas")]
    [InlineData("jogos")]
    [InlineData("frequencia")]
    [InlineData("documentos-saude")]
    public async Task PaiVeOProprioFilhoMasNaoOFilhoDeOutraFamilia(string recurso)
    {
        var a = await NovaFamiliaAsync();
        var b = await NovaFamiliaAsync();

        Assert.NotEqual(HttpStatusCode.Forbidden, (await a.Pai.GetAsync($"/api/alunos/{a.FilhoId}/{recurso}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await a.Pai.GetAsync($"/api/alunos/{b.FilhoId}/{recurso}")).StatusCode);
    }

    [Fact]
    public async Task PaiNaoListaOsFilhosDeOutroResponsavel()
    {
        var a = await NovaFamiliaAsync();
        var b = await NovaFamiliaAsync();

        Assert.Equal(HttpStatusCode.OK, (await a.Pai.GetAsync($"/api/responsaveis/{a.ResponsavelId}/alunos")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await a.Pai.GetAsync($"/api/responsaveis/{b.ResponsavelId}/alunos")).StatusCode);
    }

    [Theory]
    [InlineData("/api/turmas")]
    [InlineData("/api/alunos")]
    [InlineData("/api/financeiro/cobrancas")]
    [InlineData("/api/usuarios")]
    [InlineData("/api/fornecedores")]
    public async Task PaiNaoAcessaTelasDaEquipe(string rota)
    {
        var pai = (await NovaFamiliaAsync()).Pai;
        Assert.Equal(HttpStatusCode.Forbidden, (await pai.GetAsync(rota)).StatusCode);
    }

    [Fact]
    public async Task EducadorVeAFichaDeQualquerAlunoMasNaoOFinanceiro()
    {
        var familia = await NovaFamiliaAsync();
        var educador = await _api.ClienteLogadoAsync(PapelUsuario.Educador);

        Assert.Equal(HttpStatusCode.OK, (await educador.GetAsync($"/api/alunos/{familia.FilhoId}/ficha-saude")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await educador.GetAsync("/api/financeiro/cobrancas")).StatusCode);
    }

    [Fact]
    public async Task SemLoginNadaAbreAlemDoLogin()
    {
        var http = _api.CreateClient();
        foreach (var rota in new[] { "/api/alunos", "/api/turmas", "/api/financeiro/cobrancas", "/api/plataforma/cliente", "/api/logs?entidadeTipo=Turma&entidadeId=" + Guid.NewGuid() })
            Assert.Equal(HttpStatusCode.Unauthorized, (await http.GetAsync(rota)).StatusCode);

        // verificação de saúde do deploy: anônima e sem nenhum detalhe
        var saude = await http.GetAsync("/healthz");
        Assert.Equal(HttpStatusCode.OK, saude.StatusCode);
        Assert.Equal("{\"status\":\"ok\"}", await saude.Content.ReadAsStringAsync());

        // anônimo de propósito: a tela de login precisa do fuso, da cor, do segmento e da logo antes de autenticar
        Assert.Equal(HttpStatusCode.OK, (await http.GetAsync("/api/configuracao/escola")).StatusCode);
    }
}
