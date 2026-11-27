using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TarjetasPro.Application.Common;
using TarjetasPro.Domain.Cards;
using TarjetasPro.Domain.Entities;
using TarjetasPro.Domain.Security;
using TarjetasPro.Infrastructure;
using Microsoft.Extensions.Options;
using TarjetasPro.Infrastructure.Identity;
using TarjetasPro.Infrastructure.Payments;
using TarjetasPro.Application.Cards;
using TarjetasPro.Infrastructure.Storage;

namespace TarjetasPro.Api.Controllers;

public record GrantCardRequest(Guid UserId, Guid TemplateId, CardPlan Plan, int Days, bool Demo, string? Title, string? Slug);
public record PromotionRequest(PromotionKind Kind, int Value, string? Label, bool IsActive, DateTime? StartsAt, DateTime? EndsAt);
public record GrantExistingRequest(CardPlan Plan, int Days);
public record ChargeRequest(string Method, CardPlan? Plan, int? AmountCents, bool Paid, Dictionary<string, string>? Data = null);
public record SaleRequest(Guid? CustomerId, string? FullName, string? Phone, string? Email, Guid TemplateId, CardPlan Plan, string? Title, string? Slug, string Method, int? AmountCents, bool Paid, Dictionary<string, string>? Data = null);
public record CouponRequest(string? Code, int Percent, CardPlan? Plan, string? Product, int? MaxUses, DateTime? ExpiresAt, bool IsActive, string? Note);

[ApiController]
[Route("api/admin")]
[Authorize(Policy = Permissions.CardsManageAll)]
public class AdminController(AppDbContext db, DriveStorageService drive, IOptions<PaymentOptions> payments, PaymentService payService, UserManager<ApplicationUser> users, TarjetasPro.Infrastructure.Auth.NotificationService notify) : ControllerBase
{
    /// <summary>Los correos que envía la plataforma, con vista previa (solo lectura).</summary>
    [HttpGet("emails")]
    public IActionResult Emails() => Ok(TarjetasPro.Infrastructure.Auth.EmailCatalog.Samples($"{Request.Scheme}://{Request.Host}"));

    /// <summary>Espacio usado/total en Google Drive.</summary>
    [HttpGet("storage")]
    public async Task<IActionResult> Storage(CancellationToken ct) => Ok(await drive.GetAsync(ct));

    /// <summary>Todas las tarjetas, con filtros por estado, dueño y texto.</summary>
    [HttpGet("cards")]
    public async Task<IActionResult> Cards(string? status, Guid? userId, string? search, int? page, int? pageSize, string? sort = null, string? dir = null)
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
        var desc = Sorting.IsDesc(dir);
        var ordered = sort switch
        {
            "title" => q.By(desc, c => c.Title),
            "owner" => q.By(desc, c => db.Users.Where(u => u.Id == c.UserId).Select(u => u.FullName).FirstOrDefault()),
            "template" => q.By(desc, c => c.Template!.Name),
            "status" => q.By(desc, c => c.Status),
            "expires" => q.ByEmptyLast(desc, c => c.ExpiresAt == null, c => c.ExpiresAt),
            "visits" => q.By(desc, c => db.CardVisits.Count(v => v.CardId == c.Id)),
            "updated" => q.By(desc, c => c.UpdatedAt),
            _ => q.OrderByDescending(c => c.UpdatedAt),
        };
        var rows = await ordered.ThenBy(c => c.Id).Skip((pg - 1) * size).Take(size)
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
                c.ExpiresAt,
                c.UpdatedAt,
                Visits = db.CardVisits.Count(v => v.CardId == c.Id),
            }).ToListAsync();
        return Ok(new Paged<object>(rows, total, pg, size));
    }

    /// <summary>Cuánta gente está navegando ahora (en memoria; tú no cuentas). Para decidir si conviene publicar.</summary>
    [HttpGet("online")]
    public IActionResult Online([FromServices] TarjetasPro.Api.Presence.PresenceTracker presence)
    {
        var me = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return Ok(presence.Snapshot(me));
    }

    /// <summary>Visitas a los enlaces: quién entró (IP), desde dónde (ubicación y origen) y con qué dispositivo.</summary>
    [HttpGet("visits")]
    public async Task<IActionResult> Visits(string? search, int? days, int? page, int? pageSize, string? kind, CancellationToken ct, string? sort = null, string? dir = null)
    {
        if (kind != "cards") return await SiteVisits(search, days, page, pageSize, sort, Sorting.IsDesc(dir), ct);
        var (pg, size) = Paged<object>.Normalize(page, pageSize);
        var q = db.CardVisits.AsNoTracking().AsQueryable();
        if (days is > 0) { var since = DateTime.UtcNow.AddDays(-days.Value); q = q.Where(v => v.VisitedAt >= since); }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().TrimStart('/');
            q = q.Where(v => db.Cards.Any(c => c.Id == v.CardId && (c.Slug.Contains(s) || c.Title.Contains(s)))
                          || (v.Ip != null && v.Ip.Contains(s)) || (v.City != null && v.City.Contains(s)));
        }

        var total = await q.CountAsync(ct);
        var uniqueIps = await q.Where(v => v.Ip != null).Select(v => v.Ip).Distinct().CountAsync(ct);
        var sources = await q.GroupBy(v => v.Source ?? "Sin dato").Select(g => new { label = g.Key, count = g.Count() })
            .OrderByDescending(x => x.count).Take(6).ToListAsync(ct);
        var places = await q.Where(v => v.City != null).GroupBy(v => v.City! + ", " + (v.Country ?? ""))
            .Select(g => new { label = g.Key, count = g.Count() }).OrderByDescending(x => x.count).Take(6).ToListAsync(ct);

        var desc = Sorting.IsDesc(dir);
        var ordered = sort switch
        {
            "date" => q.By(desc, v => v.VisitedAt),
            "page" => q.By(desc, v => db.Cards.IgnoreQueryFilters().Where(c => c.Id == v.CardId).Select(c => c.Title).FirstOrDefault()),
            "ip" => q.ByEmptyLast(desc, v => v.Ip == null, v => v.Ip),
            "place" => q.ByEmptyLast(desc, v => v.City == null, v => v.City),
            "source" => q.ByEmptyLast(desc, v => v.Source == null, v => v.Source),
            "device" => q.ByEmptyLast(desc, v => v.Device == null, v => v.Device),
            "repeats" => q.By(desc, v => v.Ip == null ? 0 : db.CardVisits.Count(o => o.CardId == v.CardId && o.Ip == v.Ip)),
            _ => q.OrderByDescending(v => v.VisitedAt),
        };
        var rows = await ordered.ThenByDescending(v => v.Id).Skip((pg - 1) * size).Take(size)
            .Select(v => new
            {
                v.Id,
                v.VisitedAt,
                v.Ip,
                v.Source,
                v.Device,
                v.Country,
                v.Region,
                v.City,
                Card = db.Cards.IgnoreQueryFilters().Where(c => c.Id == v.CardId).Select(c => c.Title).FirstOrDefault(),
                Slug = db.Cards.IgnoreQueryFilters().Where(c => c.Id == v.CardId).Select(c => c.Slug).FirstOrDefault(),
                // Cuántas veces ha entrado esa misma IP a esa tarjeta (para ver si regresa).
                Repeats = v.Ip == null ? 0 : db.CardVisits.Count(o => o.CardId == v.CardId && o.Ip == v.Ip),
            }).ToListAsync(ct);

        return Ok(new { items = rows, total, page = pg, pageSize = size, summary = new { total, uniqueIps, sources, places } });
    }

    /// <summary>Visitas a karsha.mx: quién entró a la página, por cuál página, desde dónde y con qué.</summary>
    private async Task<IActionResult> SiteVisits(string? search, int? days, int? page, int? pageSize, string? sort, bool desc, CancellationToken ct)
    {
        var (pg, size) = Paged<object>.Normalize(page, pageSize);
        var q = db.SiteVisits.AsNoTracking().AsQueryable();
        if (days is > 0) { var since = DateTime.UtcNow.AddDays(-days.Value); q = q.Where(v => v.VisitedAt >= since); }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(v => v.Path.Contains(s) || (v.Ip != null && v.Ip.Contains(s)) || (v.City != null && v.City.Contains(s)) || (v.Source != null && v.Source.Contains(s)));
        }

        var total = await q.CountAsync(ct);
        var uniqueIps = await q.Where(v => v.Ip != null).Select(v => v.Ip).Distinct().CountAsync(ct);
        var sources = await q.GroupBy(v => v.Source ?? "Sin dato").Select(g => new { label = g.Key, count = g.Count() })
            .OrderByDescending(x => x.count).Take(6).ToListAsync(ct);
        var places = await q.Where(v => v.City != null).GroupBy(v => v.City! + ", " + (v.Country ?? ""))
            .Select(g => new { label = g.Key, count = g.Count() }).OrderByDescending(x => x.count).Take(6).ToListAsync(ct);
        var pages = await q.GroupBy(v => v.Path).Select(g => new { label = g.Key, count = g.Count() })
            .OrderByDescending(x => x.count).Take(6).ToListAsync(ct);

        var ordered = sort switch
        {
            "date" => q.By(desc, v => v.VisitedAt),
            "page" => q.By(desc, v => v.Path),
            "ip" => q.ByEmptyLast(desc, v => v.Ip == null, v => v.Ip),
            "place" => q.ByEmptyLast(desc, v => v.City == null, v => v.City),
            "source" => q.ByEmptyLast(desc, v => v.Source == null, v => v.Source),
            "device" => q.ByEmptyLast(desc, v => v.Device == null, v => v.Device),
            "repeats" => q.By(desc, v => v.Ip == null ? 0 : db.SiteVisits.Count(o => o.Ip == v.Ip)),
            _ => q.OrderByDescending(v => v.VisitedAt),
        };
        var rows = await ordered.ThenByDescending(v => v.Id).Skip((pg - 1) * size).Take(size)
            .Select(v => new
            {
                v.Id,
                v.VisitedAt,
                v.Ip,
                v.Source,
                v.Device,
                v.Country,
                v.Region,
                v.City,
                Card = (string?)null,
                Slug = (string?)null,
                v.Path,
                // Cuántas veces ha entrado esa misma IP a la página (para ver si regresa).
                Repeats = v.Ip == null ? 0 : db.SiteVisits.Count(o => o.Ip == v.Ip),
            }).ToListAsync(ct);

        return Ok(new { items = rows, total, page = pg, pageSize = size, summary = new { total, uniqueIps, sources, places, pages } });
    }

    /// <summary>Papelera: tarjetas con borrado lógico, de la más reciente a la más antigua.</summary>
    [HttpGet("cards/deleted")]
    public async Task<IActionResult> DeletedCards(string? search, int? page, int? pageSize, string? sort = null, string? dir = null)
    {
        var (pg, size) = Paged<object>.Normalize(page, pageSize);
        var q = db.Cards.IgnoreQueryFilters().AsNoTracking().Where(c => c.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(c => c.Title.Contains(s) || c.Slug.Contains(s));
        }
        var total = await q.CountAsync();
        var desc = Sorting.IsDesc(dir);
        var ordered = sort switch
        {
            "title" => q.By(desc, c => c.Title),
            "owner" => q.By(desc, c => db.Users.Where(u => u.Id == c.UserId).Select(u => u.FullName).FirstOrDefault()),
            "template" => q.By(desc, c => c.Template!.Name),
            "status" => q.By(desc, c => c.Status),
            "expires" => q.ByEmptyLast(desc, c => c.ExpiresAt == null, c => c.ExpiresAt),
            "updated" => q.By(desc, c => c.DeletedAt),
            _ => q.OrderByDescending(c => c.DeletedAt),
        };
        var rows = await ordered.ThenBy(c => c.Id).Skip((pg - 1) * size).Take(size)
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
                c.ExpiresAt,
                c.DeletedAt,
            }).ToListAsync();
        return Ok(new Paged<object>(rows, total, pg, size));
    }

    /// <summary>Saca una tarjeta de la papelera con el estado que tenía al borrarse.</summary>
    [HttpPost("cards/{id:guid}/restore")]
    public async Task<IActionResult> RestoreCard(Guid id)
    {
        var card = await db.Cards.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id && c.IsDeleted);
        if (card is null) return NotFound();
        card.IsDeleted = false;
        card.DeletedAt = null;
        card.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(new { card.Id, card.Slug, Status = card.Status.ToString() });
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
    public async Task<IActionResult> Promotions(string? product)
    {
        product = CardLikeOrCards(product);
        var promos = await db.PlanPromotions.AsNoTracking().Where(x => x.Product == product).ToListAsync();
        var now = DateTime.UtcNow;
        var prices = await PlanCatalog.CardPlansAsync(db, product);
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
    public async Task<IActionResult> SetPromotion(CardPlan plan, PromotionRequest r, string? product)
    {
        product = CardLikeOrCards(product);
        if (!Enum.IsDefined(plan) || !Enum.IsDefined(r.Kind)) return BadRequest(new { errors = new[] { "Plan o tipo no válido." } });
        var list = await PlanCatalog.PriceAsync(db, plan, product);
        if (r.Kind == PromotionKind.Percent && r.Value is < 1 or > 90)
            return BadRequest(new { errors = new[] { "El porcentaje debe estar entre 1 y 90." } });
        if (r.Kind == PromotionKind.Amount && (r.Value < 100 || list - r.Value < PlanPromotion.MinFinalCents))
            return BadRequest(new { errors = new[] { $"El descuento debe ser de al menos $1 y dejar el plan en mínimo ${PlanPromotion.MinFinalCents / 100}." } });
        if (r.StartsAt is { } s && r.EndsAt is { } e && e <= s)
            return BadRequest(new { errors = new[] { "La fecha de fin debe ser posterior a la de inicio." } });

        var p = await db.PlanPromotions.FirstOrDefaultAsync(x => x.Product == product && x.Plan == plan);
        if (p is null) db.PlanPromotions.Add(p = new PlanPromotion { Product = product, Plan = plan });
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
    public async Task<IActionResult> DeletePromotion(CardPlan plan, string? product)
    {
        product = CardLikeOrCards(product);
        await db.PlanPromotions.Where(x => x.Product == product && x.Plan == plan).ExecuteDeleteAsync();
        return NoContent();
    }

    [HttpGet("coupons")]
    public async Task<IActionResult> Coupons()
    {
        var rows = await db.Coupons.AsNoTracking().OrderByDescending(c => c.CreatedAt).ToListAsync();
        var now = DateTime.UtcNow;
        return Ok(rows.Select(c => new
        {
            c.Id, c.Code, c.Percent, Plan = c.Plan?.ToString(), c.Product, c.MaxUses, c.Uses, c.ExpiresAt, c.IsActive, c.Note, c.CreatedAt,
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

    private static string CardLikeOrCards(string? product) =>
        Products.IsCardLike((product ?? "").Trim().ToLowerInvariant()) ? product!.Trim().ToLowerInvariant() : Products.Cards;

    private static string? Validate(CouponRequest r, Coupon c)
    {
        var code = (r.Code ?? "").Trim().ToUpperInvariant();
        if (code.Length is < 3 or > 30 || !code.All(ch => char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_'))
            return "El código debe tener de 3 a 30 caracteres: letras, números, guion o guion bajo.";
        if (r.Percent is < 1 or > 100) return "El descuento debe estar entre 1 y 100 %.";
        if (r.Plan is { } p && !Enum.IsDefined(p)) return "Plan no válido.";
        var prodKey = string.IsNullOrWhiteSpace(r.Product) ? null : r.Product.Trim().ToLowerInvariant();
        if (prodKey is not null && !Products.IsCardLike(prodKey)) return "Producto no válido.";
        if (r.MaxUses is < 1) return "El límite de usos debe ser de al menos 1.";
        c.Code = code;
        c.Percent = r.Percent;
        c.Plan = r.Plan;
        c.Product = prodKey;
        c.MaxUses = r.MaxUses;
        c.ExpiresAt = r.ExpiresAt;
        c.IsActive = r.IsActive;
        c.Note = (r.Note ?? "").Trim()[..Math.Min((r.Note ?? "").Trim().Length, 120)];
        return null;
    }


    // --- Venta asistida: el Admin arma el servicio por el cliente y registra el cobro a mano. ---

    private static readonly string[] ManualMethods = ["Efectivo", "Transferencia", "Otro"];
    private static string? NormMethod(string? m) => ManualMethods.FirstOrDefault(x => x.Equals((m ?? "").Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>Precio de lista de un plan para el producto de la tarjeta, para precargar el monto a cobrar.</summary>
    [HttpGet("cards/{id:guid}/quote")]
    public async Task<IActionResult> Quote(Guid id, CardPlan? plan)
    {
        var card = await db.Cards.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (card is null) return NotFound();
        var p = plan ?? card.Plan;
        return Ok(new { plan = p.ToString(), priceCents = await PlanCatalog.PriceAsync(db, p, card.Product), currency = payments.Value.Currency });
    }

    /// <summary>Registra el cobro de una tarjeta existente (efectivo, transferencia u otro). Pagado = publica y da recibo; si no, queda pendiente.</summary>
    [HttpPost("cards/{id:guid}/charge")]
    public async Task<IActionResult> Charge(Guid id, ChargeRequest r)
    {
        var method = NormMethod(r.Method);
        if (method is null) return BadRequest(new { errors = new[] { "Elige cómo pagó: efectivo, transferencia u otro." } });
        var card = await db.Cards.FirstOrDefaultAsync(c => c.Id == id);
        if (card is null) return NotFound();
        if (card.Status == CardStatus.Suspended) return BadRequest(new { errors = new[] { "La tarjeta está suspendida." } });
        var plan = r.Plan ?? card.Plan;
        if (!Enum.IsDefined(plan)) return BadRequest(new { errors = new[] { "Plan no válido." } });
        var amount = r.AmountCents ?? await PlanCatalog.PriceAsync(db, plan, card.Product);
        if (amount < 0 || amount > 100_000_00) return BadRequest(new { errors = new[] { "Monto no válido." } });
        var pay = await payService.RegisterManualAsync(card, plan, amount, method, r.Paid);
        return Ok(await ChargeResult(pay.Id, card.Id));
    }

    /// <summary>Liquida un cobro manual que había quedado pendiente.</summary>
    [HttpPut("payments/{id:guid}/pay")]
    public async Task<IActionResult> PayPending(Guid id)
    {
        var p = await db.Payments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();
        if (NormMethod(p.Provider) is null) return BadRequest(new { errors = new[] { "Solo se liquidan cobros registrados a mano." } });
        if (p.Status != PaymentStatus.Pending) return BadRequest(new { errors = new[] { "Este cobro ya no está pendiente." } });
        await payService.MarkPaidManualAsync(id);
        return Ok(await ChargeResult(id, p.CardId));
    }

    private async Task<object> ChargeResult(Guid paymentId, Guid cardId)
    {
        var p = await db.Payments.AsNoTracking().FirstAsync(x => x.Id == paymentId);
        var slug = await db.Cards.IgnoreQueryFilters().Where(c => c.Id == cardId).Select(c => c.Slug).FirstOrDefaultAsync();
        return new { paymentId = p.Id, cardId, slug, status = p.Status.ToString(), method = p.Provider, amountCents = p.AmountCents, folio = Payment.Folio(p.ReceiptNumber, p.PaidAt), periodEnd = p.PeriodEnd };
    }

    /// <summary>Una sola pantalla: cliente (nuevo o existente) + servicio + cobro. El cliente nuevo no necesita contraseña ni correo propio.</summary>
    [HttpPost("sales")]
    public async Task<IActionResult> Sale(SaleRequest r)
    {
        var method = NormMethod(r.Method);
        if (method is null) return BadRequest(new { errors = new[] { "Elige cómo pagó: efectivo, transferencia u otro." } });
        if (!Enum.IsDefined(r.Plan)) return BadRequest(new { errors = new[] { "Plan no válido." } });
        var template = await db.Templates.AsNoTracking().FirstOrDefaultAsync(t => t.Id == r.TemplateId && t.IsActive);
        if (template is null || !Products.IsCardLike(template.Product)) return BadRequest(new { errors = new[] { "Plantilla no disponible." } });
        if (r.AmountCents is < 0 or > 100_000_00) return BadRequest(new { errors = new[] { "Monto no válido." } });

        // 1) Cliente: uno existente o uno nuevo con nombre y teléfono.
        ApplicationUser? customer;
        if (r.CustomerId is { } cid)
        {
            customer = await users.FindByIdAsync(cid.ToString());
            if (customer is null) return BadRequest(new { errors = new[] { "Cliente no encontrado." } });
        }
        else
        {
            var name = (r.FullName ?? "").Trim();
            if (name.Length < 2) return BadRequest(new { errors = new[] { "Escribe el nombre del cliente." } });
            var phone = new string((r.Phone ?? "").Where(char.IsDigit).ToArray());
            var email = (r.Email ?? "").Trim().ToLowerInvariant();
            if (email.Length > 0 && await users.FindByEmailAsync(email) is not null)
                return BadRequest(new { errors = new[] { "Ese correo ya tiene cuenta. Elígelo en «Cliente registrado»." } });
            if (email.Length == 0) email = $"cliente-{Guid.NewGuid():N}"[..16] + "@clientes.karsha.mx";
            customer = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, FullName = name[..Math.Min(name.Length, 100)], PhoneNumber = phone.Length > 0 ? phone : null };
            var made = await users.CreateAsync(customer);
            if (!made.Succeeded) return BadRequest(new { errors = made.Errors.Select(e => e.Description).ToArray() });
            await users.AddToRoleAsync(customer, Roles.Cliente);
        }

        // 2) Tarjeta del cliente con lo básico ya puesto; el Admin completa el resto en el editor.
        var title = string.IsNullOrWhiteSpace(r.Title) ? (string.IsNullOrWhiteSpace(r.FullName) ? template.Name : r.FullName!.Trim()) : r.Title.Trim();
        title = title[..Math.Min(title.Length, 100)];
        var baseSlug = SlugRules.Normalize(string.IsNullOrWhiteSpace(r.Slug) ? title : r.Slug);
        if (SlugRules.Validate(baseSlug) is { } reason) return BadRequest(new { errors = new[] { reason } });
        var slug = baseSlug;
        for (var n = 2; await db.Cards.IgnoreQueryFilters().AnyAsync(c => c.Slug == slug); n++)
        {
            if (!string.IsNullOrWhiteSpace(r.Slug)) return BadRequest(new { errors = new[] { "Ese enlace ya está en uso." } });
            slug = $"{baseSlug}-{n}";
        }
        var keys = template.FieldKeys.Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var data = new Dictionary<string, string>();
        if (keys.Contains("nombre")) data["nombre"] = title;
        if (keys.Contains("whatsapp") && customer.PhoneNumber is { Length: > 0 } ph) data["whatsapp"] = ph;
        // Datos capturados en el modal de venta: solo los campos que esa plantilla usa.
        foreach (var (k, v) in r.Data ?? [])
            if (keys.Contains(k) && !string.IsNullOrWhiteSpace(v)) data[k] = v.Trim()[..Math.Min(v.Trim().Length, 2000)];
        var card = new Card
        {
            UserId = customer.Id, TemplateId = template.Id, Product = template.Product, Title = title, Slug = slug, Plan = r.Plan,
            DataJson = System.Text.Json.JsonSerializer.Serialize(data),
        };
        db.Cards.Add(card);
        await db.SaveChangesAsync();

        // 3) Cobro.
        var amount = r.AmountCents ?? await PlanCatalog.PriceAsync(db, r.Plan, template.Product);
        var pay = await payService.RegisterManualAsync(card, r.Plan, amount, method, r.Paid, sendEmails: false);
        var res = await ChargeResult(pay.Id, card.Id);
        // Un solo correo al cliente (si tiene correo propio): la tarjeta creada, con su recibo si ya pagó.
        var paid = await db.Payments.AsNoTracking().Where(x => x.Id == pay.Id).Select(x => new { x.ReceiptNumber, x.PaidAt }).FirstAsync();
        await notify.AssistedSaleAsync(customer.Id, card.Title, card.Slug, r.Paid, Payment.Folio(paid.ReceiptNumber, paid.PaidAt));
        return Ok(new { customerId = customer.Id, customerName = customer.FullName, product = card.Product, charge = res });
    }

    [HttpGet("payments")]
    public async Task<IActionResult> Payments(string? status, int? page, int? pageSize, string? sort = null, string? dir = null)
    {
        var (pg, size) = Paged<object>.Normalize(page, pageSize);
        var q = db.Payments.AsNoTracking().AsQueryable();
        if (Enum.TryParse<PaymentStatus>(status, out var st)) q = q.Where(p => p.Status == st);
        var total = await q.CountAsync();
        var desc = Sorting.IsDesc(dir);
        var ordered = sort switch
        {
            // La columna «Cliente» muestra su correo.
            "owner" => q.By(desc, p => db.Users.Where(u => u.Id == p.UserId).Select(u => u.Email).FirstOrDefault()),
            "card" => q.By(desc, p => p.CardTitle),
            "amount" => q.By(desc, p => p.AmountCents),
            "status" => q.By(desc, p => p.Status),
            "method" => q.By(desc, p => p.MethodLabel == null || p.MethodLabel == "" ? p.Provider : p.MethodLabel),
            "date" => q.By(desc, p => p.CreatedAt),
            "receipt" => q.ByEmptyLast(desc, p => p.ReceiptNumber == null, p => p.ReceiptNumber),
            "period" => q.ByEmptyLast(desc, p => p.PeriodEnd == null, p => p.PeriodEnd),
            _ => q.OrderByDescending(p => p.CreatedAt),
        };
        var rows = await ordered.ThenBy(p => p.Id).Skip((pg - 1) * size).Take(size).Select(p => new
        {
            p.Id,
            Status = p.Status.ToString(),
            p.Provider,
            p.MethodLabel, p.OxxoReference, p.VoucherUrl, p.VoucherExpiresAt,
            p.AmountCents,
            p.Currency,
            p.CreatedAt,
            p.PaidAt,
            p.PeriodEnd,
            p.CouponCode,
            p.ReceiptNumber,
            Card = p.CardTitle,
            p.CardId,
            Slug = db.Cards.Where(c => c.Id == p.CardId).Select(c => c.Slug).FirstOrDefault(),
            // Sin estado = la tarjeta ya no existe. Con estado distinto de Published, el panel ofrece la vista previa.
            CardStatus = db.Cards.Where(c => c.Id == p.CardId).Select(c => c.Status.ToString()).FirstOrDefault(),
            Owner = db.Users.Where(u => u.Id == p.UserId).Select(u => u.Email).FirstOrDefault(),
        }).ToListAsync();
        var items = rows.Select(r => new
        {
            r.Id, r.Status, r.Provider, r.MethodLabel, r.OxxoReference, r.VoucherUrl, r.VoucherExpiresAt, r.AmountCents, r.Currency, r.CreatedAt, r.PaidAt, r.PeriodEnd, r.CouponCode,
            Folio = Payment.Folio(r.ReceiptNumber, r.PaidAt), r.Card, r.CardId, r.Slug, r.CardStatus, r.Owner,
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

        var revPerDay = await db.Payments.Where(p => p.Status == PaymentStatus.Paid && p.PaidAt >= from14)
            .GroupBy(p => p.PaidAt!.Value.Date).Select(g => new { Day = g.Key, Cents = g.Sum(p => p.AmountCents) }).ToListAsync();
        var dailyRevenue = Enumerable.Range(0, 14).Select(i =>
        {
            var d = from14.AddDays(i);
            return new { date = d.ToString("yyyy-MM-dd"), cents = revPerDay.FirstOrDefault(x => x.Day == d)?.Cents ?? 0 };
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

        // Embudo: en qué paso se queda la gente (personas distintas en cada paso).
        var registered = await db.Users.CountAsync();
        var started = await db.Cards.Select(c => c.UserId).Distinct().CountAsync();
        var filled = await db.Cards.Where(c => c.DataJson != "{}" && c.DataJson != "").Select(c => c.UserId).Distinct().CountAsync();
        var toPay = await db.Payments.Select(p => p.UserId).Distinct().CountAsync();
        var paidPeople = await db.Payments.Where(p => p.Status == PaymentStatus.Paid).Select(p => p.UserId).Distinct().CountAsync();
        var perProduct = new List<object>();
        foreach (var pr in await db.Cards.Select(c => c.Product).Distinct().ToListAsync())
        {
            var cardsOf = db.Cards.Where(c => c.Product == pr);
            var idsOf = db.Cards.IgnoreQueryFilters().Where(c => c.Product == pr).Select(c => c.Id);
            var pays = db.Payments.Where(p => idsOf.Contains(p.CardId));
            perProduct.Add(new
            {
                product = pr,
                created = await cardsOf.CountAsync(),
                published = await cardsOf.CountAsync(c => c.PaidAt != null),
                steps = new object[]
                {
                    new { key = "started", label = "Empezaron", count = await cardsOf.Select(c => c.UserId).Distinct().CountAsync() },
                    new { key = "filled", label = "Llenaron sus datos", count = await cardsOf.Where(c => c.DataJson != "{}" && c.DataJson != "").Select(c => c.UserId).Distinct().CountAsync() },
                    new { key = "topay", label = "Llegaron a pagar", count = await pays.Select(p => p.UserId).Distinct().CountAsync() },
                    new { key = "paid", label = "Pagaron", count = await pays.Where(p => p.Status == PaymentStatus.Paid).Select(p => p.UserId).Distinct().CountAsync() },
                },
                stuck = new
                {
                    emptyDrafts = await cardsOf.CountAsync(c => c.Status == CardStatus.Draft && (c.DataJson == "{}" || c.DataJson == "")),
                    readyUnpaid = await cardsOf.CountAsync(c => c.Status == CardStatus.Draft && c.DataJson != "{}" && c.DataJson != ""),
                    abandoned3d = await cardsOf.CountAsync(c => c.Status == CardStatus.Draft && c.UpdatedAt < now.AddDays(-3)),
                    pendingPayments = await pays.CountAsync(p => p.Status == PaymentStatus.Pending),
                    failedPayments = await pays.CountAsync(p => p.Status == PaymentStatus.Failed || p.Status == PaymentStatus.Expired),
                },
            });
        }
        var funnel = new
        {
            steps = new object[]
            {
                new { key = "registered", label = "Se registraron", count = registered },
                new { key = "started", label = "Empezaron una tarjeta o catálogo", count = started },
                new { key = "filled", label = "Llenaron sus datos", count = filled },
                new { key = "topay", label = "Llegaron a pagar", count = toPay },
                new { key = "paid", label = "Pagaron", count = paidPeople },
            },
            stuck = new
            {
                emptyDrafts = await db.Cards.CountAsync(c => c.Status == CardStatus.Draft && (c.DataJson == "{}" || c.DataJson == "")),
                readyUnpaid = await db.Cards.CountAsync(c => c.Status == CardStatus.Draft && c.DataJson != "{}" && c.DataJson != ""),
                abandoned3d = await db.Cards.CountAsync(c => c.Status == CardStatus.Draft && c.UpdatedAt < now.AddDays(-3)),
                pendingPayments = await db.Payments.CountAsync(p => p.Status == PaymentStatus.Pending),
                failedPayments = await db.Payments.CountAsync(p => p.Status == PaymentStatus.Failed || p.Status == PaymentStatus.Expired),
            },
            products = perProduct,
        };

        // Con qué entra la gente: inicios de sesión de los últimos 30 días (veces y personas distintas) y cómo está vinculada cada cuenta.
        var loginRows = await db.LoginEvents.Where(l => l.At >= since30)
            .GroupBy(l => l.Method).Select(g => new { Method = g.Key, Times = g.Count(), People = g.Select(l => l.UserId).Distinct().Count() }).ToListAsync();
        var linked = await db.Set<IdentityUserLogin<Guid>>().GroupBy(l => l.LoginProvider)
            .Select(g => new { Method = g.Key, Accounts = g.Select(l => l.UserId).Distinct().Count() }).ToListAsync();
        var withPassword = await db.Users.CountAsync(u => u.PasswordHash != null);
        var logins = new
        {
            // Marcada como UTC para que el navegador la pase a la hora de México (si no, de noche sale el día siguiente).
            since = await db.LoginEvents.MinAsync(l => (DateTime?)l.At) is { } first ? DateTime.SpecifyKind(first, DateTimeKind.Utc) : (DateTime?)null,
            methods = new[] { LoginEvent.Email, LoginEvent.Google, LoginEvent.Facebook }.Select(m => new
            {
                method = m,
                times = loginRows.FirstOrDefault(r => r.Method == m)?.Times ?? 0,
                people = loginRows.FirstOrDefault(r => r.Method == m)?.People ?? 0,
                accounts = m == LoginEvent.Email ? withPassword : linked.FirstOrDefault(r => r.Method == m)?.Accounts ?? 0,
            }),
        };

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
            dailyRevenue,
            top,
            funnel,
            logins,
        });
    }
}
