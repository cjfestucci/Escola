using Escola.Api.Auth;
using Escola.Api.Dtos;
using Escola.Infrastructure.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Escola.Api.Controllers;

[ApiController]
[Route("api/uploads")]
[Authorize(Roles = GruposDePapeis.Equipe)]
public class UploadsController(IFotoStorage fotoStorage) : ControllerBase
{
    private static readonly HashSet<string> TiposPermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/gif"
    };

    private const long TamanhoMaximoBytes = 8 * 1024 * 1024;

    [HttpPost]
    [RequestSizeLimit(TamanhoMaximoBytes)]
    public async Task<ActionResult<UploadResultDto>> Enviar(IFormFile arquivo, CancellationToken ct)
    {
        if (arquivo is null || arquivo.Length == 0)
            return BadRequest("Nenhum arquivo enviado.");

        if (!TiposPermitidos.Contains(arquivo.ContentType))
            return BadRequest("Formato de imagem não suportado. Use JPEG, PNG, WEBP ou GIF.");

        if (arquivo.Length > TamanhoMaximoBytes)
            return BadRequest("Arquivo maior que o limite de 8MB.");

        await using var stream = arquivo.OpenReadStream();
        var url = await fotoStorage.SalvarAsync(stream, arquivo.FileName, arquivo.ContentType, ct);

        return Ok(new UploadResultDto(url));
    }
}
