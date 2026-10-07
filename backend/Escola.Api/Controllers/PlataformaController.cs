using Escola.Api.Auth;
using Escola.Api.Dtos;
using Escola.Api.Dtos.Requests;
using Escola.Api.Servicos;
using Escola.Infrastructure.Auth;
using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Escola.Infrastructure.Auditoria;
using Escola.Infrastructure.Clientes;
using Escola.Infrastructure.Data;
using Escola.Infrastructure.Email;
using Escola.Infrastructure.Pagamentos;
using Escola.Infrastructure.Storage;
using Escola.Infrastructure.Tempo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Escola.Api.Controllers;

public record ClientePlataformaDto(Guid Id, string Nome, SegmentoCliente Segmento, bool Ativo, DateTime CriadoEm);

public record EditarClientePlataformaRequest(string Nome, SegmentoCliente Segmento, bool Ativo);

public record LogoDto(string? LogoUrl);

/// <param name="Pendente">Conta criada por convite que ainda não cadastrou a senha (o link de convite ainda não foi aceito).</param>
/// <param name="UltimoEnvioEm">Quando o último e-mail (convite ou link de senha) foi gerado pra essa conta.</param>
public record AdminEscolaDto(Guid Id, string Nome, string Email, bool Ativo, bool Pendente, DateTime? UltimoEnvioEm);

public record CriarAdminEscolaRequest(string Nome, string Email);

/// <param name="EmailEnviado">Falso se o e-mail não foi entregue (ver <c>Aviso</c>); a conta/link existem do mesmo jeito e dá pra reenviar.</param>
public record ResultadoEnvioAdminDto(AdminEscolaDto Admin, bool EmailEnviado, string? Aviso);

/// <summary>Sem nenhum segredo: só diz se cada integração do ambiente está configurada.</summary>
public record DiagnosticoPlataformaDto(
    string Ambiente,
    bool SmtpConfigurado,
    bool UrlBaseConfigurada,
    bool PixAutomaticoConfigurado,
    string PixAutomaticoAmbiente,
    bool PixCertificadoConfigurado,
    bool PixWebhookHabilitado);

/// <summary>Área da equipe do produto (papel <c>Suporte</c>) sobre o <b>cliente deste ambiente</b>: dados do cliente (nome, segmento,
/// acesso liberado/suspenso) e um diagnóstico do ambiente. Só o Suporte entra aqui — nem Admin do cliente.</summary>
[ApiController]
[Route("api/plataforma")]
[Authorize(Roles = GruposDePapeis.Suporte)]
public class PlataformaController(
    EscolaDbContext db,
    IClienteAtual clienteAtual,
    IAuditoriaService auditoria,
    IConfiguration config,
    IHostEnvironment ambiente,
    IEmailSender emailSender,
    IOptions<OpcoesPixBb> pixBb,
    IFotoStorage fotoStorage,
    ILinkSenhaService linkSenha) : ControllerBase
{
    /// <summary>Logo é mostrada na tela de login e no menu: precisa ser leve. (GIF e SVG ficam de fora: animação não combina com
    /// logo, e SVG é XML que pode carregar script.)</summary>
    private const long TamanhoMaximoLogoBytes = 2 * 1024 * 1024;

    private static readonly HashSet<string> TiposLogo = new() { "image/png", "image/jpeg", "image/webp" };

    [HttpGet("cliente")]
    public async Task<ActionResult<ClientePlataformaDto>> ObterCliente()
    {
        var cliente = await db.Clientes.FirstOrDefaultAsync(c => c.Id == clienteAtual.Id);
        return cliente is null ? NotFound("Cliente não encontrado.") : Ok(ParaDto(cliente));
    }

    [HttpPut("cliente")]
    public async Task<ActionResult<ClientePlataformaDto>> EditarCliente(EditarClientePlataformaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nome)) return BadRequest("Informe o nome do cliente.");
        if (request.Nome.Trim().Length > 200) return BadRequest("O nome do cliente pode ter no máximo 200 caracteres.");
        if (!Enum.IsDefined(request.Segmento)) return BadRequest("Segmento inválido.");

        var cliente = await db.Clientes.FirstOrDefaultAsync(c => c.Id == clienteAtual.Id);
        if (cliente is null) return NotFound("Cliente não encontrado.");

        var nomeAntes = cliente.Nome;
        var segmentoAntes = cliente.Segmento;
        var ativoAntes = cliente.Ativo;

        cliente.Nome = request.Nome.Trim();
        cliente.Segmento = request.Segmento;
        cliente.Ativo = request.Ativo;

        var detalhe = AuditoriaDetalhe.MontarAlteracoes(
            ("Nome do cliente", nomeAntes, cliente.Nome),
            ("Segmento", segmentoAntes.ToString(), cliente.Segmento.ToString()),
            ("Acesso do cliente liberado", ativoAntes, cliente.Ativo));

        if (detalhe is not null)
            auditoria.Registrar(nameof(Cliente), cliente.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(), detalhe);
        await db.SaveChangesAsync();
        return Ok(ParaDto(cliente));
    }

    /// <summary>Define a logo do cliente (aparece ao lado do nome do app, inclusive na tela de login). Substitui a anterior.</summary>
    [HttpPut("logo")]
    [RequestSizeLimit(TamanhoMaximoLogoBytes + 64 * 1024)]
    public async Task<ActionResult<LogoDto>> EnviarLogo(IFormFile arquivo, CancellationToken ct)
    {
        if (arquivo is null || arquivo.Length == 0) return BadRequest("Nenhum arquivo enviado.");
        if (arquivo.Length > TamanhoMaximoLogoBytes) return BadRequest("A logo pode ter no máximo 2MB.");

        await using var stream = arquivo.OpenReadStream();
        // Tipo pelo conteúdo, não pelo que o cliente declara (ver DetectorImagem).
        var tipo = await DetectorImagem.DetectarAsync(stream, ct);
        if (tipo is null || !TiposLogo.Contains(tipo.Value.ContentType))
            return BadRequest("Formato não suportado. Use uma imagem PNG, JPEG ou WEBP.");

        var url = await fotoStorage.SalvarAsync(stream, $"logo{tipo.Value.Extensao}", tipo.Value.ContentType, ct);
        await DefinirLogoAsync(url, "Logo da empresa alterada");
        return Ok(new LogoDto(url));
    }

    [HttpDelete("logo")]
    public async Task<ActionResult<LogoDto>> RemoverLogo()
    {
        await DefinirLogoAsync(null, "Logo da empresa removida");
        return Ok(new LogoDto(null));
    }

    private async Task DefinirLogoAsync(string? url, string detalhe)
    {
        var config = await db.ConfiguracoesEscola.FirstOrDefaultAsync();
        var criando = config is null;
        if (config is null)
        {
            config = new ConfiguracaoEscola { Id = Guid.NewGuid(), FusoHorario = RelogioEscola.FusoPadrao };
            db.ConfiguracoesEscola.Add(config);
        }

        config.LogoUrl = url;
        config.AtualizadoEm = DateTime.UtcNow;
        auditoria.Registrar(nameof(ConfiguracaoEscola), config.Id, criando ? AcaoAuditoria.Criado : AcaoAuditoria.Editado, this.UsuarioIdAtual(), detalhe);
        await db.SaveChangesAsync();
    }

    /// <summary>Fuso horário da escola: define quando o dia vira no app inteiro (o "hoje" da rotina, da chamada, atrasos, vencimentos).
    /// Só o Suporte muda (saiu de Configurações → Geral em 2026-10-06: trocar o fuso no meio da vida mexe em datas de tudo). Fica no
    /// histórico da Configuração Geral.</summary>
    [HttpPut("fuso")]
    public async Task<ActionResult<ConfiguracaoEscolaDto>> EditarFuso(EditarFusoHorarioRequest request)
    {
        var fusoNovo = request.FusoHorario?.Trim();
        if (!RelogioEscola.FusoValido(fusoNovo))
            return BadRequest("Fuso horário inválido.");

        var config = await db.ConfiguracoesEscola.FirstOrDefaultAsync();
        var criando = config is null;
        if (config is null)
        {
            config = new ConfiguracaoEscola { Id = Guid.NewGuid(), FusoHorario = RelogioEscola.FusoPadrao };
            db.ConfiguracoesEscola.Add(config);
        }

        var fusoAntes = config.FusoHorario;
        config.FusoHorario = fusoNovo!;
        config.AtualizadoEm = DateTime.UtcNow;

        var detalhe = AuditoriaDetalhe.MontarAlteracoes(("Fuso horário", fusoAntes, config.FusoHorario));
        if (criando || detalhe is not null)
            auditoria.Registrar(nameof(ConfiguracaoEscola), config.Id, criando ? AcaoAuditoria.Criado : AcaoAuditoria.Editado, this.UsuarioIdAtual(), detalhe);
        await db.SaveChangesAsync();

        var segmento = await db.Clientes.Where(c => c.Id == clienteAtual.Id).Select(c => c.Segmento).FirstOrDefaultAsync();
        return Ok(new ConfiguracaoEscolaDto(config.Id, config.FusoHorario, config.CorPrincipal, segmento, config.LogoUrl));
    }

    // ----- Administradores da escola (convite por e-mail) -----

    /// <summary>Contas de Admin do cliente — só elas: o Suporte não vê (nem cria) as demais contas da escola.</summary>
    [HttpGet("admins")]
    public async Task<ActionResult<List<AdminEscolaDto>>> ListarAdmins()
    {
        var admins = await db.Usuarios.Where(u => u.Papel == PapelUsuario.Admin).OrderBy(u => u.Nome).ToListAsync();
        var ids = admins.Select(a => a.Id).ToList();
        var ultimos = await db.RedefinicoesSenha.Where(r => ids.Contains(r.UsuarioId))
            .GroupBy(r => r.UsuarioId).Select(g => new { UsuarioId = g.Key, Em = g.Max(r => r.CriadoEm) }).ToListAsync();
        var porUsuario = ultimos.ToDictionary(x => x.UsuarioId, x => x.Em);
        return Ok(admins.Select(a => ParaDto(a, porUsuario.TryGetValue(a.Id, out var em) ? em : null)).ToList());
    }

    /// <summary>Cria o Admin da escola <b>sem senha</b> e manda o convite por e-mail: a pessoa clica no link (confirmando que o e-mail é
    /// dela) e cadastra a própria senha. O Suporte nunca sabe nem define a senha de ninguém. Até aceitar, a conta fica "pendente" e não entra.</summary>
    [HttpPost("admins")]
    public async Task<ActionResult<ResultadoEnvioAdminDto>> CriarAdmin(CriarAdminEscolaRequest request)
    {
        var nome = request.Nome?.Trim() ?? string.Empty;
        var email = request.Email?.Trim() ?? string.Empty;
        if (nome.Length == 0) return BadRequest("Informe o nome do administrador.");
        if (nome.Length > 200) return BadRequest("O nome pode ter no máximo 200 caracteres.");
        if (!EmailValido(email)) return BadRequest("Informe um e-mail válido.");

        // Sem como entregar o convite (fora de desenvolvimento) não vale criar uma conta que ninguém consegue ativar.
        if (!emailSender.Configurado && !ambiente.IsDevelopment())
            return BadRequest("O envio de e-mail (SMTP) não está configurado neste ambiente: não há como entregar o convite.");
        if (string.IsNullOrWhiteSpace(config["App:UrlBase"]))
            return BadRequest("O endereço do site (App:UrlBase) não está configurado neste ambiente: o link do convite não teria destino.");

        if (await db.Usuarios.AnyAsync(u => u.Email.ToLower() == email.ToLower()))
            return BadRequest("Já existe uma conta com esse e-mail neste cliente.");

        var admin = new Usuario
        {
            Id = Guid.NewGuid(), Nome = nome, Email = email,
            SenhaHash = SenhaHasher.ConvitePendente, Papel = PapelUsuario.Admin, Ativo = true
        };
        db.Usuarios.Add(admin);
        auditoria.Registrar(nameof(Usuario), admin.Id, AcaoAuditoria.Criado, this.UsuarioIdAtual(), $"Administrador criado por convite: {admin.Nome} ({admin.Email})");
        await db.SaveChangesAsync();

        return Ok(await EnviarAsync(admin, TipoLinkSenha.Convite));
    }

    /// <summary>Reenvia o convite (conta pendente) ou, se o Admin já tem senha, manda um link de nova senha — é como o Suporte ajuda quem
    /// perdeu o acesso, sem ver nem definir a senha.</summary>
    [HttpPost("admins/{id:guid}/reenviar")]
    public async Task<ActionResult<ResultadoEnvioAdminDto>> ReenviarAdmin(Guid id)
    {
        var admin = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == id && u.Papel == PapelUsuario.Admin);
        if (admin is null) return NotFound("Administrador não encontrado.");
        if (!admin.Ativo) return BadRequest("Esta conta está desativada.");

        var pendente = admin.SenhaHash == SenhaHasher.ConvitePendente;
        auditoria.Registrar(nameof(Usuario), admin.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(),
            pendente ? $"Convite reenviado: {admin.Nome}" : $"Link de nova senha enviado: {admin.Nome}");
        return Ok(await EnviarAsync(admin, pendente ? TipoLinkSenha.Convite : TipoLinkSenha.Redefinicao));
    }

    /// <summary>Cancela um convite ainda não aceito: a conta é desativada e o link deixa de valer. (Conta que já tem senha não se mexe aqui.)</summary>
    [HttpPost("admins/{id:guid}/cancelar-convite")]
    public async Task<ActionResult<AdminEscolaDto>> CancelarConvite(Guid id)
    {
        var admin = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == id && u.Papel == PapelUsuario.Admin);
        if (admin is null) return NotFound("Administrador não encontrado.");
        if (admin.SenhaHash != SenhaHasher.ConvitePendente) return BadRequest("Só dá para cancelar um convite que ainda não foi aceito.");

        admin.Ativo = false;
        admin.EncerrarSessoes();
        var abertos = await db.RedefinicoesSenha.Where(r => r.UsuarioId == admin.Id && r.UsadoEm == null).ToListAsync();
        foreach (var aberto in abertos) aberto.UsadoEm = DateTime.UtcNow;

        auditoria.Registrar(nameof(Usuario), admin.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(), $"Convite cancelado: {admin.Nome}");
        await db.SaveChangesAsync();
        return Ok(ParaDto(admin, null));
    }

    private async Task<ResultadoEnvioAdminDto> EnviarAsync(Usuario admin, TipoLinkSenha tipo)
    {
        var cliente = await db.Clientes.Where(c => c.Id == clienteAtual.Id).Select(c => c.Nome).FirstOrDefaultAsync();
        var resultado = await linkSenha.EnviarAsync(admin, tipo, cliente);
        return new ResultadoEnvioAdminDto(ParaDto(admin, DateTime.UtcNow), resultado.Entregue, resultado.Aviso);
    }

    private static AdminEscolaDto ParaDto(Usuario u, DateTime? ultimoEnvio) =>
        new(u.Id, u.Nome, u.Email, u.Ativo, u.SenhaHash == SenhaHasher.ConvitePendente, ultimoEnvio);

    private static bool EmailValido(string email)
    {
        if (email.Length is 0 or > 256) return false;
        return System.Net.Mail.MailAddress.TryCreate(email, out var endereco) && endereco.Address == email && email.Contains('.');
    }

    [HttpGet("diagnostico")]
    public ActionResult<DiagnosticoPlataformaDto> Diagnostico()
    {
        var pix = pixBb.Value;
        return Ok(new DiagnosticoPlataformaDto(
            ambiente.EnvironmentName,
            emailSender.Configurado,
            !string.IsNullOrWhiteSpace(config["App:UrlBase"]),
            pix.Configurado,
            pix.Producao ? "Produção" : "Homologação",
            !string.IsNullOrWhiteSpace(pix.CertificadoPfxCaminho),
            pix.WebhookHabilitado));
    }

    private static ClientePlataformaDto ParaDto(Cliente c) => new(c.Id, c.Nome, c.Segmento, c.Ativo, c.CriadoEm);
}
