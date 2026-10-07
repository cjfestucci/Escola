namespace Escola.Infrastructure.Pagamentos;

/// <summary>Configuração da integração com a API Pix do Banco do Brasil (seção <c>Pix:Bb</c>). Vem do deploy (como o SMTP e o JWT),
/// não do banco: são credenciais técnicas do cliente, não dado de negócio editável por Gestão. Sem <c>ClientId</c>,
/// <c>ClientSecret</c> e <c>ChaveAplicacao</c> a integração fica desligada e o app continua gerando o Pix estático de sempre.</summary>
public class OpcoesPixBb
{
    public const string Secao = "Pix:Bb";

    /// <summary>"Homologacao" (padrão — o seguro) ou "Producao". Define os endereços e o nome do parâmetro da chave de aplicação.</summary>
    public string Ambiente { get; set; } = "Homologacao";

    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }

    /// <summary>"Developer application key" do portal de desenvolvedores do BB.</summary>
    public string? ChaveAplicacao { get; set; }

    /// <summary>Endereços (só preencher pra sobrescrever o padrão do ambiente — por exemplo apontando pra um servidor de teste).</summary>
    public string? UrlOauth { get; set; }
    public string? UrlApi { get; set; }

    /// <summary>Escopos pedidos no OAuth (separados por espaço).</summary>
    public string Escopos { get; set; } = "cob.read cob.write pix.read webhook.read webhook.write";

    /// <summary>Nome do parâmetro de query que leva a chave de aplicação (o BB usa nomes diferentes em homologação e produção).</summary>
    public string? ParametroChaveAplicacao { get; set; }

    /// <summary>Certificado do cliente (.pfx) para o mTLS do BB — obrigatório em produção.</summary>
    public string? CertificadoPfxCaminho { get; set; }
    public string? CertificadoPfxSenha { get; set; }

    /// <summary>Endereço público da API (ex.: https://api.suaescola.com.br) — só pra registrar o webhook no BB.</summary>
    public string? WebhookUrlBase { get; set; }

    /// <summary>Trecho secreto que vai no caminho do webhook. Sem ele o webhook fica desligado (e a baixa segue pela consulta periódica).</summary>
    public string? WebhookSegredo { get; set; }

    /// <summary>De quanto em quanto tempo o app pergunta ao banco se as cobranças pendentes foram pagas.</summary>
    public int IntervaloConciliacaoSegundos { get; set; } = 120;

    /// <summary>Cliente (escola) dono destas credenciais. Desde que todos os clientes compartilham o mesmo deploy (2026-10-07), as
    /// credenciais do BB ainda vêm da configuração do deploy — então a integração só vale pra UM cliente. Sem valor, é o cliente
    /// inicial (<c>Cliente:Id</c>). Próximo passo: credenciais por cliente no banco, criptografadas.</summary>
    public Guid? ClienteId { get; set; }

    public bool Configurado =>
        !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret) && !string.IsNullOrWhiteSpace(ChaveAplicacao);

    public bool Producao => string.Equals(Ambiente, "Producao", StringComparison.OrdinalIgnoreCase);

    public string UrlOauthEfetiva => UrlOauth ?? (Producao ? "https://oauth.bb.com.br/oauth/token" : "https://oauth.hm.bb.com.br/oauth/token");
    public string UrlApiEfetiva => (UrlApi ?? (Producao ? "https://api.bb.com.br/pix/v2" : "https://api.hm.bb.com.br/pix/v2")).TrimEnd('/');
    public string ParametroChaveAplicacaoEfetivo => ParametroChaveAplicacao ?? (Producao ? "gw-app-key" : "gw-dev-app-key");

    public bool WebhookHabilitado => !string.IsNullOrWhiteSpace(WebhookSegredo);
}
