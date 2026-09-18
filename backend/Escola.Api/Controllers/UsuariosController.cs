using Escola.Api.Dtos;
using Escola.Domain.Enums;
using Escola.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Escola.Api.Controllers;

// Sem login ainda: usado pelo front para deixar o educador se identificar
// numa lista, até a autenticação de verdade existir.
[ApiController]
[Route("api/usuarios")]
public class UsuariosController(EscolaDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<UsuarioDto>>> Listar([FromQuery] string? papel)
    {
        var query = db.Usuarios.AsQueryable();

        if (papel is not null && Enum.TryParse<PapelUsuario>(papel, ignoreCase: true, out var papelEnum))
            query = query.Where(u => u.Papel == papelEnum);

        var usuarios = await query
            .Select(u => new UsuarioDto(u.Id, u.Nome, u.Papel.ToString()))
            .ToListAsync();

        return Ok(usuarios);
    }
}
