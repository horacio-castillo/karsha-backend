using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TarjetasPro.Domain.Entities;
using TarjetasPro.Domain.Security;
using TarjetasPro.Infrastructure;

namespace TarjetasPro.Api.Controllers;

/// <summary>
/// Reportes del negocio. Todos devuelven la misma forma (KPIs, una serie para graficar, desgloses y una tabla)
/// para que la pantalla y la descarga a CSV sean genéricas.
/// </summary>
[ApiController]
[Route("api/admin/reports")]
[Authorize(Policy = Permissions.CardsManageAll)]
public class ReportsController(AppDbContext db) : ControllerBase
{
    private record Kpi(string Label, object Value, string Type);
    private record Col(string Key, string Label, string Type);
    private record Point(string Label, long Value);
    private record Group(string Title, string Type, List<Point> Rows);

    private static readonly string[] Online = ["Stripe", "Simulated", "Coupon"];

    private static (DateTime From, DateTime To, bool Monthly) Range(string? from, string? to)
    {
        var t = DateOnly.TryParse(to, out var td) ? td : DateOnly.FromDateTime(DateTime.UtcNow);
        var f = DateOnly.TryParse(from, out var fd) ? fd : t.AddDays(-29);
        if (f > t) (f, t) = (t, f);
        if (t.DayNumber - f.DayNumber > 1100) f = t.AddDays(-1100);
        return (f.ToDateTime(TimeOnly.MinValue), t.AddDays(1).ToDateTime(TimeOnly.MinValue), t.DayNumber - f.DayNumber > 62);
    }

    private static List<Point> Series(IEnumerable<(DateTime At, long V)> items, DateTime from, DateTime to, bool monthly)
    {
        var by = items.GroupBy(i => monthly ? new DateTime(i.At.Year, i.At.Month, 1) : i.At.Date).ToDictionary(g => g.Key, g => g.Sum(x => x.V));
        var res = new List<Point>();
        for (var d = monthly ? new DateTime(from.Year, from.Month, 1) : from; d < to; d = monthly ? d.AddMonths(1) : d.AddDays(1))
            res.Add(new(monthly ? d.ToString("MMM yy") : d.ToString("d MMM"), by.GetValueOrDefault(d)));
        return res;
    }

    private static List<Point> Top(IEnumerable<(string Key, long V)> items) =>
        items.GroupBy(i => i.Key).Select(g => new Point(g.Key, g.Sum(x => x.V))).OrderByDescending(p => p.Value).ToList();

    private IActionResult Out(List<Kpi> kpis, List<Point> series, string seriesType, string seriesTitle, List<Group> groups, List<Col> cols, List<Dictionary<string, object?>> rows)
        => Ok(new { kpis, series, seriesType, seriesTitle, groups, columns = cols, rows });

    [HttpGet("{kind}")]
    public async Task<IActionResult> Get(string kind, string? from, string? to, CancellationToken ct)
    {
        var (f, t, monthly) = Range(from, to);
        return kind switch
        {
            "ventas" => await Sales(f, t, monthly, ct),
            "renovaciones" => await Renewals(f, t, monthly, ct),
            "promociones" => await Promos(f, t, monthly, ct),
            "clientes" => await Customers(f, t, monthly, ct),
            "uso" => await Usage(f, t, monthly, ct),
            _ => NotFound(),
        };
    }

    private async Task<IActionResult> Sales(DateTime f, DateTime t, bool monthly, CancellationToken ct)
    {
        var rows = await db.Payments.AsNoTracking().Where(p => p.Status == PaymentStatus.Paid && p.PaidAt >= f && p.PaidAt < t).OrderByDescending(p => p.PaidAt).ToListAsync(ct);
        var names = await db.Users.Where(u => rows.Select(r => r.UserId).Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName, ct);
        var total = rows.Sum(r => (long)r.AmountCents);
        var kpis = new List<Kpi> { new("Ingresos", total, "money"), new("Ventas", rows.Count, "number"), new("Ticket promedio", rows.Count == 0 ? 0 : total / rows.Count, "money"), new("Descuentos dados", rows.Sum(r => (long)r.DiscountCents), "money") };
        var groups = new List<Group>
        {
            new("Por plan", "money", Top(rows.Select(r => (r.Plan.ToString(), (long)r.AmountCents)))),
            new("Por método", "money", Top(rows.Select(r => (r.MethodType == "oxxo" ? "OXXO" : r.MethodType == "card" ? "Tarjeta" : Online.Contains(r.Provider) ? "En línea" : r.Provider, (long)r.AmountCents)))),
            new("Por canal", "money", Top(rows.Select(r => (Online.Contains(r.Provider) ? "Venta en línea" : "Venta asistida", (long)r.AmountCents)))),
        };
        var cols = new List<Col> { new("fecha", "Fecha", "date"), new("folio", "Folio", "text"), new("cliente", "Cliente", "text"), new("tarjeta", "Tarjeta", "text"), new("plan", "Plan", "text"), new("canal", "Canal", "text"), new("cupon", "Cupón", "text"), new("descuento", "Descuento", "money"), new("monto", "Monto", "money") };
        var table = rows.Select(r => new Dictionary<string, object?>
        {
            ["fecha"] = r.PaidAt, ["folio"] = Payment.Folio(r.ReceiptNumber, r.PaidAt) ?? "", ["cliente"] = names.GetValueOrDefault(r.UserId, ""), ["tarjeta"] = r.CardTitle,
            ["plan"] = r.Plan.ToString(), ["canal"] = Online.Contains(r.Provider) ? "En línea" : "Asistida", ["cupon"] = r.CouponCode ?? "", ["descuento"] = r.DiscountCents, ["monto"] = r.AmountCents,
        }).ToList();
        return Out(kpis, Series(rows.Select(r => (r.PaidAt!.Value, (long)r.AmountCents)), f, t, monthly), "money", "Ingresos", groups, cols, table);
    }

    private async Task<IActionResult> Renewals(DateTime f, DateTime t, bool monthly, CancellationToken ct)
    {
        var paid = await db.Payments.AsNoTracking().Where(p => p.Status == PaymentStatus.Paid && p.PaidAt != null).Select(p => new { p.CardId, p.PaidAt }).ToListAsync(ct);
        var firstPaid = paid.GroupBy(p => p.CardId).ToDictionary(g => g.Key, g => g.Min(x => x.PaidAt!.Value));
        var renewals = paid.Where(p => p.PaidAt >= f && p.PaidAt < t && firstPaid[p.CardId] < p.PaidAt).ToList();
        var due = await db.Cards.AsNoTracking().Where(c => c.ExpiresAt != null && c.ExpiresAt >= f && c.ExpiresAt < t && c.Status != CardStatus.Draft).OrderBy(c => c.ExpiresAt).ToListAsync(ct);
        var lost = due.Count(c => c.Status == CardStatus.Expired);
        var rate = renewals.Count + lost == 0 ? 0 : (long)Math.Round(100.0 * renewals.Count / (renewals.Count + lost));
        var soon = await db.Cards.CountAsync(c => c.Status == CardStatus.Published && c.ExpiresAt != null && c.ExpiresAt >= DateTime.UtcNow && c.ExpiresAt < DateTime.UtcNow.AddDays(30), ct);
        var users = await db.Users.Where(u => due.Select(c => c.UserId).Contains(u.Id)).ToDictionaryAsync(u => u.Id, ct);
        var kpis = new List<Kpi> { new("Vencen en el periodo", due.Count, "number"), new("Renovaciones", renewals.Count, "number"), new("Vencidas sin renovar", lost, "number"), new("Tasa de renovación", rate, "percent"), new("Por vencer en 30 días", soon, "number") };
        var groups = new List<Group> { new("Por plan", "number", Top(due.Select(c => (c.Plan.ToString(), 1L)))), new("Por estado", "number", Top(due.Select(c => (c.Status == CardStatus.Expired ? "Vencida" : c.Status == CardStatus.Published ? "Vigente" : "Suspendida", 1L)))) };
        var cols = new List<Col> { new("vence", "Vence", "date"), new("estado", "Estado", "text"), new("tarjeta", "Tarjeta", "text"), new("plan", "Plan", "text"), new("cliente", "Cliente", "text"), new("correo", "Correo", "text"), new("telefono", "WhatsApp", "text") };
        var table = due.Select(c => { users.TryGetValue(c.UserId, out var u); return new Dictionary<string, object?>
        {
            ["vence"] = c.ExpiresAt, ["estado"] = c.Status == CardStatus.Expired ? "Vencida" : c.Status == CardStatus.Published ? "Vigente" : "Suspendida", ["tarjeta"] = c.Title, ["plan"] = c.Plan.ToString(),
            ["cliente"] = u?.FullName ?? "", ["correo"] = u?.Email is { } e && !e.EndsWith("@clientes.karsha.mx") ? e : "", ["telefono"] = u?.PhoneNumber ?? "",
        }; }).ToList();
        return Out(kpis, Series(renewals.Select(r => (r.PaidAt!.Value, 1L)), f, t, monthly), "number", "Renovaciones", groups, cols, table);
    }

    private async Task<IActionResult> Promos(DateTime f, DateTime t, bool monthly, CancellationToken ct)
    {
        var rows = await db.Payments.AsNoTracking().Where(p => p.Status == PaymentStatus.Paid && p.PaidAt >= f && p.PaidAt < t && p.CouponCode != null && p.CouponCode != "").ToListAsync(ct);
        var coupons = await db.Coupons.AsNoTracking().ToDictionaryAsync(c => c.Code, c => c, StringComparer.OrdinalIgnoreCase, ct);
        var kpis = new List<Kpi> { new("Ventas con cupón", rows.Count, "number"), new("Descuento dado", rows.Sum(r => (long)r.DiscountCents), "money"), new("Ingresos con cupón", rows.Sum(r => (long)r.AmountCents), "money"), new("Cupones distintos", rows.Select(r => r.CouponCode).Distinct().Count(), "number") };
        var by = rows.GroupBy(r => r.CouponCode!).OrderByDescending(g => g.Count()).ToList();
        var groups = new List<Group> { new("Usos por cupón", "number", by.Select(g => new Point(g.Key, g.Count())).ToList()), new("Descuento por cupón", "money", by.Select(g => new Point(g.Key, g.Sum(x => (long)x.DiscountCents))).ToList()) };
        var cols = new List<Col> { new("cupon", "Cupón", "text"), new("porcentaje", "Descuento %", "number"), new("usos", "Usos en el periodo", "number"), new("descuento", "Descuento dado", "money"), new("ingresos", "Ingresos", "money"), new("estado", "Estado", "text") };
        var table = by.Select(g => { coupons.TryGetValue(g.Key, out var c); return new Dictionary<string, object?>
        {
            ["cupon"] = g.Key, ["porcentaje"] = c?.Percent ?? 0, ["usos"] = g.Count(), ["descuento"] = g.Sum(x => (long)x.DiscountCents), ["ingresos"] = g.Sum(x => (long)x.AmountCents),
            ["estado"] = c is null ? "Eliminado" : !c.IsActive ? "Inactivo" : c.ExpiresAt < DateTime.UtcNow ? "Vencido" : "Activo",
        }; }).ToList();
        return Out(kpis, Series(rows.Select(r => (r.PaidAt!.Value, 1L)), f, t, monthly), "number", "Ventas con cupón", groups, cols, table);
    }

    private async Task<IActionResult> Customers(DateTime f, DateTime t, bool monthly, CancellationToken ct)
    {
        var clienteRole = await db.Roles.Where(r => r.Name == Roles.Cliente).Select(r => r.Id).FirstOrDefaultAsync(ct);
        var users = await db.Users.AsNoTracking().Where(u => u.CreatedAt >= f && u.CreatedAt < t && db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == clienteRole)).OrderByDescending(u => u.CreatedAt).ToListAsync(ct);
        var ids = users.Select(u => u.Id).ToList();
        var cards = await db.Cards.AsNoTracking().Where(c => ids.Contains(c.UserId)).Select(c => new { c.UserId, c.Status }).ToListAsync(ct);
        var paid = await db.Payments.AsNoTracking().Where(p => p.Status == PaymentStatus.Paid && ids.Contains(p.UserId)).GroupBy(p => p.UserId).Select(g => new { g.Key, Sum = g.Sum(x => (long)x.AmountCents) }).ToDictionaryAsync(x => x.Key, x => x.Sum, ct);
        var withPay = users.Count(u => paid.ContainsKey(u.Id));
        var neverPub = users.Count(u => !cards.Any(c => c.UserId == u.Id && c.Status is CardStatus.Published or CardStatus.Expired));
        var kpis = new List<Kpi> { new("Altas nuevas", users.Count, "number"), new("Ya pagaron", withPay, "number"), new("Conversión", users.Count == 0 ? 0 : (long)Math.Round(100.0 * withPay / users.Count), "percent"), new("Nunca publicaron", neverPub, "number") };
        var groups = new List<Group> { new("Por situación", "number", [new("Pagaron", withPay), new("Publicaron sin pagar", users.Count(u => !paid.ContainsKey(u.Id) && cards.Any(c => c.UserId == u.Id && c.Status == CardStatus.Published))), new("Solo se registraron", users.Count - withPay - users.Count(u => !paid.ContainsKey(u.Id) && cards.Any(c => c.UserId == u.Id && c.Status == CardStatus.Published))) ]) };
        var cols = new List<Col> { new("alta", "Alta", "date"), new("nombre", "Nombre", "text"), new("correo", "Correo", "text"), new("telefono", "WhatsApp", "text"), new("tarjetas", "Tarjetas", "number"), new("pagado", "Ha pagado", "money"), new("acceso", "Último acceso", "date") };
        var table = users.Select(u => new Dictionary<string, object?>
        {
            ["alta"] = u.CreatedAt, ["nombre"] = u.FullName, ["correo"] = u.Email is { } e && !e.EndsWith("@clientes.karsha.mx") ? e : "", ["telefono"] = u.PhoneNumber ?? "",
            ["tarjetas"] = cards.Count(c => c.UserId == u.Id), ["pagado"] = paid.GetValueOrDefault(u.Id), ["acceso"] = u.LastLoginAt,
        }).ToList();
        return Out(kpis, Series(users.Select(u => (u.CreatedAt, 1L)), f, t, monthly), "number", "Altas", groups, cols, table);
    }

    private async Task<IActionResult> Usage(DateTime f, DateTime t, bool monthly, CancellationToken ct)
    {
        var visits = await db.CardVisits.AsNoTracking().Where(v => v.VisitedAt >= f && v.VisitedAt < t).Select(v => new { v.CardId, v.VisitedAt }).ToListAsync(ct);
        var clicks = await db.CardClicks.AsNoTracking().Where(c => c.ClickedAt >= f && c.ClickedAt < t).Select(c => new { c.CardId, c.Kind }).ToListAsync(ct);
        var ids = visits.Select(v => v.CardId).Union(clicks.Select(c => c.CardId)).ToList();
        var cards = await db.Cards.AsNoTracking().Where(c => ids.Contains(c.Id)).Select(c => new { c.Id, c.Title, c.Product, c.UserId, Template = c.Template!.Name, c.Plan }).ToListAsync(ct);
        var owners = await db.Users.Where(u => cards.Select(c => c.UserId).Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName, ct);
        var kpis = new List<Kpi> { new("Visitas", visits.Count, "number"), new("Clics", clicks.Count, "number"), new("Tarjetas con visitas", visits.Select(v => v.CardId).Distinct().Count(), "number"), new("Clics por visita", visits.Count == 0 ? 0 : (long)Math.Round(100.0 * clicks.Count / visits.Count), "percent") };
        var groups = new List<Group>
        {
            new("Visitas por plantilla", "number", Top(visits.Select(v => (cards.FirstOrDefault(c => c.Id == v.CardId)?.Template ?? "—", 1L))).Take(10).ToList()),
            new("Clics por tipo", "number", Top(clicks.Select(c => (c.Kind, 1L))).Take(10).ToList()),
        };
        var cols = new List<Col> { new("tarjeta", "Tarjeta", "text"), new("dueno", "Dueño", "text"), new("plantilla", "Plantilla", "text"), new("plan", "Plan", "text"), new("visitas", "Visitas", "number"), new("clics", "Clics", "number") };
        var table = cards.Select(c => new Dictionary<string, object?>
        {
            ["tarjeta"] = c.Title, ["dueno"] = owners.GetValueOrDefault(c.UserId, ""), ["plantilla"] = c.Template, ["plan"] = c.Plan.ToString(),
            ["visitas"] = visits.Count(v => v.CardId == c.Id), ["clics"] = clicks.Count(x => x.CardId == c.Id),
        }).OrderByDescending(r => (int)r["visitas"]!).Take(200).ToList();
        return Out(kpis, Series(visits.Select(v => (v.VisitedAt, 1L)), f, t, monthly), "number", "Visitas", groups, cols, table);
    }
}
