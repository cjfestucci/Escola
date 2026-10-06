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
    private const long TamanhoMaximoBytes = 8 * 1024 * 1024;

    [HttpPost]
    [RequestSizeLimit(TamanhoMaximoBytes)]
    public async Task<ActionResult<UploadResultDto>> Enviar(IFormFile arquivo, CancellationToken ct)
    {
        if (arquivo is null || arquivo.Length == 0)
            return BadRequest("Nenhum arquivo enviado.");

        if (arquivo.Length > TamanhoMaximoBytes)
            return BadRequest("Arquivo maior que o limite de 8MB.");

        await using var stream = arquivo.OpenReadStream();

        // O tipo vem do CONTEÚDO, não do Content-Type nem da extensão do nome (os dois são livres pra quem envia): senão um .html
        // declarado como imagem seria servido como página do site. A extensão gravada é sempre a do tipo detectado.
        var tipo = await DetectorImagem.DetectarAsync(stream, ct);
        if (tipo is null)
            return BadRequest("Formato de imagem não suportado. Use JPEG, PNG, WEBP ou GIF.");

        var url = await fotoStorage.SalvarAsync(stream, $"imagem{tipo.Value.Extensao}", tipo.Value.ContentType, ct);
        return Ok(new UploadResultDto(url));
    }
}
