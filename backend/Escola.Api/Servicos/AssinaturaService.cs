using System.Globalization;
using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Escola.Infrastructure;
using Escola.Infrastructure.Assinaturas;
using Escola.Infrastructure.Auditoria;
using Escola.Infrastructure.Auth;
using Escola.Infrastructure.Clientes;
using Escola.Infrastructure.Data;
using Escola.Infrastructure.Email;
using Escola.Infrastructure.Pagamentos;
using Escola.Infrastructure.Pagamentos.Asaas;
using Escola.Infrastructure.Tempo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Escola.Api.Servicos;

/// <summary>Cadastro pelo site. Os dados já chegam validados pelo controller.</summary>
public sealed record DadosCadastroAssinatura(
    string NomeClube, string CpfCnpj, string Cidade, int Atletas,
    string NomeAdmin, string Email, string Celular, string? Ip, string? Navegador, Cupom? Cupom = null);

/// <param name="LinkPagamento">Só sem teste grátis: a página de pagamento da 1ª fatura (o acesso sai depois do pagamento).</param>
public sealed record ResultadoCadastroAssinatura(
    Guid ClienteId, SituacaoAssinatura Situacao, DateOnly? TesteAte, decimal ValorMensal, string? LinkPagamento, bool EmailEnviado);

public interface IAssinaturaService
{
    /// <summary>Cria o cliente (clube), a Unidade Principal, o Admin (convite pendente), a conta de Suporte e a assinatura; abre a
    /// assinatura no gateway (melhor esforço) e, com teste grátis, manda o e-mail de boas-vindas.</summary>
    Task<ResultadoCadastroAssinatura> CadastrarAsync(DadosCadastroAssinatura dados, CancellationToken ct = default);

    /// <summary>Confere as faturas no gateway e recalcula a situação de um cliente (num escopo próprio). Nulo se o cliente não tem
    /// assinatura (provisionado à mão).</summary>
    Task<SituacaoAssinatura?> AtualizarAsync(Guid clienteId, CancellationToken ct = default);

    /// <summary>Faturas da assinatura do cliente da requisição; <c>Disponivel = false</c> se o gateway não respondeu.</summary>
    Task<(IReadOnlyList<FaturaAssinatura> Faturas, bool Disponivel)> FaturasAsync(Assinatura assinatura, CancellationToken ct = default);

    /// <summary>Cancela: encerra a assinatura no gateway (sem faturas novas) e o acesso segue até o fim do período pago ou do teste.</summary>
    Task<(DateOnly? AcessoAte, string? Erro)> CancelarAsync(Guid clienteId, Guid usuarioId, CancellationToken ct = default);

    /// <summary>Desfaz o cancelamento (ainda dentro do acesso ou já Cancelada): abre uma assinatura nova no gateway. Dentro do acesso, a próxima
    /// fatura vence no fim do período já pago; depois dele, vence hoje.</summary>
    Task<string?> ReativarAsync(Guid clienteId, Guid usuarioId, CancellationToken ct = default);

    /// <summary>Retenção de dados (cancelada ou teste nunca pago): aviso 7 dias antes e, passado o prazo, exclusão — se
    /// <c>Assinatura:ExclusaoAutomatica</c> estiver ligada.</summary>
    Task ProcessarRetencaoAsync(Guid clienteId, CancellationToken ct = default);
}

public sealed class AssinaturaService(
    IServiceScopeFactory escopos,
    IOptions<OpcoesAssinatura> opcoes,
    IOptions<OpcoesAsaas> opcoesAsaas,
    AsaasApi asaas,
    IConfiguration config,
    ILogger<AssinaturaService> logger) : IAssinaturaService
{
    /// <summary>Versão dos Termos de Uso e da Política de Privacidade aceitos no cadastro (publicados no site: site/termos.html e
    /// site/privacidade.html). Mudou o texto → troque aqui e em VERSAO_DOCUMENTOS (site/paginas.js).</summary>
    public const string VersaoTermosUso = "2026-10-15.2";

    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public async Task<ResultadoCadastroAssinatura> CadastrarAsync(DadosCadastroAssinatura dados, CancellationToken ct = default)
    {
        var o = opcoes.Value;
        var clienteId = Guid.NewGuid();
        var agora = DateTime.UtcNow;

        // Cliente novo = escopo novo (um DbContext nunca atende dois clientes).
        using var escopo = escopos.CreateScope();
        escopo.ServiceProvider.GetRequiredService<ClienteAtual>().Definir(clienteId);
        var db = escopo.ServiceProvider.GetRequiredService<EscolaDbContext>();
        var auditoria = escopo.ServiceProvider.GetRequiredService<IAuditoriaService>();

        // O cliente primeiro: todas as outras linhas apontam pra ele.
        db.Clientes.Add(new Cliente { Id = clienteId, Nome = dados.NomeClube, Segmento = SegmentoCliente.Clube, Ativo = true, CriadoEm = agora });
        await db.SaveChangesAsync(ct);

        var hoje = await escopo.ServiceProvider.GetRequiredService<IRelogioEscola>().HojeAsync();
        DateOnly? testeAte = o.DiasTeste > 0 ? hoje.AddDays(o.DiasTeste) : null;

        var assinatura = new Assinatura
        {
            Id = Guid.NewGuid(),
            Situacao = testeAte is null ? SituacaoAssinatura.AguardandoPagamento : SituacaoAssinatura.EmTeste,
            TesteAte = testeAte,
            PrimeiroVencimento = testeAte ?? hoje,
            VencimentoEmAberto = testeAte ?? hoje,
            ValorMensal = CupomRegras.AplicarDesconto(o.CalcularValor(dados.Atletas), dados.Cupom?.PercentualDesconto),
            CupomCodigo = dados.Cupom?.Codigo,
            CupomPercentual = dados.Cupom?.PercentualDesconto,
            CupomMeses = dados.Cupom?.Meses,
            AtletasInformados = dados.Atletas,
            CpfCnpj = dados.CpfCnpj,
            Cidade = dados.Cidade,
            Celular = dados.Celular,
            EmailCobranca = dados.Email,
            CriadaEm = agora,
            SituacaoAtualizadaEm = agora,
            TermosVersao = VersaoTermosUso,
            TermosAceitosEm = agora,
            TermosIp = dados.Ip,
            TermosNavegador = dados.Navegador
        };
        var admin = new Usuario
        {
            Id = Guid.NewGuid(), Nome = dados.NomeAdmin, Email = dados.Email,
            SenhaHash = SenhaHasher.ConvitePendente, Papel = PapelUsuario.Admin, Ativo = true
        };
        db.Assinaturas.Add(assinatura);
        db.Usuarios.Add(admin);
        // Toda turma precisa de uma unidade: o clube já começa com a principal.
        db.Unidades.Add(new Unidade { Id = Guid.NewGuid(), Nome = "Unidade Principal", Ativa = true });
        auditoria.RegistrarSistema(nameof(Assinatura), assinatura.Id, AcaoAuditoria.Criado,
            $"Assinatura criada pelo site: {Plano(assinatura)}" + (testeAte is { } t ? $", teste grátis até {t:dd/MM/yyyy}" : ", sem teste grátis"));
        auditoria.RegistrarSistema(nameof(Usuario), admin.Id, AcaoAuditoria.Criado,
            $"Administrador criado pela assinatura do site: {admin.Nome} ({admin.Email})");
        await db.SaveChangesAsync(ct);

        // A conta da equipe do produto existe em todo cliente (normalmente criada na subida da API).
        await SuporteProvisionador.GarantirAsync(db, config["Suporte:Email"], config["Suporte:Nome"], config["Suporte:SenhaHash"],
            config["Suporte:TotpSegredo"], logger);

        await GarantirNoGatewayAsync(db, assinatura, dados.NomeClube, hoje, ct);
        if (assinatura.IdAssinaturaGateway is not null) await SincronizarFaturasAsync(db, escopo.ServiceProvider, assinatura, ct);

        var emailEnviado = false;
        if (!SituacaoAssinaturaCalculo.Bloqueia(assinatura.Situacao))
            emailEnviado = await EnviarBoasVindasAsync(db, escopo.ServiceProvider, assinatura, admin, dados.NomeClube, ct);

        return new ResultadoCadastroAssinatura(clienteId, assinatura.Situacao, testeAte, assinatura.ValorMensal,
            assinatura.Situacao == SituacaoAssinatura.AguardandoPagamento ? assinatura.LinkPagamento : null, emailEnviado);
    }

    public async Task<SituacaoAssinatura?> AtualizarAsync(Guid clienteId, CancellationToken ct = default)
    {
        using var escopo = escopos.CreateScope();
        escopo.ServiceProvider.GetRequiredService<ClienteAtual>().Definir(clienteId);
        var db = escopo.ServiceProvider.GetRequiredService<EscolaDbContext>();
        var assinatura = await db.Assinaturas.FirstOrDefaultAsync(ct);
        if (assinatura is null) return null;

        if (assinatura.Situacao != SituacaoAssinatura.Cancelada && assinatura.AcessoAte is null)
        {
            var nomeClube = await db.Clientes.Where(c => c.Id == clienteId).Select(c => c.Nome).FirstAsync(ct);
            var hoje = await escopo.ServiceProvider.GetRequiredService<IRelogioEscola>().HojeAsync();
            await GarantirNoGatewayAsync(db, assinatura, nomeClube, hoje, ct);
        }
        await SincronizarFaturasAsync(db, escopo.ServiceProvider, assinatura, ct);

        // Sem teste, o e-mail de boas-vindas só sai quando o acesso é liberado (depois do 1º pagamento).
        if (assinatura.ConviteEnviadoEm is null && !SituacaoAssinaturaCalculo.Bloqueia(assinatura.Situacao))
        {
            var admin = await db.Usuarios.Where(u => u.Papel == PapelUsuario.Admin && u.Ativo).OrderBy(u => u.Nome).FirstOrDefaultAsync(ct);
            var nomeClube = await db.Clientes.Where(c => c.Id == clienteId).Select(c => c.Nome).FirstAsync(ct);
            if (admin is not null && admin.SenhaHash == SenhaHasher.ConvitePendente)
                await EnviarBoasVindasAsync(db, escopo.ServiceProvider, assinatura, admin, nomeClube, ct);
        }
        return assinatura.Situacao;
    }

    public async Task<(IReadOnlyList<FaturaAssinatura> Faturas, bool Disponivel)> FaturasAsync(Assinatura assinatura, CancellationToken ct = default)
    {
        // Sem gateway (ou assinatura ainda não aberta nele) simplesmente não há fatura — não é falha.
        if (!GatewayDaAssinatura(assinatura)) return ([], true);
        try
        {
            var faturas = await asaas.ListarFaturasDaAssinaturaAsync(assinatura.IdAssinaturaGateway!, ct);
            return (faturas.OrderByDescending(f => f.Vencimento).ToList(), true);
        }
        catch (PixProvedorException ex)
        {
            logger.LogWarning(ex, "Não foi possível listar as faturas da assinatura {AssinaturaId}.", assinatura.Id);
            return ([], false);
        }
    }

    public static string Plano(Assinatura a) =>
        $"{Moeda(a.ValorMensal)} por mês ({a.AtletasCobrados ?? a.AtletasInformados} atletas)"
        + (a.CupomCodigo is not null && a.CupomPercentual is { } pct ? $", cupom {a.CupomCodigo}: {CupomRegras.Descricao(pct, a.CupomMeses)}" : string.Empty);

    public static string Moeda(decimal valor) => valor.ToString("C", PtBr);

    public static string Rotulo(SituacaoAssinatura s) => s switch
    {
        SituacaoAssinatura.AguardandoPagamento => "Aguardando pagamento",
        SituacaoAssinatura.EmTeste => "Teste grátis",
        SituacaoAssinatura.Ativa => "Ativa",
        SituacaoAssinatura.EmAtraso => "Em atraso",
        SituacaoAssinatura.Suspensa => "Suspensa",
        SituacaoAssinatura.Cancelada => "Cancelada",
        _ => s.ToString()
    };

    // ----- internos -----

    /// <summary>A assinatura tem ids no gateway configurado neste ambiente (ids de sandbox não valem em produção e vice-versa).</summary>
    private bool GatewayDaAssinatura(Assinatura a) =>
        opcoesAsaas.Value.Configurado && a.IdAssinaturaGateway is not null && a.Ambiente == opcoesAsaas.Value.NomeAmbiente;

    /// <summary>Cria o cliente e a assinatura no Asaas se ainda não existirem (ex.: o gateway estava fora do ar no cadastro). Falha não
    /// desfaz nada: o clube já pode usar o teste, e a tarefa periódica tenta de novo.</summary>
    private async Task GarantirNoGatewayAsync(EscolaDbContext db, Assinatura a, string nomeClube, DateOnly hoje, CancellationToken ct)
    {
        var o = opcoesAsaas.Value;
        if (!o.Configurado) return;
        if (a.Ambiente is not null && a.Ambiente != o.NomeAmbiente)
        {
            a.IdClienteGateway = null;
            a.IdAssinaturaGateway = null;
        }
        if (a.IdAssinaturaGateway is not null) return;

        try
        {
            a.Ambiente = o.NomeAmbiente;
            a.IdClienteGateway ??= await asaas.CriarClientePlataformaAsync(nomeClube, a.CpfCnpj, a.EmailCobranca, a.Celular, a.Id.ToString(), ct);
            await db.SaveChangesAsync(ct);

            var vencimento = a.PrimeiroVencimento < hoje ? hoje : a.PrimeiroVencimento;
            a.IdAssinaturaGateway = await asaas.CriarAssinaturaAsync(a.IdClienteGateway, a.ValorMensal, vencimento,
                $"{MarcaProduto.Nome} — plano mensal ({a.AtletasInformados} atletas)", a.Id.ToString(), ct);
            await db.SaveChangesAsync(ct);
        }
        catch (PixProvedorException ex)
        {
            await db.SaveChangesAsync(ct); // guarda o que deu certo (ex.: o cliente já criado)
            logger.LogWarning(ex, "Não foi possível abrir a assinatura {AssinaturaId} no gateway; a tarefa periódica tenta de novo.", a.Id);
        }
    }

    /// <summary>Lê as faturas e recalcula a situação. Com o gateway configurado mas fora do ar, <b>não mexe</b> na situação — senão um
    /// clube em dia seria suspenso por falha nossa.</summary>
    private async Task SincronizarFaturasAsync(EscolaDbContext db, IServiceProvider sp, Assinatura a, CancellationToken ct)
    {
        var hoje = await sp.GetRequiredService<IRelogioEscola>().HojeAsync();
        SituacaoAssinatura nova;
        IReadOnlyList<FaturaAssinatura> faturas = [];

        if (a.AcessoAte is { } acessoAte)
        {
            // Cancelamento pedido: a assinatura já saiu do gateway; até o fim do acesso nada muda, depois vira Cancelada.
            nova = hoje > acessoAte ? SituacaoAssinatura.Cancelada : a.Situacao;
        }
        else
        {
            if (GatewayDaAssinatura(a))
            {
                var (lidas, disponivel) = await FaturasAsync(a, ct);
                if (!disponivel) return;
                faturas = lidas;
            }

            if (faturas.Any(f => f.Paga)) a.JaPagou = true;
            nova = SituacaoAssinaturaCalculo.Calcular(hoje, a.TesteAte, a.PrimeiroVencimento, opcoes.Value.DiasToleranciaAtraso, null, faturas, a.JaPagou);
            var aberta = SituacaoAssinaturaCalculo.EmAberto(faturas);
            a.LinkPagamento = aberta?.LinkPagamento;
            a.VencimentoEmAberto = faturas.Count > 0 ? aberta?.Vencimento : nova == SituacaoAssinatura.Ativa ? null : a.PrimeiroVencimento;

            if (GatewayDaAssinatura(a) && SituacaoAssinaturaCalculo.PodeAtualizarValor(hoje, faturas))
                await AtualizarValorAsync(db, sp, a, faturas.Count(f => f.Paga), ct);
        }

        if (nova != a.Situacao)
        {
            sp.GetRequiredService<IAuditoriaService>().RegistrarSistema(nameof(Assinatura), a.Id, AcaoAuditoria.Editado,
                $"Situação da assinatura alterada de \"{Rotulo(a.Situacao)}\" para \"{Rotulo(nova)}\"");
            if (nova == SituacaoAssinatura.Suspensa) a.SuspensaEm = DateTime.UtcNow;
            // Voltou a pagar: uma próxima suspensão avisa de novo.
            if (nova == SituacaoAssinatura.Ativa) a.AvisoSuspensaoEnviadoEm = null;
            a.Situacao = nova;
        }
        a.SituacaoAtualizadaEm = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await EnviarAvisosAsync(db, sp, a, hoje, ct);
        await EnviarAvisoFaturaAsync(db, sp, a, hoje, faturas, ct);
    }

    /// <summary>Depois da 1ª fatura paga, o valor segue os atletas <b>ativos</b> (no teste vale o número informado). Falha no gateway só
    /// adia: a próxima atualização tenta de novo.</summary>
    private async Task AtualizarValorAsync(EscolaDbContext db, IServiceProvider sp, Assinatura a, int mensalidadesPagas, CancellationToken ct)
    {
        var ativos = await db.Alunos.CountAsync(x => x.Ativo, ct);
        var valor = opcoes.Value.CalcularValor(ativos);
        // Cupom: desconto nas N primeiras mensalidades (ou sempre); passou disso, o valor volta ao cheio.
        if (CupomRegras.DescontoVigente(a.CupomPercentual, a.CupomMeses, mensalidadesPagas))
            valor = CupomRegras.AplicarDesconto(valor, a.CupomPercentual);
        if (valor == a.ValorMensal && a.AtletasCobrados == ativos) return;

        try
        {
            if (valor != a.ValorMensal)
                await asaas.AtualizarValorAssinaturaAsync(a.IdAssinaturaGateway!, valor, $"{MarcaProduto.Nome} — plano mensal ({ativos} atletas)", ct);
        }
        catch (PixProvedorException ex)
        {
            logger.LogWarning(ex, "Não foi possível atualizar o valor da assinatura {AssinaturaId} no gateway.", a.Id);
            return;
        }

        if (valor != a.ValorMensal)
            sp.GetRequiredService<IAuditoriaService>().RegistrarSistema(nameof(Assinatura), a.Id, AcaoAuditoria.Editado,
                $"Valor do plano alterado de \"{Moeda(a.ValorMensal)}\" para \"{Moeda(valor)}\" ({ativos} atletas ativos)");
        a.ValorMensal = valor;
        a.AtletasCobrados = ativos;
    }

    public async Task<(DateOnly? AcessoAte, string? Erro)> CancelarAsync(Guid clienteId, Guid usuarioId, CancellationToken ct = default)
    {
        using var escopo = escopos.CreateScope();
        escopo.ServiceProvider.GetRequiredService<ClienteAtual>().Definir(clienteId);
        var db = escopo.ServiceProvider.GetRequiredService<EscolaDbContext>();
        var a = await db.Assinaturas.FirstOrDefaultAsync(ct);
        if (a is null) return (null, "Este clube não tem assinatura pelo site.");
        if (a.AcessoAte is not null || a.Situacao == SituacaoAssinatura.Cancelada) return (null, "A assinatura já foi cancelada.");

        IReadOnlyList<FaturaAssinatura> faturas = [];
        if (GatewayDaAssinatura(a))
        {
            var (lidas, disponivel) = await FaturasAsync(a, ct);
            if (!disponivel) return (null, "Não foi possível falar com o gateway de pagamento agora. Tente de novo em instantes.");
            faturas = lidas;
            try
            {
                await asaas.CancelarAssinaturaAsync(a.IdAssinaturaGateway!, ct);
            }
            catch (PixProvedorException ex)
            {
                logger.LogWarning(ex, "Não foi possível cancelar a assinatura {AssinaturaId} no gateway.", a.Id);
                return (null, "Não foi possível cancelar no gateway de pagamento agora. Tente de novo em instantes.");
            }
        }

        var hoje = await escopo.ServiceProvider.GetRequiredService<IRelogioEscola>().HojeAsync();
        if (faturas.Any(f => f.Paga)) a.JaPagou = true; // depois do cancelamento as faturas não são mais lidas
        a.AcessoAte = SituacaoAssinaturaCalculo.AcessoAteAoCancelar(hoje, a.TesteAte, a.PrimeiroVencimento, faturas);
        a.CanceladaEm = DateTime.UtcNow;
        a.LinkPagamento = null;
        escopo.ServiceProvider.GetRequiredService<IAuditoriaService>().Registrar(nameof(Assinatura), a.Id, AcaoAuditoria.Editado, usuarioId,
            $"Assinatura cancelada: acesso até {a.AcessoAte:dd/MM/yyyy}");
        await db.SaveChangesAsync(ct);
        return (a.AcessoAte, null);
    }

    public async Task<string?> ReativarAsync(Guid clienteId, Guid usuarioId, CancellationToken ct = default)
    {
        using var escopo = escopos.CreateScope();
        escopo.ServiceProvider.GetRequiredService<ClienteAtual>().Definir(clienteId);
        var sp = escopo.ServiceProvider;
        var db = sp.GetRequiredService<EscolaDbContext>();
        var a = await db.Assinaturas.FirstOrDefaultAsync(ct);
        if (a is null) return "Este clube não tem assinatura pelo site.";
        if (a.AcessoAte is not { } acessoAte) return "A assinatura não está cancelada.";

        var hoje = await sp.GetRequiredService<IRelogioEscola>().HojeAsync();
        // Ainda dentro do acesso: o período já pago (ou o teste) vale, e a próxima fatura vence quando ele acabar. Depois dele, vence hoje
        // — e quem nunca pagou e já passou do teste volta como "aguardando pagamento" (sem um teste novo).
        var dentroDoAcesso = hoje <= acessoAte;
        // Quem nunca pagou e ainda está dentro do teste grátis não é cobrado antes do fim dele.
        var vencimento = dentroDoAcesso ? acessoAte : !a.JaPagou && a.TesteAte is { } testeAte && testeAte >= hoje ? testeAte : hoje;
        if (!dentroDoAcesso && a.TesteAte is { } fimTeste && hoje > fimTeste) a.TesteAte = null;

        a.PrimeiroVencimento = vencimento;
        a.VencimentoEmAberto = vencimento;
        a.AcessoAte = null;
        a.CanceladaEm = null;
        a.LinkPagamento = null;
        a.AvisoExclusaoEnviadoEm = null;
        a.IdAssinaturaGateway = null; // a cancelada não volta: abre uma nova
        sp.GetRequiredService<IAuditoriaService>().Registrar(nameof(Assinatura), a.Id, AcaoAuditoria.Editado, usuarioId,
            $"Assinatura reativada: próxima fatura vence em {vencimento:dd/MM/yyyy}");
        await db.SaveChangesAsync(ct);

        var nomeClube = await db.Clientes.Where(c => c.Id == clienteId).Select(c => c.Nome).FirstAsync(ct);
        await GarantirNoGatewayAsync(db, a, nomeClube, hoje, ct);
        await SincronizarFaturasAsync(db, sp, a, ct);
        return null;
    }

    public async Task ProcessarRetencaoAsync(Guid clienteId, CancellationToken ct = default)
    {
        using var escopo = escopos.CreateScope();
        escopo.ServiceProvider.GetRequiredService<ClienteAtual>().Definir(clienteId);
        var sp = escopo.ServiceProvider;
        var db = sp.GetRequiredService<EscolaDbContext>();
        var a = await db.Assinaturas.FirstOrDefaultAsync(ct);
        if (a is null) return;

        var o = opcoes.Value;
        var data = SituacaoAssinaturaCalculo.DataExclusao(a.Situacao, a.AcessoAte, a.SuspensaEm, a.JaPagou, o.DiasRetencaoAposSuspensao);
        if (data is not { } dataExclusao)
        {
            // Pagou ou reativou: o processo de exclusão para.
            if (a.AvisoExclusaoEnviadoEm is not null)
            {
                a.AvisoExclusaoEnviadoEm = null;
                await db.SaveChangesAsync(ct);
            }
            return;
        }

        var hoje = await sp.GetRequiredService<IRelogioEscola>().HojeAsync();
        var nomeClube = await db.Clientes.Where(c => c.Id == clienteId).Select(c => c.Nome).FirstAsync(ct);

        if (a.AvisoExclusaoEnviadoEm is null)
        {
            if (hoje < dataExclusao.AddDays(-SituacaoAssinaturaCalculo.DiasAvisoExclusao)) return;
            // O relógio da exclusão começa no aviso (mesmo sem SMTP, pra não adiar pra sempre): ela só acontece 7 dias depois dele.
            await EnviarAvisoExclusaoAsync(sp, a, nomeClube, Maior(dataExclusao, hoje.AddDays(SituacaoAssinaturaCalculo.DiasAvisoExclusao)), ct);
            a.AvisoExclusaoEnviadoEm = DateTime.UtcNow;
            sp.GetRequiredService<IAuditoriaService>().RegistrarSistema(nameof(Assinatura), a.Id, AcaoAuditoria.Editado,
                $"Aviso de exclusão dos dados enviado: o clube será apagado a partir de {dataExclusao:dd/MM/yyyy}");
            await db.SaveChangesAsync(ct);
            return;
        }

        if (hoje < dataExclusao || a.AvisoExclusaoEnviadoEm > DateTime.UtcNow.AddDays(-SituacaoAssinaturaCalculo.DiasAvisoExclusao)) return;

        if (!o.ExclusaoAutomatica)
        {
            logger.LogWarning("Os dados do cliente {ClienteId} ({Nome}) já podem ser excluídos ({Situacao}), mas Assinatura:ExclusaoAutomatica está desligada.",
                clienteId, nomeClube, a.Situacao);
            return;
        }
        await sp.GetRequiredService<ExclusaoDadosCliente>().ApagarAsync(clienteId, ct);
    }

    private static DateOnly Maior(DateOnly a, DateOnly b) => a > b ? a : b;

    private async Task EnviarAvisoExclusaoAsync(IServiceProvider sp, Assinatura a, string nomeClube, DateOnly quando, CancellationToken ct)
    {
        var email = sp.GetRequiredService<IEmailSender>();
        if (!email.Configurado) return;
        var urlAssinatura = $"{config["App:UrlBase"]?.TrimEnd('/')}/configuracoes/assinatura";
        var comoManter = a.Situacao == SituacaoAssinatura.Cancelada
            ? "reative a assinatura"
            : "pague a mensalidade em aberto";
        try
        {
            await email.EnviarAsync(a.EmailCobranca, nomeClube, $"Os dados do {nomeClube} serão apagados em {quando:dd/MM}", $"""
                <p>Olá!</p>
                <p>Como a assinatura do <strong>{Html(nomeClube)}</strong> no {MarcaProduto.Nome} não está ativa, <strong>todos os dados do clube
                serão apagados a partir de {quando:dd/MM/yyyy}</strong>: atletas, famílias, equipe, financeiro, jogos, fotos e documentos. Não
                será possível recuperar depois.</p>
                <p>Para manter os dados, {comoManter} antes dessa data: <a href="{Html(urlAssinatura)}">abrir a assinatura</a>.
                Se quiser só guardar uma cópia, responda este e-mail pedindo a exportação.</p>
                """, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Não foi possível enviar o aviso de exclusão da assinatura {AssinaturaId}.", a.Id);
        }
    }

    /// <summary>Os avisos que só o produto sabe dar: o teste acabando e o acesso suspenso. Sem SMTP não envia nem marca — sai quando o
    /// e-mail estiver configurado, se ainda fizer sentido.</summary>
    private async Task EnviarAvisosAsync(EscolaDbContext db, IServiceProvider sp, Assinatura a, DateOnly hoje, CancellationToken ct)
    {
        var email = sp.GetRequiredService<IEmailSender>();
        if (!email.Configurado) return;

        var lembreteTeste = a.Situacao == SituacaoAssinatura.EmTeste && a.AcessoAte is null && a.LembreteTesteEnviadoEm is null
            && a.TesteAte is { } fim && fim.DayNumber - hoje.DayNumber is >= 0 and <= DiasLembreteTeste;
        var avisoSuspensao = a.Situacao == SituacaoAssinatura.Suspensa && a.AvisoSuspensaoEnviadoEm is null;
        if (!lembreteTeste && !avisoSuspensao) return;

        var clienteId = db.Entry(a).Property<Guid>(EscolaDbContext.ColunaCliente).CurrentValue;
        var nomeClube = await db.Clientes.Where(c => c.Id == clienteId)
            .Select(c => c.Nome).FirstOrDefaultAsync(ct) ?? "seu clube";
        var urlAssinatura = $"{config["App:UrlBase"]?.TrimEnd('/')}/configuracoes/assinatura";
        var linkPagar = a.LinkPagamento ?? urlAssinatura;

        var (assunto, corpo) = lembreteTeste
            ? ($"Seu teste grátis termina em {a.TesteAte:dd/MM} — {nomeClube}",
               $"""
                <p>Olá!</p>
                <p>O teste grátis do <strong>{Html(nomeClube)}</strong> no {MarcaProduto.Nome} termina em <strong>{a.TesteAte:dd/MM/yyyy}</strong>.</p>
                <p>Para continuar usando sem interrupção, pague a 1ª mensalidade ({Html(Moeda(a.ValorMensal))}) com Pix, boleto ou cartão:</p>
                <p><a href="{Html(linkPagar)}">Pagar agora</a></p>
                <p>Se não quiser continuar, não precisa fazer nada.</p>
                """)
            : ($"Acesso suspenso — {nomeClube}",
               $"""
                <p>Olá!</p>
                <p>O acesso do <strong>{Html(nomeClube)}</strong> ao {MarcaProduto.Nome} foi <strong>suspenso</strong> por falta de pagamento.
                A equipe e as famílias não conseguem entrar até a mensalidade ser paga. Nenhum dado foi apagado.</p>
                <p><a href="{Html(linkPagar)}">Pagar agora</a></p>
                <p>Assim que o pagamento for confirmado, tudo volta ao normal.</p>
                """);

        try
        {
            await email.EnviarAsync(a.EmailCobranca, nomeClube, assunto, corpo, ct);
            if (lembreteTeste) a.LembreteTesteEnviadoEm = DateTime.UtcNow;
            else a.AvisoSuspensaoEnviadoEm = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Não foi possível enviar o aviso da assinatura {AssinaturaId}.", a.Id);
        }
    }

    /// <summary>Lembrete de vencimento, atraso e recibo de cada fatura (as notificações do Asaas ficam desligadas: são pagas). Um aviso
    /// por rodada; sem SMTP não envia nem marca.</summary>
    private async Task EnviarAvisoFaturaAsync(EscolaDbContext db, IServiceProvider sp, Assinatura a, DateOnly hoje,
        IReadOnlyList<FaturaAssinatura> faturas, CancellationToken ct)
    {
        var email = sp.GetRequiredService<IEmailSender>();
        if (!email.Configurado || faturas.Count == 0) return;
        if (AvisosFaturaCalculo.Proximo(hoje, a.Situacao, faturas, a.LembreteFaturaId, a.AvisoAtrasoFaturaId, a.ReciboFaturaId)
            is not { } aviso) return;

        var clienteId = db.Entry(a).Property<Guid>(EscolaDbContext.ColunaCliente).CurrentValue;
        var nomeClube = await db.Clientes.Where(c => c.Id == clienteId).Select(c => c.Nome).FirstOrDefaultAsync(ct) ?? "seu clube";
        var f = aviso.Fatura;
        var link = f.LinkPagamento ?? $"{config["App:UrlBase"]?.TrimEnd('/')}/configuracoes/assinatura";
        var valor = Html(Moeda(f.Valor));
        const string comoAparece = $"<p style=\"color:#666\">Na fatura e no extrato, a cobrança aparece como {MarcaProduto.EmpresaCobranca}.</p>";

        var (assunto, corpo) = aviso.Tipo switch
        {
            TipoAvisoFatura.Lembrete => ($"Sua mensalidade vence em {f.Vencimento:dd/MM} — {nomeClube}",
                $"""
                <p>Olá!</p>
                <p>A mensalidade do <strong>{Html(nomeClube)}</strong> no {MarcaProduto.Nome}, de <strong>{valor}</strong>, vence em
                <strong>{f.Vencimento:dd/MM/yyyy}</strong>.</p>
                <p><a href="{Html(link)}">Pagar agora</a></p>
                <p>Pix, boleto ou cartão de crédito.</p>
                {comoAparece}
                """),
            TipoAvisoFatura.Atraso => ($"Mensalidade em atraso — {nomeClube}",
                $"""
                <p>Olá!</p>
                <p>A mensalidade do <strong>{Html(nomeClube)}</strong> no {MarcaProduto.Nome}, de <strong>{valor}</strong>, venceu em
                <strong>{f.Vencimento:dd/MM/yyyy}</strong> e ainda não foi paga.</p>
                <p>O acesso continua normal até <strong>{f.Vencimento.AddDays(opcoes.Value.DiasToleranciaAtraso):dd/MM/yyyy}</strong>. Depois
                disso, a equipe e as famílias deixam de conseguir entrar até o pagamento.</p>
                <p><a href="{Html(link)}">Pagar agora</a></p>
                <p>Pix, boleto ou cartão de crédito. Se já pagou, desconsidere este e-mail.</p>
                {comoAparece}
                """),
            _ => ($"Pagamento confirmado — {nomeClube}",
                $"""
                <p>Olá!</p>
                <p>Recebemos o pagamento de <strong>{valor}</strong> da mensalidade do <strong>{Html(nomeClube)}</strong> no {MarcaProduto.Nome}
                (vencimento {f.Vencimento:dd/MM/yyyy}). Obrigado!</p>
                <p><a href="{Html(link)}">Ver a fatura e o comprovante</a></p>
                {comoAparece}
                """)
        };

        try
        {
            await email.EnviarAsync(a.EmailCobranca, nomeClube, assunto, corpo, ct);
            switch (aviso.Tipo)
            {
                case TipoAvisoFatura.Lembrete: a.LembreteFaturaId = f.Id; break;
                case TipoAvisoFatura.Atraso: a.AvisoAtrasoFaturaId = f.Id; break;
                default: a.ReciboFaturaId = f.Id; break;
            }
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Não foi possível enviar o aviso de fatura da assinatura {AssinaturaId}.", a.Id);
        }
    }

    /// <summary>Dias antes do fim do teste em que sai o lembrete.</summary>
    public const int DiasLembreteTeste = 2;

    private static string Html(string texto) => System.Net.WebUtility.HtmlEncode(texto);

    private async Task<bool> EnviarBoasVindasAsync(EscolaDbContext db, IServiceProvider sp, Assinatura a, Usuario admin, string nomeClube, CancellationToken ct)
    {
        var detalhe = a.Situacao == SituacaoAssinatura.EmTeste && a.TesteAte is { } ate
            ? $"Seu teste grátis vai até {ate:dd/MM/yyyy}. Depois, o plano fica em {Plano(a)} — você escolhe pagar com Pix, boleto ou cartão."
            : $"Pagamento confirmado. Plano: {Plano(a)}.";
        var resultado = await sp.GetRequiredService<ILinkSenhaService>().EnviarAsync(admin, TipoLinkSenha.BoasVindasAssinatura, nomeClube, detalhe: detalhe);
        // Marca mesmo sem entrega (sem SMTP): reenviar toda hora só invalidaria o link anterior. O Suporte reenvia pela Plataforma.
        if (resultado.Gerado)
        {
            a.ConviteEnviadoEm = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
        return resultado.Entregue;
    }
}
