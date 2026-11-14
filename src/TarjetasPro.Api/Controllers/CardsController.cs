using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TarjetasPro.Application.Cards;
using TarjetasPro.Application.Canvas;
using TarjetasPro.Application.Templates;
using TarjetasPro.Domain.Cards;
using TarjetasPro.Domain.Entities;
using TarjetasPro.Domain.Security;
using Microsoft.Extensions.Options;
using TarjetasPro.Infrastructure;
using TarjetasPro.Infrastructure.Payments;

namespace TarjetasPro.Api.Controllers;

public record StatusRequest(CardStatus Status);

[ApiController]
[Route("api/cards")]
[Authorize]
public class CardsController(AppDbContext db, IConfiguration config, IOptions<PaymentOptions> payments, IWebHostEnvironment env) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue("sub")!);
    private bool CanManageAll => User.HasClaim(Permissions.ClaimType, Permissions.CardsManageAll);
    private string AssetBase => $"{Request.Scheme}://{Request.Host}";
    private int MaxCards => config.GetValue("Cards:MaxDraftsPerUser", 10);

    /// <summary>Productos que un cliente puede contratar hoy con el editor de tarjetas (los apagados no aparecen).</summary>
    [AllowAnonymous]
    [HttpGet("products")]
    public async Task<IActionResult> SellableProducts() =>
        Ok(await db.ProductSettings.AsNoTracking()
            .Where(p => p.IsActive && (p.Key == Products.Cards || p.Key == Products.Menu)).OrderBy(p => p.SortOrder)
            .Select(p => new { p.Key, p.Name, p.Description }).ToListAsync());

    [AllowAnonymous]
    [HttpGet("config")]
    public async Task<IActionResult> Config(string? product)
    {
        var prod = Products.IsCardLike((product ?? "").Trim().ToLowerInvariant()) ? product!.Trim().ToLowerInvariant() : Products.Cards;
        var now = DateTime.UtcNow;
        var promos = prod != Products.Cards ? [] : (await db.PlanPromotions.AsNoTracking().ToListAsync()).Where(p => p.AppliesAt(now)).ToDictionary(p => p.Plan);
        var settings = (await db.PlanSettings.AsNoTracking()
            .Where(p => p.Product == prod && p.Plan != null && p.IsActive)
            .OrderBy(p => p.SortOrder).ToListAsync());
        PlanInfo Plan(PlanSetting ps)
        {
            var plan = ps.Plan!.Value;
            if (!promos.TryGetValue(plan, out var promo))
                return new(plan.ToString(), ps.Name, ps.PriceCents, ps.FeatureList(), ps.PriceCents, null, null, ps.Tagline, ps.IsHighlighted);
            return new(plan.ToString(), ps.Name, promo.Apply(ps.PriceCents), ps.FeatureList(), ps.PriceCents,
                string.IsNullOrWhiteSpace(promo.Label) ? "Promoción" : promo.Label, promo.EndsAt, ps.Tagline, ps.IsHighlighted);
        }

        return Ok(new CardsConfig(MaxCards, payments.Value.Provider, payments.Value.Currency, payments.Value.Months,
        settings.Select(Plan).ToArray(), ThemeRules.Fonts.Select(f => new FontInfo(f.Key, f.Value)).ToArray(),
            ThemeRules.Decors.Keys.ToArray(), ThemeRules.ButtonStyles, ThemeRules.PhotoStyles, ThemeRules.ButtonLayouts, ThemeRules.GalleryLayouts));
    }

    [HttpGet]
    [Authorize(Policy = Permissions.CardsViewOwn)]
    public async Task<IActionResult> List()
    {
        var uid = UserId;
        var cards = await db.Cards.AsNoTracking()
            .Where(c => c.UserId == uid)
            .OrderByDescending(c => c.UpdatedAt)
            .Select(c => new CardSummary(c.Id, c.Title, c.Slug, c.TemplateId, c.Template!.Name,
                c.Status.ToString(), c.UpdatedAt, db.CardVisits.Count(v => v.CardId == c.Id), c.ExpiresAt, c.Plan.ToString(), c.Product))
            .ToListAsync();
        return Ok(cards);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.CardsViewOwn)]
    public async Task<IActionResult> Get(Guid id)
    {
        var card = await Find(id);
        return card is null ? NotFound() : Ok(ToDetail(card));
    }

    [HttpGet("slug-check")]
    [Authorize(Policy = Permissions.CardsCreate)]
    public async Task<IActionResult> SlugCheckEndpoint(string slug, Guid? excludeId)
    {
        var normalized = SlugRules.Normalize(slug);
        if (SlugRules.Validate(normalized) is { } reason) return Ok(new SlugCheck(false, normalized, reason));
        var taken = await db.Cards.IgnoreQueryFilters().AnyAsync(c => c.Slug == normalized && c.Id != excludeId);
        return Ok(new SlugCheck(!taken, normalized, taken ? "Ese enlace ya está en uso." : null));
    }

    /// <summary>Vista previa (con marca de agua) de datos aún sin guardar.</summary>
    [HttpPost("preview")]
    [Authorize(Policy = Permissions.CardsCreate)]
    public async Task<IActionResult> Preview(CardPreviewRequest r)
    {
        if (r.Canvas is not null)
            return Ok(new { html = CanvasRules.Render(r.Canvas, AssetBase, watermark: true) });
        var t = await db.Templates.AsNoTracking().FirstOrDefaultAsync(x => x.Id == r.TemplateId && x.IsActive);
        if (t is null) return NotFound(new { errors = new[] { "Plantilla no disponible." } });
        var plan = Enum.TryParse<CardPlan>(r.Plan, true, out var pl) ? pl : CardPlan.Sencilla;
        var extras = BuildExtras(plan, t, r.Theme, r.Gallery, r.VideoUrl, null);
        extras = extras! with { Art = ArtCatalog.Compose(env, t.Code, extras.Theme, AssetBase) };
        return Ok(new { html = TemplateRenderer.Render(t.Html, t.Css, r.Data, AssetBase, watermark: true, extras) });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.CardsCreate)]
    public async Task<IActionResult> Create(CardRequest r)
    {
        var uid = UserId;
        if (!CanManageAll && await db.Cards.CountAsync(c => c.UserId == uid && c.Status == CardStatus.Draft) >= MaxCards)
            return BadRequest(new { errors = new[] { $"Tienes {MaxCards} borradores sin pagar. Publica o elimina alguno para crear otra tarjeta." } });

        var card = new Card { UserId = uid };
        if (await Apply(card, r) is { } error) return error;
        db.Cards.Add(card);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = card.Id }, ToDetail(card));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.CardsEditOwn)]
    public async Task<IActionResult> Update(Guid id, CardRequest r)
    {
        var card = await Find(id);
        if (card is null) return NotFound();
        if (await Apply(card, r) is { } error) return error;
        await db.SaveChangesAsync();
        return Ok(ToDetail(card));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.CardsEditOwn)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var card = await Find(id);
        if (card is null) return NotFound();
        card.IsDeleted = true;
        card.DeletedAt = DateTime.UtcNow;
        card.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Estadísticas detalladas (plan Pro): visitas por día, clics por botón y totales de los últimos 30 días.</summary>
    [HttpGet("{id:guid}/stats")]
    [Authorize(Policy = Permissions.CardsViewOwn)]
    public async Task<IActionResult> Stats(Guid id)
    {
        var card = await Find(id);
        if (card is null) return NotFound();
        if (card.Plan < CardPlan.Pro)
            return StatusCode(403, new { errors = new[] { "Las estadísticas detalladas son del plan Pro." } });

        var from = DateTime.UtcNow.Date.AddDays(-29);
        var visits = await db.CardVisits.AsNoTracking().Where(v => v.CardId == id && v.VisitedAt >= from)
            .GroupBy(v => v.VisitedAt.Date).Select(g => new { Day = g.Key, N = g.Count() }).ToListAsync();
        var clicks = await db.CardClicks.AsNoTracking().Where(c => c.CardId == id && c.ClickedAt >= from)
            .Select(c => new { c.Kind, c.ClickedAt }).ToListAsync();

        var days = Enumerable.Range(0, 30).Select(i => from.AddDays(i)).Select(d => new
        {
            Day = d.ToString("yyyy-MM-dd"),
            Visits = visits.Where(v => v.Day == d).Sum(v => v.N),
            Clicks = clicks.Count(c => c.ClickedAt.Date == d),
        }).ToList();

        return Ok(new
        {
            TotalVisits = await db.CardVisits.CountAsync(v => v.CardId == id),
            Visits30 = visits.Sum(v => v.N),
            Clicks30 = clicks.Count,
            ByKind = clicks.GroupBy(c => c.Kind).Select(g => new { Kind = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count).ToList(),
            Days = days,
        });
    }

    /// <summary>El Admin puede publicar, suspender o regresar a borrador cualquier tarjeta.</summary>
    [HttpPut("{id:guid}/status")]
    [Authorize(Policy = Permissions.CardsManageAll)]
    public async Task<IActionResult> SetStatus(Guid id, StatusRequest request)
    {
        var status = request.Status;
        var card = await db.Cards.FirstOrDefaultAsync(c => c.Id == id);
        if (card is null) return NotFound();
        card.Status = status;
        if (status == CardStatus.Published)
        {
            card.PaidAt ??= DateTime.UtcNow;
            // Publicar a mano una tarjeta vencida es una cortesía del Admin: queda sin vencimiento.
            if (card.ExpiresAt <= DateTime.UtcNow) card.ExpiresAt = null;
        }
        card.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(ToDetail(card));
    }

    private async Task<Card?> Find(Guid id)
    {
        var uid = UserId;
        return await db.Cards.FirstOrDefaultAsync(c => c.Id == id && (c.UserId == uid || CanManageAll));
    }

    private async Task<IActionResult?> Apply(Card card, CardRequest r)
    {
        // El lienzo (Pro Max) no usa una plantilla del catálogo: todas comparten una interna.
        var wantedPlan = !IsActive(card) && Enum.TryParse<CardPlan>(r.Plan, true, out var wp) && Enum.IsDefined(wp) ? wp : card.Plan;
        if (r.Canvas is not null && wantedPlan != CardPlan.ProMax)
            return BadRequest(new { errors = new[] { "El editor de lienzo es del plan Pro Max." } });
        var template = r.Canvas is not null
            ? await db.Templates.AsNoTracking().FirstOrDefaultAsync(t => t.Code == CanvasTemplate.Code)
            : await db.Templates.AsNoTracking().FirstOrDefaultAsync(t => t.Id == r.TemplateId);
        if (template is null || (r.Canvas is null && !template.IsActive && template.Id != card.TemplateId))
            return BadRequest(new { errors = new[] { "Plantilla no disponible." } });

        // El lienzo comparte una plantilla interna: el producto (tarjeta o menú) lo indica el cliente.
        var product = r.Canvas is not null && Products.IsCardLike((r.Product ?? "").Trim().ToLowerInvariant())
            ? r.Product!.Trim().ToLowerInvariant() : template.Product;
        if (!Products.IsCardLike(product))
            return BadRequest(new { errors = new[] { "Esa plantilla es de otro producto." } });
        // Un producto apagado no admite tarjetas nuevas ni cambiar a él; las ya creadas siguen editándose.
        if (card.PaidAt is null && (db.Entry(card).State == EntityState.Detached || card.Product != product)
            && !await PlanCatalog.IsSellableAsync(db, product))
            return BadRequest(new { errors = new[] { "Este producto no está disponible por ahora." } });

        var slug = SlugRules.Normalize(r.Slug);
        if (SlugRules.Validate(slug) is { } reason) return BadRequest(new { errors = new[] { reason } });
        if (await db.Cards.IgnoreQueryFilters().AnyAsync(c => c.Slug == slug && c.Id != card.Id))
            return BadRequest(new { errors = new[] { "Ese enlace ya está en uso." } });

        // Solo se guardan los campos que la plantilla realmente usa.
        var allowed = template.FieldKeys.Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var data = (r.Data ?? [])
            .Where(kv => allowed.Contains(kv.Key) && !string.IsNullOrWhiteSpace(kv.Value))
            .ToDictionary(kv => kv.Key, kv => kv.Value.Trim()[..Math.Min(kv.Value.Trim().Length, 1000)]);

        card.Title = r.Title.Trim();
        card.TemplateId = template.Id;
        card.Product = product;
        card.Slug = slug;
        card.DataJson = JsonSerializer.Serialize(data);

        // El plan solo se elige mientras la tarjeta no esté vigente; después cambia únicamente pagando.
        if (!IsActive(card) && Enum.TryParse<CardPlan>(r.Plan, true, out var plan) && Enum.IsDefined(plan))
            card.Plan = plan;

        card.CanvasJson = card.Plan == CardPlan.ProMax && r.Canvas is not null
            ? JsonSerializer.Serialize(CanvasRules.Sanitize(r.Canvas), CanvasRules.Json)
            : card.Plan == CardPlan.ProMax ? card.CanvasJson : null;
        var theme = card.Plan >= CardPlan.Media ? ThemeRules.Sanitize(r.Theme, allowed, card.Plan >= CardPlan.Pro) : new CardTheme();
        card.ThemeJson = JsonSerializer.Serialize(theme);
        if (card.Plan >= CardPlan.Pro)
        {
            card.GalleryJson = JsonSerializer.Serialize(CleanGallery(r.Gallery));
            card.VideoUrl = CleanVideo(r.VideoUrl);
        }
        else
        {
            card.GalleryJson = "[]";
            card.VideoUrl = null;
        }
        card.UpdatedAt = DateTime.UtcNow;
        return null;
    }

    private static bool IsActive(Card c) =>
        c.Status == CardStatus.Published && (c.ExpiresAt is null || c.ExpiresAt > DateTime.UtcNow);

    private static string[] CleanGallery(string[]? g) =>
        (g ?? []).Select(u => u.Trim())
            .Where(u => u.Length is > 0 and <= 500 && (u.StartsWith("/uploads/", StringComparison.Ordinal) && !u.Contains("..")
                || u.StartsWith("https://", StringComparison.Ordinal)))
            .Distinct().Take(8).ToArray();

    private static string? CleanVideo(string? v) =>
        v is { Length: > 0 and <= 300 } && Uri.TryCreate(v.Trim(), UriKind.Absolute, out var u) && u.Scheme == Uri.UriSchemeHttps
            ? u.AbsoluteUri : null;

    private static TemplateRenderer.Extras? BuildExtras(CardPlan plan, Template t, CardTheme? theme, string[]? gallery, string? video, TemplateRenderer.ArtPack? art)
    {
        var fields = t.FieldKeys.Split(',', StringSplitOptions.RemoveEmptyEntries);
        return new TemplateRenderer.Extras(
            plan >= CardPlan.Media ? ThemeRules.Sanitize(theme, fields, plan >= CardPlan.Pro) : null,
            plan >= CardPlan.Pro ? CleanGallery(gallery) : null,
            plan >= CardPlan.Pro ? CleanVideo(video) : null,
            Art: art);
    }

    private static CardDetail ToDetail(Card c) =>
        new(c.Id, c.Title, c.Slug, c.TemplateId, c.Status.ToString(),
            JsonSerializer.Deserialize<Dictionary<string, string>>(c.DataJson) ?? [], c.Plan.ToString(),
            JsonSerializer.Deserialize<CardTheme>(c.ThemeJson) ?? new CardTheme(),
            JsonSerializer.Deserialize<string[]>(c.GalleryJson) ?? [], c.VideoUrl, IsActive(c), c.Product,
            c.CanvasJson is null ? null : JsonSerializer.Deserialize<CanvasDoc>(c.CanvasJson, CanvasRules.Json));
}
