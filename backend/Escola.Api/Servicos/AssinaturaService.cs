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
    string NomeAdmin, string Email, string Celular, string? Ip, string? Navegador);

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
    public const string VersaoTermosUso = "2026-10-13";

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
            ValorMensal = o.CalcularValor(dados.Atletas),
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
        $"{Moeda(a.ValorMensal)} por mês ({a.AtletasCobrados ?? a.AtletasInformados} atletas)";

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

        if (a.AcessoAte is { } acessoAte)
        {
            // Cancelamento pedido: a assinatura já saiu do gateway; até o fim do acesso nada muda, depois vira Cancelada.
            nova = hoje > acessoAte ? SituacaoAssinatura.Cancelada : a.Situacao;
        }
        else
        {
            IReadOnlyList<FaturaAssinatura> faturas = [];
            if (GatewayDaAssinatura(a))
            {
                var (lidas, disponivel) = await FaturasAsync(a, ct);
                if (!disponivel) return;
                faturas = lidas;
            }

            nova = SituacaoAssinaturaCalculo.Calcular(hoje, a.TesteAte, a.PrimeiroVencimento, opcoes.Value.DiasToleranciaAtraso, null, faturas);
            var aberta = SituacaoAssinaturaCalculo.EmAberto(faturas);
            a.LinkPagamento = aberta?.LinkPagamento;
            a.VencimentoEmAberto = faturas.Count > 0 ? aberta?.Vencimento : nova == SituacaoAssinatura.Ativa ? null : a.PrimeiroVencimento;

            if (GatewayDaAssinatura(a) && SituacaoAssinaturaCalculo.PodeAtualizarValor(hoje, faturas))
                await AtualizarValorAsync(db, sp, a, ct);
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
    }

    /// <summary>Depois da 1ª fatura paga, o valor segue os atletas <b>ativos</b> (no teste vale o número informado). Falha no gateway só
    /// adia: a próxima atualização tenta de novo.</summary>
    private async Task AtualizarValorAsync(EscolaDbContext db, IServiceProvider sp, Assinatura a, CancellationToken ct)
    {
        var ativos = await db.Alunos.CountAsync(x => x.Ativo, ct);
        var valor = opcoes.Value.CalcularValor(ativos);
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
        a.AcessoAte = SituacaoAssinaturaCalculo.AcessoAteAoCancelar(hoje, a.TesteAte, a.PrimeiroVencimento, faturas);
        a.CanceladaEm = DateTime.UtcNow;
        a.LinkPagamento = null;
        escopo.ServiceProvider.GetRequiredService<IAuditoriaService>().Registrar(nameof(Assinatura), a.Id, AcaoAuditoria.Editado, usuarioId,
            $"Assinatura cancelada: acesso até {a.AcessoAte:dd/MM/yyyy}");
        await db.SaveChangesAsync(ct);
        return (a.AcessoAte, null);
    }

    /// <summary>Os avisos que só o produto sabe dar: o teste acabando e o acesso suspenso. (Fatura gerada/vencendo/vencida o próprio Asaas
    /// avisa ao cliente.) Sem SMTP não envia nem marca — sai quando o e-mail estiver configurado, se ainda fizer sentido.</summary>
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
                <p><a href="{Html(linkPagar)}">Pagar agora</a> — assim que o pagamento for confirmado, tudo volta ao normal.</p>
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
