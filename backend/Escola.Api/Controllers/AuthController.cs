using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Escola.Api.Dtos;
using Escola.Api.Dtos.Requests;
using Escola.Infrastructure.Auth;
using Escola.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Escola.Api.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController(EscolaDbContext db, IConfiguration config) : ControllerBase
{
    [HttpPost("entrar")]
    public async Task<ActionResult<LoginRespostaDto>> Entrar(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == email);

        if (usuario is null || !SenhaHasher.Verificar(request.Senha, usuario.SenhaHash))
            return Unauthorized("E-mail ou senha inválidos.");

        if (!usuario.Ativo)
            return Unauthorized("Esta conta foi desativada. Fale com a coordenação.");

        var token = GerarToken(usuario.Id, usuario.Nome, usuario.Papel.ToString(), usuario.ResponsavelId);
        return Ok(new LoginRespostaDto(token, usuario.Id, usuario.Nome, usuario.Papel.ToString(), usuario.ResponsavelId));
    }

    private string GerarToken(Guid usuarioId, string nome, string papel, Guid? responsavelId)
    {
        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Chave"]!));
        var credenciais = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuarioId.ToString()),
            new(ClaimTypes.Name, nome),
            new(ClaimTypes.Role, papel)
        };
        if (responsavelId is { } id)
            claims.Add(new Claim("responsavelId", id.ToString()));

        var diasValidade = config.GetValue<int?>("Jwt:DiasValidade") ?? 30;
        var token = new JwtSecurityToken(
            issuer: config["Jwt:Emissor"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(diasValidade),
            signingCredentials: credenciais);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
