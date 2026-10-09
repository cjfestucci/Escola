using System.Net;
using Escola.Domain.Enums;
using Escola.Infrastructure;
using Escola.Infrastructure.Auth;
using Escola.Infrastructure.Clientes;
using Escola.Infrastructure.Data;
using Escola.Infrastructure.Email;
using Microsoft.EntityFrameworkCore;

namespace Escola.Api.Servicos;

/// <param name="Tipo"><c>Convite</c> (conta nova: confirma o e-mail e cria a senha) ou <c>Aviso</c> (já tem conta: só avisa pra entrar no
/// portal e aceitar o termo).</param>
/// <param name="Link">Só preenchido quando o convite <b>não</b> foi entregue por e-mail, pra quem fez a matrícula poder repassá-lo.</param>
public sealed record ConviteMatriculaDto(Guid ResponsavelId, string Nome, string Email, string Tipo, bool Entregue, string? Link, string? Aviso);

public interface IConviteMatriculaService
{
    /// <summary>Manda o e-mail da matrícula de <paramref name="alunoId"/> pros responsáveis indicados (ou todos, se nulo). Nunca lança por
    /// falha de entrega. Deve ser chamado depois do <c>SaveChanges</c> que criou as contas.</summary>
    Task<List<ConviteMatriculaDto>> EnviarAsync(Guid alunoId, IReadOnlyCollection<Guid>? responsavelIds = null);
}

public sealed class ConviteMatriculaService(
    EscolaDbContext db,
    IClienteAtual clienteAtual,
    ILinkSenhaService linkSenha,
    IEmailSender emailSender,
    IConfiguration config,
    ILogger<ConviteMatriculaService> logger) : IConviteMatriculaService
{
    public async Task<List<ConviteMatriculaDto>> EnviarAsync(Guid alunoId, IReadOnlyCollection<Guid>? responsavelIds = null)
    {
        var resultado = new List<ConviteMatriculaDto>();
        var aluno = await db.Alunos.Where(a => a.Id == alunoId).Select(a => new { a.Nome }).FirstOrDefaultAsync();
        if (aluno is null) return resultado;

        var nomeCliente = await db.Clientes.Where(c => c.Id == clienteAtual.Id).Select(c => c.Nome).FirstOrDefaultAsync();
        var responsaveis = await db.AlunoResponsaveis
            .Where(ar => ar.AlunoId == alunoId && (responsavelIds == null || responsavelIds.Contains(ar.ResponsavelId)))
            .Select(ar => ar.Responsavel)
            .ToListAsync();

        foreach (var responsavel in responsaveis)
        {
            var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.ResponsavelId == responsavel.Id && u.Papel == PapelUsuario.Responsavel);
            if (usuario is null)
            {
                resultado.Add(new(responsavel.Id, responsavel.Nome, responsavel.Email, "Aviso", false, null,
                    "Este e-mail já é usado por uma conta da equipe, então não há login no portal para aceitar o termo."));
                continue;
            }

            if (!usuario.Ativo && usuario.SenhaHash != SenhaHasher.ConvitePendente)
            {
                resultado.Add(new(responsavel.Id, responsavel.Nome, responsavel.Email, "Aviso", false, null, "A conta deste responsável está desativada."));
                continue;
            }

            if (usuario.SenhaHash == SenhaHasher.ConvitePendente)
            {
                var link = await linkSenha.EnviarAsync(usuario, TipoLinkSenha.ConviteMatricula, nomeCliente, [aluno.Nome]);
                resultado.Add(new(responsavel.Id, responsavel.Nome, responsavel.Email, "Convite", link.Entregue, link.Entregue ? null : link.Link, link.Aviso));
                continue;
            }

            var (entregue, aviso) = await AvisarContaExistenteAsync(usuario.Email, usuario.Nome, nomeCliente, aluno.Nome);
            resultado.Add(new(responsavel.Id, responsavel.Nome, responsavel.Email, "Aviso", entregue, null, aviso));
        }

        return resultado;
    }

    /// <summary>Quem já tem conta não recebe link com token: só o endereço do portal (o termo aparece depois do login).</summary>
    private async Task<(bool Entregue, string? Aviso)> AvisarContaExistenteAsync(string email, string nome, string? nomeCliente, string nomeAluno)
    {
        var urlBase = config["App:UrlBase"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(urlBase))
            return (false, "O endereço do site (App:UrlBase) não está configurado neste ambiente.");
        if (!emailSender.Configurado)
            return (false, "O envio de e-mail não está configurado neste ambiente (SMTP): peça ao responsável que entre no portal e aceite o termo.");

        var linkSeguro = WebUtility.HtmlEncode($"{urlBase}/entrar");
        var corpo = $"""
            <p>Olá, {WebUtility.HtmlEncode(nome)}!</p>
            <p>A matrícula de <strong>{WebUtility.HtmlEncode(nomeAluno)}</strong> foi registrada por <strong>{WebUtility.HtmlEncode(nomeCliente ?? "a escola")}</strong>.</p>
            <p>Entre no portal com o seu e-mail e senha de sempre e aceite o termo de matrícula. <strong>A matrícula só é efetivada depois desse aceite.</strong></p>
            <p><a href="{linkSeguro}">Abrir o portal</a></p>
            <p>Se você não reconhece esta matrícula, fale com a escola.</p>
            """;
        try
        {
            await emailSender.EnviarAsync(email, nome, $"Confirme a matrícula — {nomeCliente ?? MarcaProduto.Nome}", corpo);
            return (true, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao enviar o aviso de matrícula.");
            return (false, "Não foi possível enviar o e-mail agora. Tente reenviar em instantes.");
        }
    }
}
