using Microsoft.AspNetCore.DataProtection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TarjetasPro.Domain.Security;
using TarjetasPro.Infrastructure.Storage;

namespace TarjetasPro.Api.Controllers;

public record DriveConfigRequest(string ClientId, string? ClientSecret);

/// <summary>Vincula la cuenta de Google Drive de la plataforma. El Admin guarda sus credenciales de Google y autoriza una sola vez.</summary>
[ApiController]
[Route("api/admin/drive")]
public class DriveController(DriveLinkService drive, DriveStorageService storage, IConfiguration config) : ControllerBase
{
    /// <summary>Dirección que se registra en Google Cloud como «URI de redireccionamiento autorizado».</summary>
    private string RedirectUri => (config["App:ApiPublicUrl"] ?? $"{Request.Scheme}://{Request.Host}").TrimEnd('/') + "/api/admin/drive/callback";
    private string PublicUrl => (config["App:PublicUrl"] ?? "http://localhost:4200").TrimEnd('/');

    [HttpGet]
    [Authorize(Policy = Permissions.CardsManageAll)]
    public async Task<IActionResult> Status(CancellationToken ct)
    {
        var s = await drive.StatusAsync();
        return Ok(new
        {
            s.HasClient, s.HasSecret, s.Linked, s.Email, s.LinkedAt, s.ClientId,
            redirectUri = RedirectUri,
            rule = "KARSHA / Clientes / «Nombre (id corto)»",
            storage = s.Linked ? await storage.GetAsync(ct) : null,
        });
    }

    [HttpPut("config")]
    [Authorize(Policy = Permissions.CardsManageAll)]
    public async Task<IActionResult> Config(DriveConfigRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.ClientId)) return BadRequest(new { errors = new[] { "Escribe el ID de cliente de Google." } });
        var current = await drive.StatusAsync();
        if (!current.HasSecret && string.IsNullOrWhiteSpace(r.ClientSecret)) return BadRequest(new { errors = new[] { "Escribe el secreto del cliente." } });
        await drive.SaveClientAsync(r.ClientId, r.ClientSecret);
        return NoContent();
    }

    /// <summary>Da la dirección de Google a la que mandar al Admin para autorizar.</summary>
    [HttpPost("connect")]
    [Authorize(Policy = Permissions.CardsManageAll)]
    public async Task<IActionResult> Connect()
    {
        var state = drive.StateProtector.Protect(User.FindFirstValue("sub") ?? "admin", TimeSpan.FromMinutes(10));
        var url = await drive.BuildAuthUrlAsync(RedirectUri, state);
        return url is null ? BadRequest(new { errors = new[] { "Primero guarda el ID de cliente y el secreto." } }) : Ok(new { url });
    }

    /// <summary>Google regresa aquí. No lleva sesión del Admin: la autoriza el «state» firmado y con vigencia de 10 minutos.</summary>
    [HttpGet("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback(string? code, string? state, string? error, CancellationToken ct)
    {
        string Back(string kind, string? msg = null) =>
            $"{PublicUrl}/admin/almacenamiento?drive={kind}" + (msg is null ? "" : "&msg=" + Uri.EscapeDataString(msg));
        if (!string.IsNullOrEmpty(error)) return Redirect(Back("error", error == "access_denied" ? "Cancelaste la autorización en Google." : error));
        try { drive.StateProtector.Unprotect(state ?? ""); }
        catch { return Redirect(Back("error", "La autorización venció o no es válida. Vuelve a intentarlo.")); }
        if (string.IsNullOrEmpty(code)) return Redirect(Back("error", "Google no envió el código de autorización."));
        try { await drive.CompleteAsync(code, RedirectUri, ct); return Redirect(Back("ok")); }
        catch (Exception ex) { return Redirect(Back("error", ex.Message)); }
    }

    [HttpDelete]
    [Authorize(Policy = Permissions.CardsManageAll)]
    public async Task<IActionResult> Unlink() { await drive.UnlinkAsync(); return NoContent(); }
}
