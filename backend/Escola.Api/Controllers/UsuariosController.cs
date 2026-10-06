using Escola.Api.Auth;
using Escola.Api.Dtos;
using Escola.Api.Dtos.Requests;
using Escola.Domain.Entities;
using Escola.Domain.Enums;
using Escola.Infrastructure.Auditoria;
using Escola.Infrastructure.Auth;
using Escola.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Escola.Api.Controllers;

[ApiController]
[Route("api/usuarios")]
[Authorize]
public class UsuariosController(EscolaDbContext db, IAuditoriaService auditoria) : ControllerBase
{
    /// <summary>Lista por papel — usado pra montar o seletor de professor ao cadastrar turma. Só contas
    /// ativas entram aqui (uma conta desativada não pode ser escolhida pra uma turma nova, mas continua
    /// aparecendo como professor de qualquer turma onde já estava vinculada).</summary>
    [HttpGet]
    [Authorize(Roles = GruposDePapeis.Gestao)]
    public async Task<ActionResult<List<UsuarioDto>>> Listar([FromQuery] string? papel)
    {
        var query = db.Usuarios.Where(u => u.Ativo && u.Papel != PapelUsuario.Suporte);

        if (papel is not null && Enum.TryParse<PapelUsuario>(papel, ignoreCase: true, out var papelEnum))
            query = query.Where(u => u.Papel == papelEnum);

        var usuarios = await query
            .OrderBy(u => u.Nome)
            .Select(u => new UsuarioDto(u.Id, u.Nome, u.Papel.ToString()))
            .ToListAsync();

        return Ok(usuarios);
    }

    /// <summary>Contas da equipe (todos os papéis exceto Responsável) — tela de gestão de usuários.</summary>
    [HttpGet("contas")]
    [Authorize(Roles = GruposDePapeis.Gestao)]
    public async Task<ActionResult<List<UsuarioContaDto>>> ListarContas()
    {
        var contas = await db.Usuarios
            .Where(u => u.Papel != PapelUsuario.Responsavel && u.Papel != PapelUsuario.Suporte)
            .OrderBy(u => u.Nome)
            .Select(u => new UsuarioContaDto(u.Id, u.Nome, u.Email, u.Papel.ToString(), u.Ativo))
            .ToListAsync();

        return Ok(contas);
    }

    [HttpPost("contas")]
    [Authorize(Roles = GruposDePapeis.Gestao)]
    public async Task<ActionResult<SenhaGeradaDto>> CriarConta(CriarUsuarioRequest request)
    {
        if (request.Papel == PapelUsuario.Responsavel)
            return BadRequest("Contas de responsável são criadas pela Matrícula, não por aqui.");

        if (request.Papel == PapelUsuario.Suporte || !Enum.IsDefined(request.Papel))
            return BadRequest("Papel inválido.");

        if (string.IsNullOrWhiteSpace(request.Nome) || string.IsNullOrWhiteSpace(request.Email))
            return BadRequest("Nome e e-mail são obrigatórios.");

        var email = request.Email.Trim();
        if (await db.Usuarios.AnyAsync(u => u.Email.ToLower() == email.ToLower()))
            return BadRequest("Já existe uma conta com esse e-mail.");

        var senha = GeradorSenhaTemporaria.Gerar();
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(), Nome = request.Nome.Trim(), Email = email,
            SenhaHash = SenhaHasher.Hash(senha), Papel = request.Papel
        };
        db.Usuarios.Add(usuario);
        auditoria.Registrar(nameof(Usuario), usuario.Id, AcaoAuditoria.Criado, this.UsuarioIdAtual(), usuario.Nome);
        await db.SaveChangesAsync();

        return Ok(new SenhaGeradaDto(usuario.Nome, usuario.Email, senha));
    }

    [HttpPut("contas/{id:guid}")]
    [Authorize(Roles = GruposDePapeis.Gestao)]
    public async Task<ActionResult<UsuarioContaDto>> EditarConta(Guid id, EditarUsuarioRequest request)
    {
        if (request.Papel == PapelUsuario.Responsavel)
            return BadRequest("Contas de responsável são geridas pela Matrícula, não por aqui.");

        if (request.Papel == PapelUsuario.Suporte || !Enum.IsDefined(request.Papel))
            return BadRequest("Papel inválido.");

        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == id && u.Papel != PapelUsuario.Responsavel && u.Papel != PapelUsuario.Suporte);
        if (usuario is null) return NotFound("Conta não encontrada.");

        if (string.IsNullOrWhiteSpace(request.Nome) || string.IsNullOrWhiteSpace(request.Email))
            return BadRequest("Nome e e-mail são obrigatórios.");

        var email = request.Email.Trim();
        if (await db.Usuarios.AnyAsync(u => u.Id != id && u.Email.ToLower() == email.ToLower()))
            return BadRequest("Já existe uma conta com esse e-mail.");

        var nomeAntes = usuario.Nome;
        var emailAntes = usuario.Email;
        var papelAntes = usuario.Papel;

        usuario.Nome = request.Nome.Trim();
        usuario.Email = email;
        usuario.Papel = request.Papel;

        var detalhe = AuditoriaDetalhe.MontarAlteracoes(
            ("Nome", nomeAntes, usuario.Nome),
            ("E-mail", emailAntes, usuario.Email),
            ("Papel", papelAntes.ToString(), usuario.Papel.ToString()));

        auditoria.Registrar(nameof(Usuario), usuario.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(), detalhe);
        await db.SaveChangesAsync();

        return Ok(new UsuarioContaDto(usuario.Id, usuario.Nome, usuario.Email, usuario.Papel.ToString(), usuario.Ativo));
    }

    [HttpPost("contas/{id:guid}/redefinir-senha")]
    [Authorize(Roles = GruposDePapeis.Gestao)]
    public async Task<ActionResult<SenhaGeradaDto>> RedefinirSenha(Guid id)
    {
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == id && u.Papel != PapelUsuario.Responsavel && u.Papel != PapelUsuario.Suporte);
        if (usuario is null) return NotFound("Conta não encontrada.");

        var senha = GeradorSenhaTemporaria.Gerar();
        usuario.SenhaHash = SenhaHasher.Hash(senha);
        if (usuario.Id != this.UsuarioIdAtual()) usuario.EncerrarSessoes(); // a senha antiga não pode continuar com sessão aberta
        auditoria.Registrar(nameof(Usuario), usuario.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(), $"Senha redefinida: {usuario.Nome}");
        await db.SaveChangesAsync();

        return Ok(new SenhaGeradaDto(usuario.Nome, usuario.Email, senha));
    }

    [HttpPost("contas/{id:guid}/desativar")]
    [Authorize(Roles = GruposDePapeis.Gestao)]
    public async Task<ActionResult<UsuarioContaDto>> DesativarConta(Guid id)
    {
        var usuarioLogadoId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (id.ToString() == usuarioLogadoId)
            return BadRequest("Você não pode desativar a própria conta.");

        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == id && u.Papel != PapelUsuario.Responsavel && u.Papel != PapelUsuario.Suporte);
        if (usuario is null) return NotFound("Conta não encontrada.");

        usuario.Ativo = false;
        usuario.EncerrarSessoes(); // reativar depois não ressuscita as sessões antigas
        auditoria.Registrar(nameof(Usuario), usuario.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(), $"Desativado: {usuario.Nome}");
        await db.SaveChangesAsync();

        return Ok(new UsuarioContaDto(usuario.Id, usuario.Nome, usuario.Email, usuario.Papel.ToString(), usuario.Ativo));
    }

    /// <summary>Derruba todas as sessões abertas da conta (aparelho perdido, suspeita de acesso indevido) sem desativá-la nem trocar a senha:
    /// a pessoa só precisa entrar de novo.</summary>
    [HttpPost("contas/{id:guid}/encerrar-sessoes")]
    [Authorize(Roles = GruposDePapeis.Gestao)]
    public async Task<ActionResult<UsuarioContaDto>> EncerrarSessoes(Guid id)
    {
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == id && u.Papel != PapelUsuario.Responsavel && u.Papel != PapelUsuario.Suporte);
        if (usuario is null) return NotFound("Conta não encontrada.");

        usuario.EncerrarSessoes();
        auditoria.Registrar(nameof(Usuario), usuario.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(), $"Sessões encerradas: {usuario.Nome}");
        await db.SaveChangesAsync();

        return Ok(new UsuarioContaDto(usuario.Id, usuario.Nome, usuario.Email, usuario.Papel.ToString(), usuario.Ativo));
    }

    [HttpPost("contas/{id:guid}/ativar")]
    [Authorize(Roles = GruposDePapeis.Gestao)]
    public async Task<ActionResult<UsuarioContaDto>> AtivarConta(Guid id)
    {
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == id && u.Papel != PapelUsuario.Responsavel && u.Papel != PapelUsuario.Suporte);
        if (usuario is null) return NotFound("Conta não encontrada.");

        usuario.Ativo = true;
        auditoria.Registrar(nameof(Usuario), usuario.Id, AcaoAuditoria.Editado, this.UsuarioIdAtual(), $"Reativado: {usuario.Nome}");
        await db.SaveChangesAsync();

        return Ok(new UsuarioContaDto(usuario.Id, usuario.Nome, usuario.Email, usuario.Papel.ToString(), usuario.Ativo));
    }

    /// <summary>Um educador só vê as próprias turmas; Admin/Coordenador podem consultar qualquer um.</summary>
    [HttpGet("{id:guid}/turmas")]
    [Authorize(Roles = $"{GruposDePapeis.Equipe}")]
    public async Task<ActionResult<List<TurmaDto>>> ListarTurmas(Guid id)
    {
        var ehGestao = User.IsInRole("Admin") || User.IsInRole("Coordenador");
        var usuarioLogadoId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!ehGestao && id.ToString() != usuarioLogadoId)
            return Forbid();

        if (!await db.Usuarios.AnyAsync(u => u.Id == id))
            return NotFound("Usuário não encontrado.");

        var turmas = await db.Turmas
            .Include(t => t.Alunos)
            .Include(t => t.Educadores).ThenInclude(te => te.Usuario)
            .Where(t => t.Educadores.Any(te => te.UsuarioId == id))
            .OrderBy(t => t.Nome)
            .ToListAsync();

        return Ok(turmas.Select(t => t.ToDto()));
    }
}
