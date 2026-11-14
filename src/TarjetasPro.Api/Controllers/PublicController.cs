using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using TarjetasPro.Application.Cards;
using TarjetasPro.Application.Templates;
using TarjetasPro.Domain.Cards;
using TarjetasPro.Domain.Entities;
using TarjetasPro.Infrastructure;

namespace TarjetasPro.Api.Controllers;

/// <summary>Endpoints anónimos: solo exponen tarjetas con estado Publicada.</summary>
[ApiController]
[Route("api/public")]
[EnableRateLimiting("public")]
public class PublicController(AppDbContext db, IConfiguration config, IWebHostEnvironment env) : ControllerBase
{
    private string ApiBase => $"{Request.Scheme}://{Request.Host}";
    private string PublicUrl => (config["App:PublicUrl"] ?? ApiBase).TrimEnd('/');

    /// <summary>Cifras reales para el landing (solo conteos, nada personal).</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> Stats()
    {
        var cards = await db.Cards.CountAsync(c => c.Status == CardStatus.Published);
        var visits = await db.CardVisits.CountAsync();
        Response.Headers.CacheControl = "public, max-age=300";
        return Ok(new { cards, visits });
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Get(string slug)
    {
        var loaded = await Load(slug);
        if (loaded is null) return NotFound();
        var (card, data) = loaded.Value;

        if (card.CanvasJson is not null && card.Plan == CardPlan.ProMax)
        {
            var doc = JsonSerializer.Deserialize<TarjetasPro.Application.Canvas.CanvasDoc>(card.CanvasJson, TarjetasPro.Application.Canvas.CanvasRules.Json);
            return Ok(BuildPublic(card, data, TarjetasPro.Application.Canvas.CanvasRules.Render(doc!, ApiBase, false, $"{ApiBase}/api/public/{card.Slug}/go-c")));
        }
        var fields = card.Template!.FieldKeys.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var theme = card.Plan >= CardPlan.Media ? ThemeRules.Sanitize(JsonSerializer.Deserialize<CardTheme>(card.ThemeJson), fields, card.Plan >= CardPlan.Pro) : null;
        var extras = new TemplateRenderer.Extras(
            theme,
            card.Plan >= CardPlan.Pro ? JsonSerializer.Deserialize<string[]>(card.GalleryJson) : null,
            card.Plan >= CardPlan.Pro ? card.VideoUrl : null,
            card.Plan >= CardPlan.Pro ? $"{ApiBase}/api/public/{card.Slug}/go" : null,
            ArtCatalog.Compose(env, card.Template.Code, theme, ApiBase));
        var html = TemplateRenderer.Render(card.Template.Html, card.Template.Css, data, ApiBase, false, extras);
        return Ok(BuildPublic(card, data, html));
    }

    /// <summary>La cuenta una visita real (la llama la página, no los robots de vista previa).</summary>
    [HttpPost("{slug}/visit")]
    public async Task<IActionResult> Visit(string slug)
    {
        var normalized = SlugRules.Normalize(slug);
        var id = await db.Cards.Where(c => c.Slug == normalized && c.Status == CardStatus.Published && (c.ExpiresAt == null || c.ExpiresAt > DateTime.UtcNow) && db.Users.Any(u => u.Id == c.UserId && u.IsActive))
            .Select(c => (Guid?)c.Id).FirstOrDefaultAsync();
        if (id is null) return NotFound();
        db.CardVisits.Add(new CardVisit { CardId = id.Value });
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Cuenta el clic de un botón (solo planes Pro) y redirige al destino guardado en la tarjeta.</summary>
    [HttpGet("{slug}/go/{kind}")]
    public async Task<IActionResult> Go(string slug, string kind)
    {
        var loaded = await Load(slug);
        if (loaded is null) return NotFound();
        var (card, data) = loaded.Value;
        var target = TemplateRenderer.LinkTarget(kind, data);
        if (target is null) return NotFound();
        if (card.Plan >= CardPlan.Pro)
        {
            db.CardClicks.Add(new CardClick { CardId = card.Id, Kind = kind });
            await db.SaveChangesAsync();
        }
        return Redirect(target);
    }

    /// <summary>Clic en un botón del lienzo (Pro Max): se cuenta por tipo y se redirige al destino guardado en la tarjeta.</summary>
    [HttpGet("{slug}/go-c/{elId}")]
    public async Task<IActionResult> GoCanvas(string slug, string elId)
    {
        var loaded = await Load(slug);
        if (loaded is null) return NotFound();
        var (card, _) = loaded.Value;
        if (card.CanvasJson is null || card.Plan != CardPlan.ProMax) return NotFound();
        var doc = JsonSerializer.Deserialize<TarjetasPro.Application.Canvas.CanvasDoc>(card.CanvasJson, TarjetasPro.Application.Canvas.CanvasRules.Json);
        var t = doc is null ? null : TarjetasPro.Application.Canvas.CanvasRules.ButtonTarget(doc, elId);
        if (t is null) return NotFound();
        db.CardClicks.Add(new CardClick { CardId = card.Id, Kind = t.Value.Kind });
        await db.SaveChangesAsync();
        return Redirect(t.Value.Target);
    }

    [HttpGet("{slug}/vcard")]
    public async Task<IActionResult> VCard(string slug)
    {
        var loaded = await Load(slug);
        if (loaded is null) return NotFound();
        var (card, data) = loaded.Value;
        var bytes = System.Text.Encoding.UTF8.GetBytes(VCardBuilder.Build(data));
        return File(bytes, "text/vcard; charset=utf-8", $"{card.Slug}.vcf");
    }

    [HttpGet("{slug}/qr")]
    public async Task<IActionResult> Qr(string slug, bool download = false)
    {
        var normalized = SlugRules.Normalize(slug);
        if (!await db.Cards.AnyAsync(c => c.Slug == normalized && c.Status == CardStatus.Published && (c.ExpiresAt == null || c.ExpiresAt > DateTime.UtcNow) && db.Users.Any(u => u.Id == c.UserId && u.IsActive)))
            return NotFound();

        using var generator = new QRCodeGenerator();
        using var qr = generator.CreateQrCode($"{PublicUrl}/{normalized}", QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(qr).GetGraphic(12);
        return download ? File(png, "image/png", $"qr-{normalized}.png") : File(png, "image/png");
    }

    /// <summary>
    /// HTML mínimo con etiquetas Open Graph para que WhatsApp/Facebook/etc. muestren
    /// nombre y foto al compartir. Las personas son redirigidas a la tarjeta real.
    /// </summary>
    [HttpGet("{slug}/share")]
    public async Task<IActionResult> Share(string slug)
    {
        var loaded = await Load(slug);
        if (loaded is null) return NotFound();
        var (card, data) = loaded.Value;
        var meta = BuildPublic(card, data, "");
        var url = $"{PublicUrl}/{card.Slug}";

        static string E(string? s) => WebUtility.HtmlEncode(s ?? "");
        var image = meta.ImageUrl is null ? "" : $"""<meta property="og:image" content="{E(meta.ImageUrl)}">""";
        var html = $"""
            <!doctype html><html lang="es"><head><meta charset="utf-8">
            <title>{E(meta.Title)}</title>
            <meta name="description" content="{E(meta.Description)}">
            <meta property="og:type" content="profile">
            <meta property="og:title" content="{E(meta.Title)}">
            <meta property="og:description" content="{E(meta.Description)}">
            <meta property="og:url" content="{E(url)}">
            {image}
            <meta name="twitter:card" content="summary">
            <meta http-equiv="refresh" content="0;url={E(url)}">
            </head><body><a href="{E(url)}">{E(meta.Title)}</a></body></html>
            """;
        return Content(html, "text/html; charset=utf-8");
    }

    private async Task<(Card Card, Dictionary<string, string> Data)?> Load(string slug)
    {
        var normalized = SlugRules.Normalize(slug);
        var card = await db.Cards.AsNoTracking().Include(c => c.Template)
            .FirstOrDefaultAsync(c => c.Slug == normalized && c.Status == CardStatus.Published && (c.ExpiresAt == null || c.ExpiresAt > DateTime.UtcNow) && db.Users.Any(u => u.Id == c.UserId && u.IsActive));
        if (card?.Template is null) return null;
        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(card.DataJson) ?? [];
        return (card, data);
    }

    private PublicCard BuildPublic(Card card, Dictionary<string, string> data, string html)
    {
        string Get(string k) => data.TryGetValue(k, out var v) ? v.Trim() : "";
        var name = Get("nombre") is { Length: > 0 } n ? n : card.Title;
        var role = string.Join(" · ", new[] { Get("puesto"), Get("empresa") }.Where(x => x.Length > 0));
        var title = role.Length > 0 ? $"{name} — {role}" : name;
        var description = Get("descripcion") is { Length: > 0 } d ? d : role.Length > 0 ? role : $"Tarjeta de presentación de {name}";
        if (description.Length > 200) description = description[..200] + "…";

        string? image = null;
        var raw = Get("foto") is { Length: > 0 } f ? f : Get("logo");
        if (raw.StartsWith("/uploads/", StringComparison.Ordinal) && !raw.Contains("..")) image = ApiBase + raw;
        else if (Uri.TryCreate(raw, UriKind.Absolute, out var u) && u.Scheme is "http" or "https") image = u.AbsoluteUri;

        return new PublicCard(card.Slug, html, name, title, description, image);
    }
}
