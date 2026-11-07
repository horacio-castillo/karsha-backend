using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TarjetasPro.Api.Security;
using TarjetasPro.Domain.Security;

namespace TarjetasPro.Api.Controllers;

[ApiController]
[Route("api/uploads")]
[Authorize(Policy = Permissions.CardsCreate)]
public class UploadsController(IWebHostEnvironment env) : ControllerBase
{
    private const long MaxBytes = 2 * 1024 * 1024;

    [HttpPost]
    [RequestSizeLimit(MaxBytes + 64 * 1024)]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        var saved = await ImageSniffer.SaveAsync(file, Path.Combine(env.WebRootPath, "uploads"), MaxBytes);
        return saved.Error is not null
            ? BadRequest(new { errors = new[] { saved.Error } })
            : Ok(new { url = $"/uploads/{saved.FileName}" });
    }
}
