using Escola.Api.Dtos;
using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Escola.Infrastructure.Auditoria;
using Escola.Infrastructure.Clientes;
using Escola.Infrastructure.Data;
using Escola.Infrastructure.Storage;
using Escola.Infrastructure.Tempo;
using Microsoft.EntityFrameworkCore;

namespace Escola.Api.Servicos;

/// <summary>Identidade da escola — nome, logo e fuso horário. Usado pelo Admin da escola (Configurações → Geral) e pelo Suporte
/// (Plataforma): uma regra só pros dois. Tudo vai pro histórico da Configuração Geral, com o nome de quem mudou.</summary>
public sealed class IdentidadeEscolaService(EscolaDbContext db, IClienteAtual clienteAtual, IAuditoriaService auditoria, IFotoStorage fotoStorage)
{
    /// <summary>Logo é mostrada no menu e na escolha de escola: precisa ser leve. (GIF e SVG ficam de fora: animação não combina com
    /// logo, e SVG é XML que pode carregar script.)</summary>
    public const long TamanhoMaximoLogoBytes = 2 * 1024 * 1024;

    private static readonly HashSet<string> TiposLogo = new() { "image/png", "image/jpeg", "image/webp" };

    public async Task<(ConfiguracaoEscolaDto? Config, string? Erro)> DefinirDadosAsync(string? nomeEscola, string? fusoHorario, Guid usuarioId)
    {
        var nome = nomeEscola?.Trim();
        if (string.IsNullOrWhiteSpace(nome)) return (null, "Informe o nome da escola.");
        if (nome.Length > 200) return (null, "O nome da escola pode ter no máximo 200 caracteres.");
        var fuso = fusoHorario?.Trim();
        if (!RelogioEscola.FusoValido(fuso)) return (null, "Fuso horário inválido.");

        var cliente = await db.Clientes.FirstAsync(c => c.Id == clienteAtual.Id);
        var (config, criando) = await ObterOuCriarConfigAsync();

        var nomeAntes = cliente.Nome;
        var fusoAntes = config.FusoHorario;
        cliente.Nome = nome;
        config.FusoHorario = fuso!;
        config.AtualizadoEm = DateTime.UtcNow;

        var detalhe = AuditoriaDetalhe.MontarAlteracoes(("Nome da escola", nomeAntes, cliente.Nome), ("Fuso horário", fusoAntes, config.FusoHorario));
        if (criando || detalhe is not null)
            auditoria.Registrar(nameof(ConfiguracaoEscola), config.Id, criando ? AcaoAuditoria.Criado : AcaoAuditoria.Editado, usuarioId, detalhe);
        await db.SaveChangesAsync();
        return (ParaDto(config, cliente), null);
    }

    public async Task<(ConfiguracaoEscolaDto? Config, string? Erro)> DefinirFusoAsync(string? fusoHorario, Guid usuarioId)
    {
        var cliente = await db.Clientes.FirstAsync(c => c.Id == clienteAtual.Id);
        return await DefinirDadosAsync(cliente.Nome, fusoHorario, usuarioId);
    }

    /// <summary>Tipo validado pelo <b>conteúdo</b> (<see cref="DetectorImagem"/>), nunca pelo Content-Type/extensão enviados.</summary>
    public async Task<(string? Url, string? Erro)> SalvarLogoAsync(IFormFile? arquivo, Guid usuarioId, CancellationToken ct)
    {
        if (arquivo is null || arquivo.Length == 0) return (null, "Nenhum arquivo enviado.");
        if (arquivo.Length > TamanhoMaximoLogoBytes) return (null, "A logo pode ter no máximo 2MB.");

        await using var stream = arquivo.OpenReadStream();
        var tipo = await DetectorImagem.DetectarAsync(stream, ct);
        if (tipo is null || !TiposLogo.Contains(tipo.Value.ContentType))
            return (null, "Formato não suportado. Use uma imagem PNG, JPEG ou WEBP.");

        var url = await fotoStorage.SalvarAsync(stream, $"logo{tipo.Value.Extensao}", tipo.Value.ContentType, ct);
        await DefinirLogoAsync(url, "Logo da empresa alterada", usuarioId);
        return (url, null);
    }

    public Task RemoverLogoAsync(Guid usuarioId) => DefinirLogoAsync(null, "Logo da empresa removida", usuarioId);

    private async Task DefinirLogoAsync(string? url, string detalhe, Guid usuarioId)
    {
        var (config, criando) = await ObterOuCriarConfigAsync();
        config.LogoUrl = url;
        config.AtualizadoEm = DateTime.UtcNow;
        auditoria.Registrar(nameof(ConfiguracaoEscola), config.Id, criando ? AcaoAuditoria.Criado : AcaoAuditoria.Editado, usuarioId, detalhe);
        await db.SaveChangesAsync();
    }

    private async Task<(ConfiguracaoEscola Config, bool Criando)> ObterOuCriarConfigAsync()
    {
        var config = await db.ConfiguracoesEscola.FirstOrDefaultAsync();
        if (config is not null) return (config, false);
        config = new ConfiguracaoEscola { Id = Guid.NewGuid(), FusoHorario = RelogioEscola.FusoPadrao };
        db.ConfiguracoesEscola.Add(config);
        return (config, true);
    }

    public static ConfiguracaoEscolaDto ParaDto(ConfiguracaoEscola? config, Cliente? cliente) => new(
        config?.Id,
        config?.FusoHorario ?? RelogioEscola.FusoPadrao,
        config?.CorPrincipal,
        cliente?.Segmento ?? SegmentoCliente.Escola,
        config?.LogoUrl,
        cliente?.Nome);
}
