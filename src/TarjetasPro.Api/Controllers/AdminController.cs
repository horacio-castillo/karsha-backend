using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TarjetasPro.Application.Common;
using TarjetasPro.Domain.Cards;
using TarjetasPro.Domain.Entities;
using TarjetasPro.Domain.Security;
using TarjetasPro.Infrastructure;
using Microsoft.Extensions.Options;
using TarjetasPro.Infrastructure.Payments;
using TarjetasPro.Infrastructure.Storage;

namespace TarjetasPro.Api.Controllers;

public record GrantCardRequest(Guid UserId, Guid TemplateId, CardPlan Plan, int Days, bool Demo, string? Title, string? Slug);
public record PromotionRequest(PromotionKind Kind, int Value, string? Label, bool IsActive, DateTime? StartsAt, DateTime? EndsAt);
public record GrantExistingRequest(CardPlan Plan, int Days);
public record CouponRequest(string? Code, int Percent, CardPlan? Plan, int? MaxUses, DateTime? ExpiresAt, bool IsActive, string? Note);

[ApiController]
[Route("api/admin")]
[Authorize(Policy = Permissions.CardsManageAll)]
public class AdminController(AppDbContext db, DriveStorageService drive, IOptions<PaymentOptions> payments) : ControllerBase
{
    /// <summary>Espacio usado/total en Google Drive.</summary>
    [HttpGet("storage")]
    public async Task<IActionResult> Storage(CancellationToken ct) => Ok(await drive.GetAsync(ct));

    /// <summary>Todas las tarjetas, con filtros por estado, dueño y texto.</summary>
    [HttpGet("cards")]
    public async Task<IActionResult> Cards(string? status, Guid? userId, string? search, int? page, int? pageSize)
    {
        var (pg, size) = Paged<object>.Normalize(page, pageSize);
        var q = db.Cards.AsNoTracking().AsQueryable();
        if (Enum.TryParse<CardStatus>(status, out var st)) q = q.Where(c => c.Status == st);
        if (userId is not null) q = q.Where(c => c.UserId == userId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(c => c.Title.Contains(s) || c.Slug.Contains(s));
        }

        var total = await q.CountAsync();
        var rows = await q.OrderByDescending(c => c.UpdatedAt).ThenBy(c => c.Id).Skip((pg - 1) * size).Take(size)
            .Select(c => new
            {
                c.Id,
                c.Title,
                c.Slug,
                Status = c.Status.ToString(),
                Template = c.Template!.Name,
                OwnerId = c.UserId,
                Owner = db.Users.Where(u => u.Id == c.UserId).Select(u => u.Email).FirstOrDefault(),
                OwnerName = db.Users.Where(u => u.Id == c.UserId).Select(u => u.FullName).FirstOrDefault(),
                c.PaidAt,
                c.UpdatedAt,
                Visits = db.CardVisits.Count(v => v.CardId == c.Id),
            }).ToListAsync();
        return Ok(new Paged<object>(rows, total, pg, size));
    }

    /// <summary>
    /// Crea una tarjeta publicada para un usuario sin pasar por el pago: regalo (plan y vigencia a elegir)
    /// o demo (plan Pro, vigencia corta). No genera Payment, así que no cuenta en los ingresos.
    /// </summary>
    [HttpPost("cards/grant")]
    public async Task<IActionResult> Grant(GrantCardRequest r)
    {
        if (!Enum.IsDefined(r.Plan)) return BadRequest(new { errors = new[] { "Plan no válido." } });
        if (r.Days is < 1 or > 3650) return BadRequest(new { errors = new[] { "La vigencia debe ser de 1 a 3650 días." } });
        if (!await db.Users.AnyAsync(u => u.Id == r.UserId))
            return BadRequest(new { errors = new[] { "Usuario no encontrado." } });

        var template = await db.Templates.AsNoTracking().FirstOrDefaultAsync(t => t.Id == r.TemplateId && t.IsActive);
        if (template is null) return BadRequest(new { errors = new[] { "Plantilla no disponible." } });

        var title = string.IsNullOrWhiteSpace(r.Title) ? (r.Demo ? $"Demo {template.Name}" : template.Name) : r.Title.Trim();
        var slug = SlugRules.Normalize(string.IsNullOrWhiteSpace(r.Slug) ? title : r.Slug);
        if (SlugRules.Validate(slug) is { } reason) return BadRequest(new { errors = new[] { reason } });
        if (await db.Cards.IgnoreQueryFilters().AnyAsync(c => c.Slug == slug))
            return BadRequest(new { errors = new[] { "Ese enlace ya está en uso." } });

        var now = DateTime.UtcNow;
        var card = new Card
        {
            UserId = r.UserId,
            TemplateId = template.Id,
            Title = title[..Math.Min(title.Length, 100)],
            Slug = slug,
            Status = CardStatus.Published,
            Plan = r.Demo ? CardPlan.Pro : r.Plan,
            PaidAt = now,
            ExpiresAt = now.AddDays(r.Days),
        };
        db.Cards.Add(card);
        await db.SaveChangesAsync();
        return Ok(new { card.Id, card.Slug, Plan = card.Plan.ToString(), card.ExpiresAt });
    }

    /// <summary>Regala plan y días a una tarjeta existente (suma a la vigencia actual si aún no vence).</summary>
    [HttpPut("cards/{id:guid}/grant")]
    public async Task<IActionResult> GrantExisting(Guid id, GrantExistingRequest r)
    {
        if (!Enum.IsDefined(r.Plan)) return BadRequest(new { errors = new[] { "Plan no válido." } });
        if (r.Days is < 1 or > 3650) return BadRequest(new { errors = new[] { "La vigencia debe ser de 1 a 3650 días." } });
        var card = await db.Cards.FirstOrDefaultAsync(c => c.Id == id);
        if (card is null) return NotFound();

        var now = DateTime.UtcNow;
        var active = card.Status == CardStatus.Published && card.ExpiresAt is { } e && e > now;
        var start = active ? card.ExpiresAt!.Value : now;
        card.ExpiresAt = start.AddDays(r.Days);
        card.Plan = active ? (CardPlan)Math.Max((int)card.Plan, (int)r.Plan) : r.Plan;
        card.PaidAt ??= now;
        // Una tarjeta suspendida sigue suspendida; el resto queda publicada.
        if (card.Status != CardStatus.Suspended) card.Status = CardStatus.Published;
        card.UpdatedAt = now;
        await db.SaveChangesAsync();
        return Ok(new { card.Id, Plan = card.Plan.ToString(), card.ExpiresAt });
    }

    /// <summary>Precio de lista y promoción (si hay) de cada plan.</summary>
    [HttpGet("promotions")]
    public async Task<IActionResult> Promotions()
    {
        var promos = await db.PlanPromotions.AsNoTracking().ToListAsync();
        var now = DateTime.UtcNow;
        var prices = await PlanCatalog.CardPlansAsync(db);
        return Ok(Enum.GetValues<CardPlan>().Select(plan =>
        {
            var list = prices.TryGetValue(plan, out var ps) ? ps.PriceCents : 0;
            var p = promos.FirstOrDefault(x => x.Plan == plan);
            return new
            {
                Plan = plan.ToString(),
                ListPriceCents = list,
                Promotion = p is null ? null : new
                {
                    Kind = p.Kind.ToString(), p.Value, p.Label, p.IsActive, p.StartsAt, p.EndsAt,
                    Live = p.AppliesAt(now),
                    FinalPriceCents = p.Apply(list),
                },
            };
        }));
    }

    [HttpPut("promotions/{plan}")]
    public async Task<IActionResult> SetPromotion(CardPlan plan, PromotionRequest r)
    {
        if (!Enum.IsDefined(plan) || !Enum.IsDefined(r.Kind)) return BadRequest(new { errors = new[] { "Plan o tipo no válido." } });
        var list = await PlanCatalog.PriceAsync(db, plan);
        if (r.Kind == PromotionKind.Percent && r.Value is < 1 or > 90)
            return BadRequest(new { errors = new[] { "El porcentaje debe estar entre 1 y 90." } });
        if (r.Kind == PromotionKind.Amount && (r.Value < 100 || list - r.Value < PlanPromotion.MinFinalCents))
            return BadRequest(new { errors = new[] { $"El descuento debe ser de al menos $1 y dejar el plan en mínimo ${PlanPromotion.MinFinalCents / 100}." } });
        if (r.StartsAt is { } s && r.EndsAt is { } e && e <= s)
            return BadRequest(new { errors = new[] { "La fecha de fin debe ser posterior a la de inicio." } });

        var p = await db.PlanPromotions.FirstOrDefaultAsync(x => x.Plan == plan);
        if (p is null) db.PlanPromotions.Add(p = new PlanPromotion { Plan = plan });
        p.Kind = r.Kind;
        p.Value = r.Value;
        p.Label = (r.Label ?? "").Trim();
        p.IsActive = r.IsActive;
        p.StartsAt = r.StartsAt;
        p.EndsAt = r.EndsAt;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("promotions/{plan}")]
    public async Task<IActionResult> DeletePromotion(CardPlan plan)
    {
        await db.PlanPromotions.Where(x => x.Plan == plan).ExecuteDeleteAsync();
        return NoContent();
    }

    [HttpGet("coupons")]
    public async Task<IActionResult> Coupons()
    {
        var rows = await db.Coupons.AsNoTracking().OrderByDescending(c => c.CreatedAt).ToListAsync();
        var now = DateTime.UtcNow;
        return Ok(rows.Select(c => new
        {
            c.Id, c.Code, c.Percent, Plan = c.Plan?.ToString(), c.MaxUses, c.Uses, c.ExpiresAt, c.IsActive, c.Note, c.CreatedAt,
            Live = c.IsActive && (c.ExpiresAt is null || c.ExpiresAt > now) && (c.MaxUses is null || c.Uses < c.MaxUses),
        }));
    }

    [HttpPost("coupons")]
    public async Task<IActionResult> CreateCoupon(CouponRequest r)
    {
        var c = new Coupon();
        if (Validate(r, c) is { } err) return BadRequest(new { errors = new[] { err } });
        if (await db.Coupons.AnyAsync(x => x.Code == c.Code)) return BadRequest(new { errors = new[] { "Ya existe un cupón con ese código." } });
        db.Coupons.Add(c);
        await db.SaveChangesAsync();
        return Ok(new { c.Id });
    }

    [HttpPut("coupons/{id:guid}")]
    public async Task<IActionResult> UpdateCoupon(Guid id, CouponRequest r)
    {
        var c = await db.Coupons.FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound();
        if (Validate(r, c) is { } err) return BadRequest(new { errors = new[] { err } });
        if (await db.Coupons.AnyAsync(x => x.Code == c.Code && x.Id != id)) return BadRequest(new { errors = new[] { "Ya existe un cupón con ese código." } });
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("coupons/{id:guid}")]
    public async Task<IActionResult> DeleteCoupon(Guid id)
    {
        await db.Coupons.Where(x => x.Id == id).ExecuteDeleteAsync();
        return NoContent();
    }

    private static string? Validate(CouponRequest r, Coupon c)
    {
        var code = (r.Code ?? "").Trim().ToUpperInvariant();
        if (code.Length is < 3 or > 30 || !code.All(ch => char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_'))
            return "El código debe tener de 3 a 30 caracteres: letras, números, guion o guion bajo.";
        if (r.Percent is < 1 or > 100) return "El descuento debe estar entre 1 y 100 %.";
        if (r.Plan is { } p && !Enum.IsDefined(p)) return "Plan no válido.";
        if (r.MaxUses is < 1) return "El límite de usos debe ser de al menos 1.";
        c.Code = code;
        c.Percent = r.Percent;
        c.Plan = r.Plan;
        c.MaxUses = r.MaxUses;
        c.ExpiresAt = r.ExpiresAt;
        c.IsActive = r.IsActive;
        c.Note = (r.Note ?? "").Trim()[..Math.Min((r.Note ?? "").Trim().Length, 120)];
        return null;
    }

    [HttpGet("payments")]
    public async Task<IActionResult> Payments(string? status, int? page, int? pageSize)
    {
        var (pg, size) = Paged<object>.Normalize(page, pageSize);
        var q = db.Payments.AsNoTracking().AsQueryable();
        if (Enum.TryParse<PaymentStatus>(status, out var st)) q = q.Where(p => p.Status == st);
        var total = await q.CountAsync();
        var rows = await q.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Id).Skip((pg - 1) * size).Take(size).Select(p => new
        {
            p.Id,
            Status = p.Status.ToString(),
            p.Provider,
            p.AmountCents,
            p.Currency,
            p.CreatedAt,
            p.PaidAt,
            p.PeriodEnd,
            p.CouponCode,
            p.ReceiptNumber,
            Card = p.CardTitle,
            Slug = db.Cards.Where(c => c.Id == p.CardId).Select(c => c.Slug).FirstOrDefault(),
            Owner = db.Users.Where(u => u.Id == p.UserId).Select(u => u.Email).FirstOrDefault(),
        }).ToListAsync();
        var items = rows.Select(r => new
        {
            r.Id, r.Status, r.Provider, r.AmountCents, r.Currency, r.CreatedAt, r.PaidAt, r.PeriodEnd, r.CouponCode,
            Folio = Payment.Folio(r.ReceiptNumber, r.PaidAt), r.Card, r.Slug, r.Owner,
        }).ToList();
        return Ok(new Paged<object>(items, total, pg, size));
    }

    [HttpGet("stats")]
    public async Task<IActionResult> Stats()
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var from14 = today.AddDays(-13);

        var perDay = await db.CardVisits.Where(v => v.VisitedAt >= from14)
            .GroupBy(v => v.VisitedAt.Date).Select(g => new { Day = g.Key, Count = g.Count() }).ToListAsync();
        var daily = Enumerable.Range(0, 14).Select(i =>
        {
            var d = from14.AddDays(i);
            return new { date = d.ToString("yyyy-MM-dd"), count = perDay.FirstOrDefault(x => x.Day == d)?.Count ?? 0 };
        }).ToList();

        var since30 = today.AddDays(-29);
        var topRaw = await db.CardVisits.Where(v => v.VisitedAt >= since30 && db.Cards.Any(c => c.Id == v.CardId))
            .GroupBy(v => v.CardId).Select(g => new { CardId = g.Key, Visits = g.Count() })
            .OrderByDescending(x => x.Visits).Take(5).ToListAsync();
        var ids = topRaw.Select(t => t.CardId).ToList();
        var titles = await db.Cards.Where(c => ids.Contains(c.Id)).Select(c => new { c.Id, c.Title, c.Slug }).ToListAsync();
        var top = topRaw.Select(t =>
        {
            var c = titles.First(x => x.Id == t.CardId);
            return new { slug = c.Slug, title = c.Title, visits = t.Visits };
        });

        return Ok(new
        {
            users = new
            {
                total = await db.Users.CountAsync(),
                active = await db.Users.CountAsync(u => u.IsActive),
            },
            cards = new
            {
                draft = await db.Cards.CountAsync(c => c.Status == CardStatus.Draft),
                published = await db.Cards.CountAsync(c => c.Status == CardStatus.Published),
                suspended = await db.Cards.CountAsync(c => c.Status == CardStatus.Suspended),
                expired = await db.Cards.CountAsync(c => c.Status == CardStatus.Expired),
            },
            visits = new
            {
                today = await db.CardVisits.CountAsync(v => v.VisitedAt >= today),
                last7 = await db.CardVisits.CountAsync(v => v.VisitedAt >= today.AddDays(-6)),
                last30 = await db.CardVisits.CountAsync(v => v.VisitedAt >= since30),
                total = await db.CardVisits.CountAsync(),
            },
            revenue = new
            {
                last30 = await db.Payments.Where(p => p.Status == PaymentStatus.Paid && p.PaidAt >= since30).SumAsync(p => (int?)p.AmountCents) ?? 0,
                total = await db.Payments.Where(p => p.Status == PaymentStatus.Paid).SumAsync(p => (int?)p.AmountCents) ?? 0,
                currency = "mxn",
            },
            daily,
            top,
        });
    }
}
