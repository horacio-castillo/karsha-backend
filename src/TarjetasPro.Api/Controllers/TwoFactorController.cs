using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using QRCoder;
using TarjetasPro.Domain.Entities;
using TarjetasPro.Domain.Security;
using TarjetasPro.Infrastructure;
using TarjetasPro.Infrastructure.Auth;
using TarjetasPro.Infrastructure.Identity;

namespace TarjetasPro.Api.Controllers;

public record TwoFactorCodeRequest(string? Code);

/// <summary>
/// Verificación en dos pasos de la propia cuenta, con una app de autenticación (Google Authenticator, Microsoft Authenticator, Authy…).
/// Al encenderla se dan 10 códigos de respaldo de un solo uso para cuando no se tenga el celular.
/// </summary>
[ApiController]
[Route("api/me/2fa")]
[Authorize]
public class TwoFactorController(UserManager<ApplicationUser> users, AppDbContext db, TarjetasPro.Application.Auth.IEmailSender email, IConfiguration config, ILogger<TwoFactorController> logger) : ControllerBase
{
    private string PublicUrl => (config["App:PublicUrl"] ?? "http://localhost:4200").TrimEnd('/');
    private IActionResult Wrong(string message) => BadRequest(new { errors = new[] { message } });

    private async Task<ApplicationUser?> MeAsync() =>
        User.HasClaim(c => c.Type == ViewAs.AdminClaim) ? null : await users.FindByIdAsync(User.FindFirstValue("sub") ?? "");

    [HttpGet]
    public async Task<IActionResult> Status()
    {
        var me = await MeAsync();
        if (me is null) return NotFound();
        var privileged = (await users.GetRolesAsync(me)).Any(Roles.IsPrivileged);
        return Ok(new { enabled = me.TwoFactorEnabled, recoveryLeft = me.TwoFactorEnabled ? await users.CountRecoveryCodesAsync(me) : 0, recommended = privileged });
    }

    /// <summary>Paso 1: genera una llave nueva y su QR. Aún no se enciende: falta comprobar un código.</summary>
    [HttpPost("setup")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Setup()
    {
        var me = await MeAsync();
        if (me is null) return NotFound();
        if (me.TwoFactorEnabled) return Wrong("Ya tienes la verificación en dos pasos encendida.");
        await users.ResetAuthenticatorKeyAsync(me);
        var key = await users.GetAuthenticatorKeyAsync(me) ?? "";
        var uri = TwoFactorCodes.Uri(me.Email!, key);
        using var gen = new QRCodeGenerator();
        using var qr = gen.CreateQrCode(uri, QRCodeGenerator.ECCLevel.M);
        var svg = new SvgQRCode(qr).GetGraphic(6, "#0b0f14", "#ffffff", true, SvgQRCode.SizingMode.ViewBoxAttribute);
        return Ok(new { key = TwoFactorCodes.Pretty(key), uri, qr = "data:image/svg+xml;base64," + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(svg)) });
    }

    /// <summary>Paso 2: con el primer código correcto se enciende y se entregan los códigos de respaldo (solo esta vez).</summary>
    [HttpPost("enable")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Enable(TwoFactorCodeRequest r)
    {
        var me = await MeAsync();
        if (me is null) return NotFound();
        if (me.TwoFactorEnabled) return Wrong("Ya tienes la verificación en dos pasos encendida.");
        var code = new string((r.Code ?? "").Where(char.IsAsciiDigit).ToArray());
        if (code.Length != 6 || !await users.VerifyTwoFactorTokenAsync(me, users.Options.Tokens.AuthenticatorTokenProvider, code))
            return Wrong("El código no es correcto. Escribe el de 6 dígitos que muestra tu app ahora mismo.");
        await users.SetTwoFactorEnabledAsync(me, true);
        var codes = (await users.GenerateNewTwoFactorRecoveryCodesAsync(me, TwoFactorCodes.RecoveryCount))?.ToArray() ?? [];
        await LogAsync(me, "Encendió la verificación en dos pasos");
        await NotifyAsync(me, "verificación en dos pasos (encendida)");
        return Ok(new { codes, message = "Listo: desde ahora te pediremos un código al entrar." });
    }

    /// <summary>Apagarla pide un código válido, para que alguien con la sesión abierta no pueda quitarla.</summary>
    [HttpPost("disable")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Disable(TwoFactorCodeRequest r)
    {
        var me = await MeAsync();
        if (me is null) return NotFound();
        if (!me.TwoFactorEnabled) return Wrong("La verificación en dos pasos ya está apagada.");
        if (!(await TwoFactorCodes.CheckAsync(users, me, r.Code)).Ok) return Wrong("El código no es correcto.");
        await users.SetTwoFactorEnabledAsync(me, false);
        await users.ResetAuthenticatorKeyAsync(me);
        await LogAsync(me, "Apagó la verificación en dos pasos");
        await NotifyAsync(me, "verificación en dos pasos (apagada)");
        return Ok(new { message = "Apagaste la verificación en dos pasos." });
    }

    /// <summary>Nuevos códigos de respaldo; los anteriores dejan de servir.</summary>
    [HttpPost("recovery-codes")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> NewCodes(TwoFactorCodeRequest r)
    {
        var me = await MeAsync();
        if (me is null) return NotFound();
        if (!me.TwoFactorEnabled) return Wrong("Primero enciende la verificación en dos pasos.");
        if (!(await TwoFactorCodes.CheckAsync(users, me, r.Code)).Ok) return Wrong("El código no es correcto.");
        var codes = (await users.GenerateNewTwoFactorRecoveryCodesAsync(me, TwoFactorCodes.RecoveryCount))?.ToArray() ?? [];
        await LogAsync(me, "Generó nuevos códigos de respaldo");
        return Ok(new { codes, message = "Códigos nuevos listos. Los anteriores ya no sirven." });
    }

    private async Task LogAsync(ApplicationUser me, string action)
    {
        db.AuditLogs.Add(new AuditLog
        {
            ActorId = me.Id, ActorName = me.FullName, Area = "seguridad", Action = action, Target = Cut(me.Email ?? "", 200), Detail = "",
            Ip = Cut(HttpContext.Connection.RemoteIpAddress?.ToString() ?? "", 45),
        });
        await db.SaveChangesAsync();
    }

    private async Task NotifyAsync(ApplicationUser me, string what)
    {
        try { await email.SendAsync(me.Email!, EmailCatalog.SecuritySubject, EmailCatalog.SecurityNotice(me.FullName, what, DateTime.UtcNow, $"{PublicUrl}/olvide-contrasena")); }
        catch (Exception ex) { logger.LogError(ex, "No se pudo enviar el aviso de 2FA a {Email}", me.Email); }
    }

    private static string Cut(string s, int max) => s.Length <= max ? s : s[..max];
}
