using Microsoft.AspNetCore.Hosting;
using System.Net.Http.Json;

namespace Escola.Tests;

/// <summary>Cada clube que se cadastra pelo site gera um aviso por e-mail pra equipe do produto (Assinatura:EmailAvisoCadastro).</summary>
public class AvisoNovoCadastroHttpTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task CadastroPeloSiteAvisaAEquipe()
    {
        var http = api.WithWebHostBuilder(b => b.UseSetting("Assinatura:EmailAvisoCadastro", "equipe@gestortatico.test, suporte@gestortatico.test")).CreateClient();
        var email = $"aviso.{Guid.NewGuid():N}@clube.test";

        var resposta = await http.PostAsJsonAsync("/api/assinaturas", new
        {
            nomeClube = "Clube <Aviso>", cpfCnpj = "11222333000181", cidade = "Campinas", atletas = 30,
            nomeAdmin = "Dona do Clube", email, celular = "(19) 99999-8888", aceiteTermos = true
        });
        resposta.EnsureSuccessStatusCode();

        List<(string Para, string Assunto, string Corpo)> enviados;
        lock (api.Email.Enviados) enviados = api.Email.Enviados.ToList();
        Assert.Single(enviados, e => e.Para == "suporte@gestortatico.test"); // mais de um destino, separados por vírgula
        var aviso = Assert.Single(enviados, e => e.Para == "equipe@gestortatico.test");
        Assert.Equal("Novo clube cadastrado: Clube <Aviso>", aviso.Assunto);
        Assert.Contains("Clube &lt;Aviso&gt;", aviso.Corpo); // texto do clube sempre escapado no HTML
        Assert.Contains(email, aviso.Corpo);
        Assert.Contains("R$ 100,00", aviso.Corpo); // 60 + 4 × (30 − 20)
    }
}
