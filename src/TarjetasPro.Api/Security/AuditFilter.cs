using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using TarjetasPro.Api.Controllers;
using Microsoft.EntityFrameworkCore;
using TarjetasPro.Infrastructure;
using TarjetasPro.Infrastructure.Logging;

namespace TarjetasPro.Api.Security;

/// <summary>
/// Anota en la bitácora cada cambio hecho desde el panel (POST, PUT, DELETE que salieron bien).
/// No guarda el contenido de lo enviado (podría traer secretos): solo qué se hizo, a quién y quién lo hizo.
/// </summary>
public class AuditFilter(SystemLogger log, AppDbContext db) : IAsyncActionFilter
{
    private static readonly string[] Prefixes = ["api/admin/", "api/users", "api/categories", "api/templates", "api/cards/"];

    // (método, ruta con {id}) → (área, frase)
    private static readonly (string Method, string Pattern, string Area, string Text)[] Map =
    [
        ("POST", "api/users", "usuarios", "Creó un usuario"),
        ("PUT", "api/users/{id}", "usuarios", "Editó los datos de un usuario"),
        ("PUT", "api/users/{id}/active", "usuarios", "Activó o desactivó una cuenta"),
        ("PUT", "api/users/{id}/role", "usuarios", "Cambió el rol de un usuario"),
        ("POST", "api/users/{id}/send-reset", "usuarios", "Envió enlace para restablecer contraseña"),
        ("POST", "api/users/{id}/resend-verification", "usuarios", "Reenvió la verificación de correo"),
        ("PUT", "api/admin/roles/{id}/permissions", "roles", "Cambió los permisos de un rol"),
        ("POST", "api/admin/roles", "roles", "Creó un rol"),
        ("DELETE", "api/admin/roles/{id}", "roles", "Eliminó un rol"),
        ("POST", "api/admin/sales", "ventas", "Hizo una venta asistida"),
        ("POST", "api/admin/cards/grant", "tarjetas", "Regaló o asignó tiempo a una tarjeta"),
        ("PUT", "api/admin/cards/{id}/grant", "tarjetas", "Cambió la vigencia de una tarjeta"),
        ("POST", "api/admin/cards/{id}/restore", "tarjetas", "Restauró una tarjeta eliminada"),
        ("POST", "api/admin/cards/{id}/charge", "pagos", "Generó un cobro"),
        ("PUT", "api/admin/payments/{id}/pay", "pagos", "Marcó un pago como cobrado"),
        ("PUT", "api/cards/{id}/status", "tarjetas", "Cambió el estado de una tarjeta"),
        ("PUT", "api/admin/promotions/{id}", "promociones", "Guardó una promoción"),
        ("DELETE", "api/admin/promotions/{id}", "promociones", "Quitó una promoción"),
        ("POST", "api/admin/coupons", "promociones", "Creó un cupón"),
        ("PUT", "api/admin/coupons/{id}", "promociones", "Editó un cupón"),
        ("DELETE", "api/admin/coupons/{id}", "promociones", "Eliminó un cupón"),
        ("POST", "api/admin/plans/products", "planes", "Creó un producto"),
        ("PUT", "api/admin/plans/products/{id}", "planes", "Editó un producto"),
        ("DELETE", "api/admin/plans/products/{id}", "planes", "Eliminó un producto"),
        ("POST", "api/admin/plans", "planes", "Creó un plan"),
        ("PUT", "api/admin/plans/{id}", "planes", "Editó un plan o su precio"),
        ("DELETE", "api/admin/plans/{id}", "planes", "Eliminó un plan"),
        ("PUT", "api/admin/drive/config", "sistema", "Guardó la configuración de Google Drive"),
        ("POST", "api/admin/drive/connect", "sistema", "Inició la vinculación con Google Drive"),
        ("DELETE", "api/admin/drive", "sistema", "Desvinculó Google Drive"),
        ("DELETE", "api/admin/errors/resolved", "sistema", "Limpió los errores resueltos"),
        ("POST", "api/categories", "plantillas", "Creó un giro"),
        ("PUT", "api/categories/{id}", "plantillas", "Editó un giro"),
        ("DELETE", "api/categories/{id}", "plantillas", "Eliminó un giro"),
        ("POST", "api/templates", "plantillas", "Creó una plantilla"),
        ("PUT", "api/templates/{id}", "plantillas", "Editó una plantilla"),
        ("DELETE", "api/templates/{id}", "plantillas", "Eliminó una plantilla"),
    ];

    private static bool Matches(string pattern, string path)
    {
        var a = pattern.Split('/'); var b = path.Split('/');
        if (a.Length != b.Length) return false;
        for (var i = 0; i < a.Length; i++) if (a[i] != "{id}" && !a[i].Equals(b[i], StringComparison.OrdinalIgnoreCase)) return false;
        return true;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext ctx, ActionExecutionDelegate next)
    {
        var http = ctx.HttpContext;
        var method = http.Request.Method;
        var path = http.Request.Path.Value!.Trim('/');
        var audited = method is "POST" or "PUT" or "DELETE" && http.User.Identity?.IsAuthenticated == true
            && Prefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
        var rule = audited ? Map.FirstOrDefault(m => m.Method == method && Matches(m.Pattern, path)) : default;
        if (rule.Text is null) { await next(); return; }

        var target = "";
        var segs = path.Split('/');
        var idSeg = segs.FirstOrDefault(s => Guid.TryParse(s, out _));
        if (idSeg is not null && Guid.TryParse(idSeg, out var id)) target = await NameOf(path, id);
        else if (rule.Area == "roles" && segs.Length > 3) target = Uri.UnescapeDataString(segs[3]);
        var detail = Describe(ctx.ActionArguments.Values);

        var executed = await next();
        var code = (executed.Result as IStatusCodeActionResult)?.StatusCode ?? (executed.Result is null ? 200 : 200);
        if (executed.Exception is not null || code >= 400) return;

        Guid? actor = Guid.TryParse(http.User.FindFirstValue("sub"), out var g) ? g : null;
        await log.AuditAsync(actor, http.User.FindFirstValue("name") ?? "—", rule.Area, rule.Text, target, detail, http.Connection.RemoteIpAddress?.ToString() ?? "");
    }

    private static string Describe(IEnumerable<object?> args)
    {
        foreach (var a in args)
            switch (a)
            {
                case RoleRequest r: return $"Nuevo rol: {r.Role}";
                case ActiveRequest r: return r.IsActive ? "Quedó activa" : "Quedó inactiva";
            }
        return "";
    }

    private async Task<string> NameOf(string path, Guid id)
    {
        try
        {
            if (path.StartsWith("api/users")) return await db.Users.Where(u => u.Id == id).Select(u => u.FullName).FirstOrDefaultAsync() ?? "";
            if (path.Contains("cards/")) return await db.Cards.IgnoreQueryFilters().Where(c => c.Id == id).Select(c => c.Title).FirstOrDefaultAsync() ?? "";
            if (path.Contains("coupons/")) return await db.Coupons.Where(c => c.Id == id).Select(c => c.Code).FirstOrDefaultAsync() ?? "";
            if (path.StartsWith("api/categories")) return await db.TemplateCategories.Where(c => c.Id == id).Select(c => c.Name).FirstOrDefaultAsync() ?? "";
            if (path.StartsWith("api/templates")) return await db.Templates.Where(t => t.Id == id).Select(t => t.Name).FirstOrDefaultAsync() ?? "";
            if (path.Contains("payments/")) return await db.Payments.Where(p => p.Id == id).Select(p => p.CardTitle).FirstOrDefaultAsync() ?? "";
        }
        catch { }
        return "";
    }
}
