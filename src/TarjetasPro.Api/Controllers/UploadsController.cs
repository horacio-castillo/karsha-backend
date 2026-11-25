using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TarjetasPro.Api.Security;
using TarjetasPro.Domain.Security;
using Microsoft.EntityFrameworkCore;
using TarjetasPro.Infrastructure;
using TarjetasPro.Infrastructure.Storage;

namespace TarjetasPro.Api.Controllers;

[ApiController]
[Route("api/uploads")]
[Authorize(Policy = Permissions.CardsCreate)]
public class UploadsController(IWebHostEnvironment env, DriveLinkService drive, AppDbContext db) : ControllerBase
{
    private const long MaxBytes = 12 * 1024 * 1024; // se acepta pesada: ImageSniffer la reduce antes de guardarla

    [HttpPost]
    [RequestSizeLimit(MaxBytes + 64 * 1024)]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        var saved = await ImageSniffer.SaveAsync(file, Path.Combine(env.WebRootPath, "uploads"), MaxBytes);
        if (saved.Error is not null) return BadRequest(new { errors = new[] { saved.Error } });

        // Con Drive vinculado, la imagen va a la carpeta del cliente y se borra la copia local.
        // La ruta conserva la forma «/uploads/…»: el resto de la plataforma no nota la diferencia.
        if (await drive.IsLinkedAsync())
        {
            var uid = Guid.Parse(User.FindFirstValue("sub")!);
            var name = await db.Users.AsNoTracking().Where(u => u.Id == uid).Select(u => u.FullName).FirstOrDefaultAsync() ?? "Cliente";
            var local = Path.Combine(env.WebRootPath, "uploads", saved.FileName!);
            var ext = Path.GetExtension(saved.FileName!).ToLowerInvariant();
            var mime = ext == ".png" ? "image/png" : ext == ".webp" ? "image/webp" : "image/jpeg";
            var driveId = await drive.UploadAsync(uid, name, local, saved.FileName!, mime);
            if (driveId is not null)
            {
                System.IO.File.Delete(local);
                return Ok(new { url = $"/uploads/drive-{driveId}{ext}" });
            }
        }
        return Ok(new { url = $"/uploads/{saved.FileName}" });
    }
}
