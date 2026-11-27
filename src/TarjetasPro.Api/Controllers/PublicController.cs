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

public record VisitRequest(string? Referrer, string? Src);
public record SiteVisitRequest(string? Path, string? Referrer, string? Src);
public record PresenceRequest(string? Tab, string? Path, bool Leave = false);

/// <summary>Endpoints anónimos: solo exponen tarjetas con estado Publicada.</summary>
[ApiController]
[Route("api/public")]
[EnableRateLimiting("public")]
public class PublicController(AppDbContext db, IConfiguration config, IWebHostEnvironment env, TarjetasPro.Infrastructure.Analytics.GeoIpService geo) : ControllerBase
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
            ArtCatalog.Compose(env, card.Template.Code, theme, ApiBase),
            Embed: true, Detail: card.Plan >= CardPlan.Pro);
        var html = TemplateRenderer.Render(card.Template.Html, card.Template.Css, data, ApiBase, false, extras);
        return Ok(BuildPublic(card, data, html));
    }

    /// <summary>La cuenta una visita real (la llama la página, no los robots de vista previa).</summary>
    [HttpPost("{slug}/visit")]
    public async Task<IActionResult> Visit(string slug, [FromBody] VisitRequest? body)
    {
        var normalized = SlugRules.Normalize(slug);
        var id = await db.Cards.Where(c => c.Slug == normalized && c.Status == CardStatus.Published && (c.ExpiresAt == null || c.ExpiresAt > DateTime.UtcNow) && db.Users.Any(u => u.Id == c.UserId && u.IsActive))
            .Select(c => (Guid?)c.Id).FirstOrDefaultAsync();
        if (id is null) return NotFound();
        var ua = Request.Headers.UserAgent.ToString();
        var ip = TarjetasPro.Infrastructure.Analytics.VisitInfo.CleanIp(HttpContext.Connection.RemoteIpAddress);
        var visit = new CardVisit
        {
            CardId = id.Value,
            Ip = ip is { Length: <= 45 } ? ip : null,
            Source = TarjetasPro.Infrastructure.Analytics.VisitInfo.Source(body?.Src, body?.Referrer, ua),
            Device = TarjetasPro.Infrastructure.Analytics.VisitInfo.Device(ua),
        };
        db.CardVisits.Add(visit);
        await db.SaveChangesAsync();
        geo.Enqueue(visit.Id, visit.Ip);
        return NoContent();
    }

    /// <summary>Páginas de KARSHA que se cuentan (las tarjetas públicas van por /{slug}/visit).</summary>
    private static readonly HashSet<string> SitePages = ["/", "/tarjetas-digitales", "/invitaciones-digitales", "/menu", "/registro", "/login", "/terminos", "/privacidad"];

    /// <summary>Cuenta una visita a karsha.mx (inicio, landings, registro) para el admin.</summary>
    [HttpPost("site-visit")]
    public async Task<IActionResult> SiteVisit([FromBody] SiteVisitRequest? body)
    {
        var ua = Request.Headers.UserAgent.ToString();
        var path = TarjetasPro.Infrastructure.Analytics.VisitInfo.CleanPath(body?.Path);
        if (!SitePages.Contains(path) || TarjetasPro.Infrastructure.Analytics.VisitInfo.IsBot(ua)) return NoContent();
        var ip = TarjetasPro.Infrastructure.Analytics.VisitInfo.CleanIp(HttpContext.Connection.RemoteIpAddress);
        var visit = new SiteVisit
        {
            Path = path,
            Ip = ip is { Length: <= 45 } ? ip : null,
            Source = TarjetasPro.Infrastructure.Analytics.VisitInfo.Source(body?.Src, body?.Referrer, ua),
            Device = TarjetasPro.Infrastructure.Analytics.VisitInfo.Device(ua),
        };
        db.SiteVisits.Add(visit);
        await db.SaveChangesAsync();
        geo.Enqueue(visit.Id, visit.Ip, site: true);
        return NoContent();
    }

    /// <summary>
    /// «Sigo aquí»: cada pestaña abierta avisa cada 30 s. Solo vive en memoria (nada se guarda en la base)
    /// y sirve para que el admin vea cuánta gente está navegando antes de publicar.
    /// </summary>
    [HttpPost("presence")]
    public IActionResult Presence([FromBody] PresenceRequest? body, [FromServices] TarjetasPro.Api.Presence.PresenceTracker presence)
    {
        var tab = body?.Tab?.Trim();
        if (string.IsNullOrEmpty(tab) || tab.Length > 64 || TarjetasPro.Infrastructure.Analytics.VisitInfo.IsBot(Request.Headers.UserAgent.ToString())) return NoContent();
        if (body!.Leave) { presence.Leave(tab); return NoContent(); }
        var userId = User.Identity?.IsAuthenticated == true ? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value : null;
        presence.Ping(tab, PresencePath(body.Path), userId);
        return NoContent();
    }

    /// <summary>Ruta legible y sin identificadores: /tarjetas/3f2a… → /tarjetas/…</summary>
    private static string PresencePath(string? raw)
    {
        var parts = TarjetasPro.Infrastructure.Analytics.VisitInfo.CleanPath(raw).Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => Guid.TryParse(s, out _) || s.All(char.IsDigit) ? "…" : s);
        return "/" + string.Join('/', parts);
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
    public async Task<IActionResult> Qr(string slug, bool download = false, string? fg = null, string? bg = null, bool logo = false)
    {
        var normalized = SlugRules.Normalize(slug);
        if (!await db.Cards.AnyAsync(c => c.Slug == normalized && c.Status == CardStatus.Published && (c.ExpiresAt == null || c.ExpiresAt > DateTime.UtcNow) && db.Users.Any(u => u.Id == c.UserId && u.IsActive)))
            return NotFound();

        // Colores del diseño del cliente: solo #rrggbb y con contraste suficiente para que el QR siga leyéndose.
        static byte[]? Rgb(string? hex)
        {
            if (hex is null || !System.Text.RegularExpressions.Regex.IsMatch(hex, "^#?[0-9a-fA-F]{6}$")) return null;
            hex = hex.TrimStart('#');
            return [Convert.ToByte(hex[..2], 16), Convert.ToByte(hex[2..4], 16), Convert.ToByte(hex[4..], 16)];
        }
        static double Lum(byte[] c) => (0.2126 * c[0] + 0.7152 * c[1] + 0.0722 * c[2]) / 255.0;
        var dark = Rgb(fg) ?? [0, 0, 0]; var light = Rgb(bg) ?? [255, 255, 255];
        if (Math.Abs(Lum(dark) - Lum(light)) < 0.45) { dark = [0, 0, 0]; light = [255, 255, 255]; }

        using var generator = new QRCodeGenerator();
        using var qr = generator.CreateQrCode($"{PublicUrl}/{normalized}?src=qr", QRCodeGenerator.ECCLevel.H);

        if (logo)
        {
            // El logo va en el centro (ECC alto), tomado de la imagen principal de la tarjeta si es una foto subida por el cliente.
            var loaded = await Load(slug);
            var img = loaded is null ? null : BuildPublic(loaded.Value.Item1, loaded.Value.Item2, "").ImageUrl;
            var name = img is not null && img.Contains("/uploads/") ? Path.GetFileName(img.Split('?')[0]) : null;
            var path = name is null ? null : Path.Combine(env.WebRootPath ?? "", "uploads", name);
            if (path is not null && System.IO.File.Exists(path) && new FileInfo(path).Length <= 1_000_000)
            {
                var svg = new SvgQRCode(qr).GetGraphic(10, "#" + Convert.ToHexString(dark), "#" + Convert.ToHexString(light), true,
                    SvgQRCode.SizingMode.WidthHeightAttribute, new SvgQRCode.SvgLogo(System.IO.File.ReadAllBytes(path), 20));
                var bytes = System.Text.Encoding.UTF8.GetBytes(svg);
                return download ? File(bytes, "image/svg+xml", $"qr-{normalized}.svg") : File(bytes, "image/svg+xml");
            }
        }
        var png = new PngByteQRCode(qr).GetGraphic(12, dark, light);
        return download ? File(png, "image/png", $"qr-{normalized}.png") : File(png, "image/png");
    }

    /// <summary>Enlace corto para compartir: t.karsha.mx/{slug} (solo responde en ese dominio).</summary>
    [HttpGet("/{slug}")]
    [Host("t.karsha.mx", "t.localhost:5080")]
    public Task<IActionResult> ShareShort(string slug) => Share(slug);

    /// <summary>
    /// HTML mínimo con etiquetas Open Graph para que WhatsApp/Facebook/etc. muestren
    /// nombre y foto al compartir. Las personas son redirigidas a la tarjeta real.
    /// </summary>
    [HttpGet("{slug}/share")]
    [HttpGet("/t/{slug}")]
    public async Task<IActionResult> Share(string slug)
    {
        var loaded = await Load(slug);
        // Si la tarjeta ya no está publicada, la persona igual llega a la página (que muestra «no disponible»).
        if (loaded is null) return Redirect($"{PublicUrl}/{Uri.EscapeDataString(SlugRules.Normalize(slug))}");
        var (card, data) = loaded.Value;
        var meta = BuildPublic(card, data, "");
        var url = $"{PublicUrl}/{card.Slug}";
        // WhatsApp solo muestra imágenes por https y livianas: las fotos subidas van reducidas (og.jpg); sin foto ni logo, el de KARSHA.
        var image = meta.ImageUrl is null ? $"{ApiBase}/gfx/karsha-og.png"
            : meta.ImageUrl.StartsWith(ApiBase + "/uploads/", StringComparison.Ordinal)
                ? $"{ApiBase}/api/public/{card.Slug}/og.jpg?v={Convert.ToHexString(System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(meta.ImageUrl)))[..10]}"
                : meta.ImageUrl;
        if (image.StartsWith("http://", StringComparison.Ordinal) && !Request.Host.Host.Contains("localhost")) image = "https://" + image[7..];

        static string E(string? s) => WebUtility.HtmlEncode(s ?? "");
        var html = $"""
            <!doctype html><html lang="es" translate="no"><head><meta charset="utf-8"><meta name="google" content="notranslate">
            <title>{E(meta.Title)}</title>
            <meta name="description" content="{E(meta.Description)}">
            <meta property="og:type" content="profile">
            <meta property="og:site_name" content="KARSHA">
            <meta property="og:locale" content="es_MX">
            <meta property="og:title" content="{E(meta.Title)}">
            <meta property="og:description" content="{E(meta.Description)}">
            <meta property="og:url" content="{E(url)}">
            <meta property="og:image" content="{E(image)}">
            <meta property="og:image:secure_url" content="{E(image)}">
            <meta property="og:image:alt" content="{E(meta.Name)}">
            <meta name="twitter:card" content="summary">
            <meta name="twitter:image" content="{E(image)}">
            <link rel="canonical" href="{E(url)}">
            <meta http-equiv="refresh" content="0;url={E(url)}">
            </head><body><a href="{E(url)}">{E(meta.Title)}</a><script>location.replace({JsonSerializer.Serialize(url)})</script></body></html>
            """;
        Response.Headers.CacheControl = "public, max-age=600";
        return Content(html, "text/html; charset=utf-8");
    }

    /// <summary>Foto de la tarjeta reducida a JPG (máx. 600 px) para la vista previa de WhatsApp, que ignora imágenes pesadas.</summary>
    [HttpGet("{slug}/og.jpg")]
    public async Task<IActionResult> OgImage(string slug, [FromServices] TarjetasPro.Infrastructure.Storage.DriveLinkService drive, CancellationToken ct)
    {
        var loaded = await Load(slug);
        var src = loaded is null ? null : BuildPublic(loaded.Value.Card, loaded.Value.Data, "").ImageUrl;
        var name = src is not null && src.StartsWith(ApiBase + "/uploads/", StringComparison.Ordinal) ? Path.GetFileName(src.Split('?')[0]) : null;
        if (name is null) return Redirect($"{ApiBase}/gfx/karsha-og.png");

        byte[]? bytes = null;
        var m = System.Text.RegularExpressions.Regex.Match(name, @"^drive-([\w-]{10,100})\.(jpg|png|webp)$");
        if (m.Success)
        {
            var file = await drive.OpenAsync(m.Groups[1].Value, ct);
            if (file is not null) { await using var s = file.Value.Stream; using var ms = new MemoryStream(); await s.CopyToAsync(ms, ct); bytes = ms.ToArray(); }
        }
        else
        {
            var path = Path.Combine(env.WebRootPath ?? "", "uploads", name);
            if (System.IO.File.Exists(path)) bytes = await System.IO.File.ReadAllBytesAsync(path, ct);
        }
        if (bytes is null || bytes.Length > 15_000_000) return Redirect(src!);

        using var original = SkiaSharp.SKBitmap.Decode(bytes);
        if (original is null) return Redirect(src!);
        var scale = Math.Min(1.0, 600.0 / Math.Max(original.Width, original.Height));
        var info = new SkiaSharp.SKImageInfo(Math.Max(1, (int)(original.Width * scale)), Math.Max(1, (int)(original.Height * scale)));
        using var surface = SkiaSharp.SKSurface.Create(info);
        surface.Canvas.Clear(SkiaSharp.SKColors.White); // JPG no tiene transparencia: fondo blanco
        using (var img = SkiaSharp.SKImage.FromBitmap(original))
            surface.Canvas.DrawImage(img, new SkiaSharp.SKRect(0, 0, info.Width, info.Height), new SkiaSharp.SKSamplingOptions(SkiaSharp.SKCubicResampler.Mitchell));
        using var snap = surface.Snapshot();
        using var jpg = snap.Encode(SkiaSharp.SKEncodedImageFormat.Jpeg, 82);
        Response.Headers.CacheControl = "public, max-age=86400";
        return File(jpg.ToArray(), "image/jpeg");
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
        var canvas = CanvasPreview(card, name);
        if (role.Length == 0 && canvas.Role is not null) role = canvas.Role;
        var title = role.Length > 0 ? $"{name} — {role}" : name;
        var description = Get("descripcion") is { Length: > 0 } d ? d : role.Length > 0 ? role : $"Tarjeta de presentación de {name}";
        if (description.Length > 200) description = description[..200] + "…";

        string? image = null;
        var raw = Get("foto") is { Length: > 0 } f ? f : Get("logo") is { Length: > 0 } l ? l : canvas.Image ?? "";
        if (raw.StartsWith("/uploads/", StringComparison.Ordinal) && !raw.Contains("..")) image = ApiBase + raw;
        else if (Uri.TryCreate(raw, UriKind.Absolute, out var u) && u.Scheme is "http" or "https") image = u.AbsoluteUri;

        return new PublicCard(card.Slug, html, name, title, description, image);
    }

    /// <summary>
    /// Tarjetas de lienzo (Pro Max): la foto y el puesto viven en el diseño, no en los campos.
    /// Imagen = la más grande que se ve (o el fondo); puesto = el primer texto corto bajo el nombre.
    /// </summary>
    private static (string? Image, string? Role) CanvasPreview(Card card, string name)
    {
        if (card.CanvasJson is null || card.Plan != CardPlan.ProMax) return (null, null);
        TarjetasPro.Application.Canvas.CanvasDoc? doc;
        try { doc = JsonSerializer.Deserialize<TarjetasPro.Application.Canvas.CanvasDoc>(card.CanvasJson, TarjetasPro.Application.Canvas.CanvasRules.Json); }
        catch (JsonException) { return (null, null); }
        if (doc is null) return (null, null);

        var els = doc.Els.Where(e => !e.Hidden && e.Opacity > 0.2).ToList();
        var image = els.Where(e => e.Type == "image" && !string.IsNullOrWhiteSpace(e.Src))
            .OrderByDescending(e => e.Src!.Contains("/uploads/")) // primero las fotos que subió el cliente, no los adornos
            .ThenByDescending(e => e.W * e.H).Select(e => e.Src).FirstOrDefault() ?? doc.BgImage;

        static string Plain(string? s) => WebUtility.HtmlDecode(System.Text.RegularExpressions.Regex.Replace(s ?? "", "<[^>]+>", " ")).Trim();
        var texts = els.Where(e => e.Type == "text").OrderBy(e => e.Y).Select(e => Plain(e.Text)).Where(t => t.Length > 0).ToList();
        var nameIdx = texts.FindIndex(t => t.Contains(name, StringComparison.OrdinalIgnoreCase));
        var role = texts.Skip(nameIdx + 1).FirstOrDefault(t => t.Length is >= 3 and <= 80 && !t.Contains(name, StringComparison.OrdinalIgnoreCase));
        return (image, role is null ? null : System.Text.RegularExpressions.Regex.Replace(role, @"\s+", " "));
    }
}
