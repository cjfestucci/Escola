using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Escola.Api.Auth;
using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Escola.Infrastructure.Documentos;
using Escola.Infrastructure.Auditoria;
using Escola.Infrastructure.Clientes;
using Escola.Infrastructure.Data;
using Escola.Infrastructure.Pagamentos;
using Escola.Infrastructure.Pagamentos.Asaas;
using Escola.Infrastructure.Seguranca;
using Escola.Infrastructure.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Escola.Api.Controllers;

/// <param name="Disponivel">A plataforma tem o Asaas configurado neste ambiente (sem isso, a escola não tem como conectar).</param>
public record ContaPagamentoDto(
    bool Disponivel,
    string Ambiente,
    bool Conectada,
    string? TitularNome,
    string? TitularCpfCnpj,
    string? TitularEmail,
    bool ChavePixCriada,
    bool WebhookConfigurado,
    string? SituacaoGateway,
    DateTime? CriadaEm,
    bool PagamentoAutomaticoAtivo,
    IReadOnlyList<DocumentoPendenteDto>? Documentos = null,
    string? MotivoRecusa = null,
    bool DocumentosEmPreparo = false);

/// <param name="Id">Identificador do grupo de documentos no Asaas (vai na rota do envio pela API).</param>
/// <param name="Situacao">Não enviado, Em análise ou Recusado.</param>
/// <param name="UrlEnvio">Página do Asaas pra enviar este documento (a selfie, por exemplo). Com ela o envio é só por lá.</param>
/// <param name="EnvioPelaApi">Sem página do Asaas: o arquivo é enviado aqui mesmo, pelo sistema (<c>POST conta/documentos/{id}</c>).</param>
public record DocumentoPendenteDto(string Id, string Titulo, string? Descricao, string Situacao, string? UrlEnvio, bool EnvioPelaApi);

/// <param name="TipoEmpresa">Só pra CNPJ: MEI, LIMITED, INDIVIDUAL ou ASSOCIATION.</param>
/// <param name="DataNascimento">Só pra CPF (titular pessoa física).</param>
public record ConectarContaPagamentoRequest(
    string Nome,
    string Email,
    string CpfCnpj,
    string? TipoEmpresa,
    DateOnly? DataNascimento,
    string Celular,
    string Cep,
    string Endereco,
    string Numero,
    string? Complemento,
    string Bairro,
    decimal FaturamentoMensal);

/// <summary>Pagamento automático pelo gateway (Asaas, modelo marketplace): a plataforma abre uma <b>subconta</b> pra escola, as
/// mensalidades são cobradas nela por Pix com baixa automática, e a escola nunca entra no painel do Asaas.</summary>
[ApiController]
[Route("api/pagamentos")]
[Authorize]
public class PagamentosController(
    EscolaDbContext db,
    AsaasApi asaas,
    IOptions<OpcoesAsaas> opcoes,
    CofreSegredos cofre,
    IAuditoriaService auditoria,
    ClienteAtual clienteAtual,
    IPixAutomaticoService pixAutomatico,
    ILogger<PagamentosController> logger) : ControllerBase
{
    private static readonly string[] TiposEmpresa = ["MEI", "LIMITED", "INDIVIDUAL", "ASSOCIATION"];

    [HttpGet("conta")]
    [Authorize(Roles = GruposDePapeis.GestaoOuSuporte)]
    public async Task<ActionResult<ContaPagamentoDto>> ObterConta(CancellationToken ct) => Ok(await ComDocumentosAsync(await ContaAtualAsync(), ct));

    /// <summary>Abre a subconta da escola no Asaas (uma por escola). Dado da empresa (KYC) — só o Admin (dono da conta) e o Suporte.</summary>
    [HttpPost("conta")]
    [Authorize(Roles = GruposDePapeis.AdminOuSuporte)]
    public async Task<ActionResult<ContaPagamentoDto>> Conectar(ConectarContaPagamentoRequest request, CancellationToken ct)
    {
        var o = opcoes.Value;
        if (!o.Configurado || !o.SubcontasHabilitadas || !cofre.Disponivel)
            return BadRequest("O pagamento automático ainda não está disponível neste ambiente. Fale com o suporte.");
        if (await ContaAtualAsync() is not null)
            return BadRequest("Esta escola já tem uma conta de pagamento conectada.");

        var (dados, erro) = Validar(request);
        if (erro is not null) return BadRequest(erro);

        SubcontaCriada criada;
        try
        {
            criada = await asaas.CriarSubcontaAsync(dados!, ct);
        }
        catch (PixProvedorException ex)
        {
            return BadRequest(ex.Message);
        }

        var conta = new ContaPagamento
        {
            Id = Guid.NewGuid(),
            Provedor = ProvedorPagamento.Asaas,
            Ambiente = o.NomeAmbiente,
            IdExterno = criada.Id,
            WalletId = criada.WalletId,
            // A chave da subconta só aparece nesta resposta do Asaas: guardada já criptografada.
            ApiKeyCriptografada = cofre.Criptografar(criada.ApiKey),
            TitularNome = dados!.Nome,
            TitularCpfCnpj = dados.CpfCnpj,
            TitularEmail = dados.Email,
            WebhookConfigurado = o.WebhookHabilitado,
            CriadaEm = DateTime.UtcNow
        };
        db.ContasPagamento.Add(conta);
        auditoria.Registrar(nameof(ContaPagamento), conta.Id, AcaoAuditoria.Criado, this.UsuarioIdAtual(),
            $"Conta de pagamento conectada (Asaas, {conta.Ambiente}): {conta.TitularNome}");
        await db.SaveChangesAsync(ct);

        // A conta já existe no Asaas: daqui pra frente nada pode desfazer o registro acima. Chave Pix e situação são "melhor esforço".
        await ConcluirConfiguracaoAsync(conta, criada.ApiKey, ct);
        return Ok(ParaDto(conta) with { DocumentosEmPreparo = o.EsperaDocumentosSegundos > 0 && conta.SituacaoGateway != "APPROVED" });
    }

    /// <summary>Tenta de novo o que pode ter falhado depois da criação (chave Pix da subconta) e atualiza a situação da conta.</summary>
    [HttpPost("conta/atualizar")]
    [Authorize(Roles = GruposDePapeis.GestaoOuSuporte)]
    public async Task<ActionResult<ContaPagamentoDto>> Atualizar(CancellationToken ct)
    {
        var conta = await db.ContasPagamento.FirstOrDefaultAsync(c => c.Ambiente == opcoes.Value.NomeAmbiente, ct);
        if (conta is null) return NotFound("Esta escola ainda não tem conta de pagamento.");
        if (!cofre.Disponivel) return BadRequest("O cofre de segredos não está configurado neste ambiente (Segredos:Chave).");

        await ConcluirConfiguracaoAsync(conta, cofre.Descriptografar(conta.ApiKeyCriptografada), ct);
        return Ok(await ComDocumentosAsync(conta, ct));
    }

    private const long TamanhoMaximoDocumentoBytes = 10 * 1024 * 1024;

    /// <summary>Envia ao Asaas o arquivo de um documento que o parceiro pediu pra aprovar a conta (KYC) e que <b>não</b> tem página de
    /// envio do Asaas. O tipo do documento vem do próprio Asaas (nunca do cliente) e o formato do arquivo é conferido pelo conteúdo.</summary>
    [HttpPost("conta/documentos/{id}")]
    [Authorize(Roles = GruposDePapeis.AdminOuSuporte)]
    [RequestSizeLimit(TamanhoMaximoDocumentoBytes + 1024 * 1024)]
    public async Task<ActionResult<ContaPagamentoDto>> EnviarDocumento(string id, IFormFile arquivo, CancellationToken ct)
    {
        var conta = await db.ContasPagamento.FirstOrDefaultAsync(c => c.Ambiente == opcoes.Value.NomeAmbiente, ct);
        if (conta is null) return NotFound("Esta escola ainda não tem conta de pagamento.");
        if (!cofre.Disponivel) return BadRequest("O cofre de segredos não está configurado neste ambiente (Segredos:Chave).");

        if (arquivo is null || arquivo.Length == 0) return BadRequest("Nenhum arquivo enviado.");
        if (arquivo.Length > TamanhoMaximoDocumentoBytes) return BadRequest("Arquivo maior que o limite de 10MB.");

        byte[] conteudo;
        (string ContentType, string Extensao)? tipoArquivo;
        await using (var entrada = arquivo.OpenReadStream())
        {
            tipoArquivo = await DetectorDocumento.DetectarAsync(entrada, ct);
            entrada.Position = 0;
            using var memoria = new MemoryStream();
            await entrada.CopyToAsync(memoria, ct);
            conteudo = memoria.ToArray();
        }
        if (tipoArquivo is null or { ContentType: "image/webp" })
            return BadRequest("Formato não suportado. Envie um PDF ou uma imagem JPEG ou PNG.");

        var apiKey = cofre.Descriptografar(conta.ApiKeyCriptografada);
        DocumentoSubconta? documento;
        try
        {
            var (documentos, _) = await asaas.DocumentosSubcontaAsync(apiKey, ct);
            documento = documentos.FirstOrDefault(d => d.Id == id);
        }
        catch (PixProvedorException ex)
        {
            return BadRequest(ex.Message);
        }
        if (documento is null) return NotFound("Documento não encontrado. Use \"Atualizar situação\" e tente de novo.");
        if (documento.UrlEnvio is not null)
            return BadRequest("Este documento é enviado pela página segura do parceiro de pagamentos (botão \"Enviar\").");
        if (documento.Status is not ("NOT_SENT" or "REJECTED"))
            return BadRequest("Este documento não está aguardando envio.");

        var nome = Path.GetFileName(arquivo.FileName);
        if (string.IsNullOrWhiteSpace(nome)) nome = $"documento{tipoArquivo!.Value.Extensao}";
        try
        {
            await asaas.EnviarDocumentoAsync(apiKey, documento.Id, documento.Tipo, conteudo, nome, tipoArquivo!.Value.ContentType, ct);
        }
        catch (PixProvedorException ex)
        {
            return BadRequest(ex.Message);
        }

        auditoria.Registrar(nameof(ContaPagamento), conta.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(),
            $"Documento enviado ao parceiro de pagamentos: {documento.Titulo}");
        await db.SaveChangesAsync(ct);
        return Ok(await ComDocumentosAsync(conta, ct));
    }

    /// <summary>Aviso do Asaas de que algo aconteceu com um pagamento. <b>O corpo nunca é confiado</b>: serve só pra dizer qual
    /// cobrança conferir, e ela é consultada no Asaas (com a chave da escola dona) antes de qualquer baixa. Responde 200 até pra cobrança
    /// desconhecida — erro repetido faz o Asaas pausar a fila de webhooks.</summary>
    [HttpPost("asaas/webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook([FromBody] JsonElement corpo, CancellationToken ct)
    {
        var o = opcoes.Value;
        var recebido = Request.Headers["asaas-access-token"].ToString();
        if (string.IsNullOrWhiteSpace(o.WebhookToken) || !TokenConfere(recebido, o.WebhookToken))
            return Unauthorized();

        var pagamentoId = corpo.ValueKind == JsonValueKind.Object && corpo.TryGetProperty("payment", out var pagamento)
            && pagamento.TryGetProperty("id", out var id) ? id.GetString() : null;
        if (string.IsNullOrWhiteSpace(pagamentoId)) return Ok();

        // Sem login não há escola: ela vem da própria cobrança (o id do pagamento é único no banco todo).
        var clienteDaCobranca = await db.CobrancasPix.IgnoreQueryFilters()
            .Where(p => p.TxId == pagamentoId && p.Provedor == ProvedorPagamento.Asaas)
            .Select(p => EF.Property<Guid>(p, EscolaDbContext.ColunaCliente))
            .FirstOrDefaultAsync(ct);
        if (clienteDaCobranca == Guid.Empty) return Ok();

        clienteAtual.Definir(clienteDaCobranca);
        try
        {
            await pixAutomatico.ConfirmarAsync(pagamentoId, ct);
        }
        catch (PixProvedorException ex)
        {
            // A conferência periódica tenta de novo; o Asaas não precisa saber da nossa falha.
            logger.LogWarning(ex, "Webhook Asaas: não foi possível conferir o pagamento {PagamentoId}.", pagamentoId);
        }
        return Ok();
    }

    private async Task ConcluirConfiguracaoAsync(ContaPagamento conta, string apiKey, CancellationToken ct)
    {
        try
        {
            if (!conta.ChavePixCriada)
            {
                await asaas.CriarChavePixAleatoriaAsync(apiKey, ct);
                conta.ChavePixCriada = true;
            }
        }
        catch (PixProvedorException ex)
        {
            // Comum logo após abrir a conta (ainda em análise): fica pendente e a tela oferece "Atualizar".
            logger.LogWarning(ex, "Não foi possível criar a chave Pix da subconta {Conta}.", conta.IdExterno);
        }

        try
        {
            conta.SituacaoGateway = await asaas.SituacaoContaAsync(apiKey, ct);
            conta.SituacaoConsultadaEm = DateTime.UtcNow;
        }
        catch (PixProvedorException ex)
        {
            logger.LogWarning(ex, "Não foi possível consultar a situação da subconta {Conta}.", conta.IdExterno);
        }

        await db.SaveChangesAsync(ct);
    }

    /// <summary>Enquanto a conta não está aprovada, acrescenta os documentos que faltam (consulta ao vivo: o link de envio do Asaas
    /// expira). Falha na consulta só deixa a lista de fora.</summary>
    private async Task<ContaPagamentoDto> ComDocumentosAsync(ContaPagamento? conta, CancellationToken ct)
    {
        var dto = ParaDto(conta);
        if (conta is null || conta.SituacaoGateway == "APPROVED" || !cofre.Disponivel) return dto;

        // O Asaas pede ao menos 15 s entre criar a subconta e consultar os documentos: antes disso a lista vem incompleta.
        if (DateTime.UtcNow < conta.CriadaEm.AddSeconds(opcoes.Value.EsperaDocumentosSegundos))
            return dto with { Documentos = [], DocumentosEmPreparo = true };

        try
        {
            var (documentos, motivo) = await asaas.DocumentosSubcontaAsync(cofre.Descriptografar(conta.ApiKeyCriptografada), ct);
            var agora = DateTime.UtcNow;
            var pendentes = documentos
                .Where(d => d.Status is "NOT_SENT" or "PENDING" or "REJECTED")
                .Select(d => new DocumentoPendenteDto(d.Id, d.Titulo, d.Descricao, d.Status switch
                {
                    "PENDING" => "Em análise",
                    "REJECTED" => "Recusado",
                    _ => "Não enviado"
                    // Em análise não há o que enviar; link vencido só confundiria (o próximo "Atualizar" traz um novo).
                }, d.Status != "PENDING" && (d.UrlExpiraEm is null || d.UrlExpiraEm > agora) ? d.UrlEnvio : null,
                    // Documento com link do Asaas (mesmo vencido) nunca vai pela API: o Asaas recusa.
                    d.Status != "PENDING" && d.UrlEnvio is null && !string.IsNullOrWhiteSpace(d.Id)))
                .ToList();
            return dto with { Documentos = pendentes, MotivoRecusa = string.IsNullOrWhiteSpace(motivo) ? null : motivo };
        }
        catch (PixProvedorException ex)
        {
            logger.LogWarning(ex, "Não foi possível consultar os documentos da subconta {Conta}.", conta.IdExterno);
            return dto;
        }
    }

    private async Task<ContaPagamento?> ContaAtualAsync() =>
        await db.ContasPagamento.AsNoTracking().FirstOrDefaultAsync(c => c.Ambiente == opcoes.Value.NomeAmbiente);

    private ContaPagamentoDto ParaDto(ContaPagamento? conta) => new(
        opcoes.Value.Configurado && opcoes.Value.SubcontasHabilitadas && cofre.Disponivel,
        opcoes.Value.NomeAmbiente,
        conta is not null,
        conta?.TitularNome,
        conta?.TitularCpfCnpj,
        conta?.TitularEmail,
        conta?.ChavePixCriada ?? false,
        conta?.WebhookConfigurado ?? false,
        conta?.SituacaoGateway,
        conta?.CriadaEm,
        conta is { ChavePixCriada: true });

    private static (DadosSubconta? Dados, string? Erro) Validar(ConectarContaPagamentoRequest r)
    {
        static string Digitos(string? s) => new((s ?? string.Empty).Where(char.IsDigit).ToArray());

        if (string.IsNullOrWhiteSpace(r.Nome)) return (null, "Informe o nome ou a razão social.");
        if (string.IsNullOrWhiteSpace(r.Email) || !r.Email.Contains('@')) return (null, "Informe um e-mail válido.");

        var documento = DocumentoBr.Normalizar(r.CpfCnpj); // CNPJ alfanumérico: mantém as letras
        var ehCpf = documento.Length == 11;
        if (!DocumentoBr.CpfOuCnpjValido(documento))
            return (null, "CPF ou CNPJ inválido.");

        string? tipoEmpresa = null;
        if (ehCpf)
        {
            if (r.DataNascimento is null) return (null, "Para CPF, informe a data de nascimento do titular.");
        }
        else
        {
            tipoEmpresa = r.TipoEmpresa?.Trim().ToUpperInvariant();
            if (tipoEmpresa is null || !TiposEmpresa.Contains(tipoEmpresa)) return (null, "Informe o tipo da empresa (MEI, Limitada, Individual ou Associação).");
        }

        var celular = Digitos(r.Celular);
        if (celular.Length is < 10 or > 11) return (null, "Informe um celular com DDD.");
        var cep = Digitos(r.Cep);
        if (cep.Length != 8) return (null, "Informe um CEP válido (8 dígitos).");
        if (string.IsNullOrWhiteSpace(r.Endereco) || string.IsNullOrWhiteSpace(r.Numero) || string.IsNullOrWhiteSpace(r.Bairro))
            return (null, "Informe o endereço completo (rua, número e bairro).");
        if (r.FaturamentoMensal <= 0) return (null, "Informe o faturamento mensal aproximado.");

        return (new DadosSubconta(r.Nome.Trim(), r.Email.Trim().ToLowerInvariant(), documento, ehCpf ? r.DataNascimento : null, tipoEmpresa,
            celular, r.Endereco.Trim(), r.Numero.Trim(), string.IsNullOrWhiteSpace(r.Complemento) ? null : r.Complemento.Trim(),
            r.Bairro.Trim(), cep, r.FaturamentoMensal), null);
    }

    private static bool TokenConfere(string recebido, string esperado) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(recebido), Encoding.UTF8.GetBytes(esperado));
}
