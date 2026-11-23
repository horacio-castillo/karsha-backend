using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TarjetasPro.Api.Security;
using TarjetasPro.Application.Auth;
using TarjetasPro.Infrastructure.Identity;

namespace TarjetasPro.Api.Controllers;

/// <summary>Perfil de la persona con sesión: nombre y foto. Lo puede usar cualquier usuario.</summary>
[ApiController]
[Route("api/me")]
[Authorize]
public class MeController(UserManager<ApplicationUser> users, IAuthService auth, IWebHostEnvironment env) : ControllerBase
{
    private const long MaxBytes = 2 * 1024 * 1024;

    public record UpdateProfileRequest(string FullName);

    [HttpPut]
    public async Task<IActionResult> Update(UpdateProfileRequest request)
    {
        var name = (request.FullName ?? "").Trim();
        if (name.Length is < 2 or > 100) return BadRequest(new { errors = new[] { "Escribe tu nombre (de 2 a 100 letras)." } });
        var user = await Current();
        if (user is null) return Unauthorized();
        user.FullName = name;
        await users.UpdateAsync(user);
        return Ok(await auth.GetUserAsync(user.Id));
    }

    [HttpPost("avatar")]
    [RequestSizeLimit(MaxBytes + 64 * 1024)]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        var user = await Current();
        if (user is null) return Unauthorized();
        var saved = await ImageSniffer.SaveAsync(file, Path.Combine(env.WebRootPath, "uploads"), MaxBytes);
        if (saved.Error is not null) return BadRequest(new { errors = new[] { saved.Error } });
        var old = user.AvatarUrl;
        user.AvatarUrl = $"/uploads/{saved.FileName}";
        await users.UpdateAsync(user);
        DeleteLocal(old);
        return Ok(await auth.GetUserAsync(user.Id));
    }

    [HttpDelete("avatar")]
    public async Task<IActionResult> RemoveAvatar()
    {
        var user = await Current();
        if (user is null) return Unauthorized();
        var old = user.AvatarUrl;
        user.AvatarUrl = null;
        await users.UpdateAsync(user);
        DeleteLocal(old);
        return Ok(await auth.GetUserAsync(user.Id));
    }

    private async Task<ApplicationUser?> Current()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(id, out var g) ? await users.FindByIdAsync(g.ToString()) : null;
    }

    /// <summary>Borra la foto anterior del disco (solo archivos propios de /uploads).</summary>
    private void DeleteLocal(string? url)
    {
        if (url is null || !url.StartsWith("/uploads/", StringComparison.Ordinal) || url.Contains("..")) return;
        try { System.IO.File.Delete(Path.Combine(env.WebRootPath, "uploads", Path.GetFileName(url))); } catch { /* no crítico */ }
    }
}
