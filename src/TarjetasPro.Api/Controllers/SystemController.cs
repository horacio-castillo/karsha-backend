using System.Diagnostics;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TarjetasPro.Application.Common;
using TarjetasPro.Domain.Security;
using TarjetasPro.Infrastructure;
using TarjetasPro.Infrastructure.Auth;
using TarjetasPro.Infrastructure.Logging;
using TarjetasPro.Infrastructure.Payments;
using TarjetasPro.Infrastructure.Storage;

namespace TarjetasPro.Api.Controllers;

public record ClientErrorRequest(string? Message, string? Stack, string? Url);
public record ResolveRequest(bool Resolved);

/// <summary>Salud del sistema, bitácora de auditoría y errores capturados. Solo Super Admin.</summary>
[ApiController]
[Route("api/admin")]
[Authorize(Policy = Permissions.RolesManage)]
public class SystemController(AppDbContext db, DriveLinkService drive, IOptions<EmailOptions> email, IOptions<PaymentOptions> pay, IConfiguration config, IWebHostEnvironment env) : ControllerBase
{
    private record Check(string Key, string Name, string Status, string Detail);

    [HttpGet("health")]
    public async Task<IActionResult> Health(CancellationToken ct)
    {
        var checks = new List<Check>();

        try
        {
            var sw = Stopwatch.StartNew();
            await db.Database.ExecuteSqlRawAsync("SELECT 1", ct);
            sw.Stop();
            var pending = (await db.Database.GetPendingMigrationsAsync(ct)).Count();
            checks.Add(pending > 0
                ? new("db", "Base de datos", "warn", $"Responde en {sw.ElapsedMilliseconds} ms, pero hay {pending} migración(es) pendientes.")
                : new("db", "Base de datos", sw.ElapsedMilliseconds > 800 ? "warn" : "ok", $"Responde en {sw.ElapsedMilliseconds} ms y está al día."));
        }
        catch (Exception ex) { checks.Add(new("db", "Base de datos", "bad", "No responde: " + ex.Message)); }

        var e = email.Value;
        checks.Add(e.LogOnly || e.Host.Length == 0
            ? new("email", "Correo", env.IsDevelopment() ? "warn" : "bad", env.IsDevelopment() ? "Modo desarrollo: los correos solo se escriben en el log, no salen." : "Falta configurar el servidor SMTP (Email:Host): ningún correo está saliendo.")
            : new("email", "Correo", e.From.Length == 0 ? "warn" : "ok", e.From.Length == 0 ? "SMTP configurado pero falta el remitente (Email:From)." : $"Sale por {e.Host} como {e.From}."));

        var p = pay.Value;
        checks.Add(string.Equals(p.Provider, "Simulated", StringComparison.OrdinalIgnoreCase)
            ? new("pay", "Pagos", env.IsDevelopment() ? "warn" : "bad", "Pagos simulados: no se cobra dinero real.")
            : p.Stripe.SecretKey.Length == 0
                ? new("pay", "Pagos", "bad", "Stripe sin llave secreta: no se pueden cobrar tarjetas.")
                : new("pay", "Pagos", p.Stripe.WebhookSecret.Length == 0 ? "warn" : "ok", p.Stripe.WebhookSecret.Length == 0 ? "Stripe conectado, pero falta el secreto del webhook: los pagos de OXXO no se confirmarán solos." : "Stripe conectado con webhook."));

        try
        {
            var d = await drive.StatusAsync();
            checks.Add(d.Linked ? new("drive", "Google Drive", "ok", $"Vinculado con {d.Email}.")
                : new("drive", "Google Drive", "warn", "Sin vincular: las fotos se guardan en el servidor."));
        }
        catch (Exception ex) { checks.Add(new("drive", "Google Drive", "bad", ex.Message)); }

        var last = CardExpiryService.LastRun;
        checks.Add(CardExpiryService.LastError is { } le
            ? new("job", "Avisos y vencimientos", "bad", "La última vuelta falló: " + le)
            : last is null ? new("job", "Avisos y vencimientos", "warn", "Todavía no corre desde que arrancó el servidor.")
            : DateTime.UtcNow - last > TimeSpan.FromHours(2) ? new("job", "Avisos y vencimientos", "warn", "Lleva más de 2 horas sin correr.")
            : new("job", "Avisos y vencimientos", "ok", $"Corrió hace {(int)(DateTime.UtcNow - last.Value).TotalMinutes} min (cada hora)."));

        if (!env.IsDevelopment() && string.IsNullOrWhiteSpace(config["DataProtection:KeysPath"]))
            checks.Add(new("keys", "Llaves de cifrado", "bad", "Falta DataProtection:KeysPath: al redesplegar habría que volver a vincular Drive."));

        var since = DateTime.UtcNow.AddHours(-24);
        var open = await db.ErrorLogs.Where(x => !x.Resolved && x.LastAt >= since).SumAsync(x => (int?)x.Count, ct) ?? 0;
        checks.Add(new("errors", "Errores (24 h)", open == 0 ? "ok" : open >= 20 ? "bad" : "warn", open == 0 ? "Sin errores sin resolver en el último día." : $"{open} error(es) sin resolver en el último día. Revisa «Logs»."));

        var overall = checks.Any(c => c.Status == "bad") ? "bad" : checks.Any(c => c.Status == "warn") ? "warn" : "ok";
        var started = Process.GetCurrentProcess().StartTime.ToUniversalTime();
        return Ok(new
        {
            overall,
            checks,
            info = new
            {
                environment = env.EnvironmentName,
                runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3),
                startedAt = started,
                uptimeMinutes = (int)(DateTime.UtcNow - started).TotalMinutes,
                users = await db.Users.CountAsync(ct),
                cards = await db.Cards.CountAsync(ct),
                payments = await db.Payments.CountAsync(ct),
                audit = await db.AuditLogs.CountAsync(ct),
            },
        });
    }

    [HttpGet("audit")]
    public async Task<IActionResult> Audit(string? area, string? search, int? page, int? pageSize, string? sort = null, string? dir = null)
    {
        var (pg, size) = Paged<object>.Normalize(page, pageSize);
        var q = db.AuditLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(area)) q = q.Where(a => a.Area == area);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(a => a.ActorName.Contains(s) || a.Target.Contains(s) || a.Action.Contains(s) || a.Detail.Contains(s));
        }
        var total = await q.CountAsync();
        var desc = Sorting.IsDesc(dir);
        var ordered = sort switch
        {
            "date" => q.By(desc, a => a.At),
            "actor" => q.By(desc, a => a.ActorName),
            "action" => q.By(desc, a => a.Action),
            "target" => q.By(desc, a => a.Target),
            "ip" => q.ByEmptyLast(desc, a => a.Ip == "", a => a.Ip),
            _ => q.OrderByDescending(a => a.At),
        };
        var items = await ordered.ThenByDescending(a => a.Id).Skip((pg - 1) * size).Take(size)
            .Select(a => new { a.Id, a.At, a.ActorId, a.ActorName, a.Area, a.Action, a.Target, a.Detail, a.Ip }).ToListAsync();
        var areas = await db.AuditLogs.Select(a => a.Area).Distinct().OrderBy(a => a).ToListAsync();
        return Ok(new { items, total, areas });
    }

    [HttpGet("errors")]
    public async Task<IActionResult> Errors(string? source, bool? resolved, string? search, int? page, int? pageSize, string? sort = null, string? dir = null)
    {
        var (pg, size) = Paged<object>.Normalize(page, pageSize);
        var q = db.ErrorLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(source)) q = q.Where(x => x.Source == source);
        if (resolved is not null) q = q.Where(x => x.Resolved == resolved);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(x => x.Message.Contains(s) || x.Path.Contains(s));
        }
        var total = await q.CountAsync();
        var desc = Sorting.IsDesc(dir);
        var ordered = sort switch
        {
            "source" => q.By(desc, x => x.Source),
            "message" => q.By(desc, x => x.Message),
            "count" => q.By(desc, x => x.Count),
            "last" => q.By(desc, x => x.LastAt),
            _ => q.OrderByDescending(x => x.LastAt),
        };
        var items = await ordered.ThenByDescending(x => x.Id).Skip((pg - 1) * size).Take(size)
            .Select(x => new { x.Id, x.Source, x.Message, x.Stack, x.Path, x.Method, x.FirstAt, x.LastAt, x.Count, x.Resolved, x.UserAgent,
                User = x.LastUserId == null ? null : db.Users.Where(u => u.Id == x.LastUserId).Select(u => u.FullName).FirstOrDefault() }).ToListAsync();
        var open = await db.ErrorLogs.CountAsync(x => !x.Resolved);
        return Ok(new { items, total, open });
    }

    [HttpPut("errors/{id:long}/resolve")]
    public async Task<IActionResult> Resolve(long id, ResolveRequest r)
    {
        var n = await db.ErrorLogs.Where(x => x.Id == id).ExecuteUpdateAsync(s => s.SetProperty(x => x.Resolved, r.Resolved));
        return n == 0 ? NotFound() : NoContent();
    }

    [HttpDelete("errors/resolved")]
    public async Task<IActionResult> ClearResolved()
    {
        var n = await db.ErrorLogs.Where(x => x.Resolved).ExecuteDeleteAsync();
        return Ok(new { deleted = n });
    }
}

/// <summary>El navegador reporta aquí sus errores (pantallas que fallan, llamadas que devuelven 5xx).</summary>
[ApiController]
[Route("api/log")]
public class ClientLogController(SystemLogger log) : ControllerBase
{
    [HttpPost("client")]
    [AllowAnonymous]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("public")]
    public async Task<IActionResult> Client(ClientErrorRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.Message)) return NoContent();
        Guid? uid = Guid.TryParse(User.FindFirstValue("sub"), out var g) ? g : null;
        await log.ErrorAsync("web", r.Message, r.Stack, r.Url, null, uid, Request.Headers.UserAgent.ToString());
        return NoContent();
    }
}
