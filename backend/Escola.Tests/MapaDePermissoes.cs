using System.Reflection;
using Escola.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Escola.Tests;

/// <summary>Lê, por reflexão, quem pode chamar cada endpoint (atributos [Authorize]/[AllowAnonymous] da classe e da ação).
/// Quando classe e ação têm Roles, valem as duas (interseção) — é o comportamento do ASP.NET.</summary>
public static class MapaDePermissoes
{
    public static readonly string[] TodosOsPapeis = ["Admin", "Coordenador", "Educador", "Financeiro", "Responsavel", "Suporte"];

    public sealed record Endpoint(string Controller, string Acao, string Metodo, string Rota, bool Anonimo, bool ExigeLogin, IReadOnlySet<string> Papeis)
    {
        public string Titulo => $"{Metodo} {Rota}";
        public bool Permite(string papel) => Anonimo || (ExigeLogin && Papeis.Contains(papel));
    }

    public static IReadOnlyList<Endpoint> Todos() => typeof(AuthController).Assembly.GetTypes()
        .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract)
        .SelectMany(Endpoints)
        .ToList();

    private static IEnumerable<Endpoint> Endpoints(Type controller)
    {
        var rotaClasse = controller.GetCustomAttribute<RouteAttribute>()?.Template ?? string.Empty;
        var autorizacaoClasse = controller.GetCustomAttributes<AuthorizeAttribute>(inherit: true).ToList();

        foreach (var acao in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            foreach (var http in acao.GetCustomAttributes<HttpMethodAttribute>())
            {
                var autorizacaoAcao = acao.GetCustomAttributes<AuthorizeAttribute>(inherit: true).ToList();
                var anonimo = acao.GetCustomAttribute<AllowAnonymousAttribute>() is not null
                    || (controller.GetCustomAttribute<AllowAnonymousAttribute>() is not null && autorizacaoAcao.Count == 0);

                var exigeLogin = autorizacaoClasse.Count > 0 || autorizacaoAcao.Count > 0;
                var papeis = new HashSet<string>(TodosOsPapeis);
                foreach (var auth in autorizacaoClasse.Concat(autorizacaoAcao).Where(a => !string.IsNullOrWhiteSpace(a.Roles)))
                    papeis.IntersectWith(auth.Roles!.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));

                // Template começando com "/" é absoluto e ignora a rota da classe.
                var rota = http.Template?.StartsWith('/') == true
                    ? http.Template
                    : "/" + string.Join('/', new[] { rotaClasse, http.Template }.Where(s => !string.IsNullOrEmpty(s))).Trim('/');
                yield return new Endpoint(controller.Name, acao.Name, http.HttpMethods.First(), rota, anonimo, exigeLogin, papeis);
            }
        }
    }
}
