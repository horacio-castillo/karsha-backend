using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TarjetasPro.Application.Common;
using TarjetasPro.Application.Templates;
using TarjetasPro.Domain.Cards;
using TarjetasPro.Domain.Entities;
using TarjetasPro.Domain.Security;
using TarjetasPro.Api.Security;
using TarjetasPro.Infrastructure;
using TarjetasPro.Infrastructure.Auth;

namespace TarjetasPro.Api.Controllers;

[ApiController]
[Route("api/templates")]
[Authorize]
public class TemplatesController(AppDbContext db, IWebHostEnvironment env) : ControllerBase
{
    private bool IsAdmin => User.HasClaim(Permissions.ClaimType, Permissions.TemplatesManage);

    [HttpGet("categories")]
    public async Task<IActionResult> Categories() =>
        Ok(await db.TemplateCategories.AsNoTracking().OrderBy(c => c.SortOrder).ThenBy(c => c.Name).Select(c => c.Name).ToListAsync());

    [HttpGet("fields")]
    public IActionResult Fields() =>
        Ok(CardFields.All.Select(f => new { f.Key, f.Label, Type = f.Type.ToString() }));

    /// <summary>Piezas de arte (arriba, abajo, mascota) de cada pack, para armar una plantilla propia.</summary>
    [HttpGet("art")]
    public IActionResult Art() => Ok(ArtCatalog.Packs(env, $"{Request.Scheme}://{Request.Host}").ToList());

    /// <summary>Clientes ven solo las activas; el Admin ve todas.</summary>
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var query = db.Templates.AsNoTracking();
        if (!IsAdmin) query = query.Where(t => t.IsActive);
        var list = await query.OrderBy(t => t.Category).ThenBy(t => t.Profession).ThenBy(t => t.Name).ToListAsync();
        return Ok(list.Select(Summary));
    }

    /// <summary>Listado paginado para el panel del Admin, con búsqueda por nombre, código, giro o profesión.</summary>
    [HttpGet("paged")]
    [Authorize(Policy = Permissions.TemplatesManage)]
    public async Task<IActionResult> ListPaged(string? search, int? page, int? pageSize)
    {
        var (pg, size) = Paged<object>.Normalize(page, pageSize ?? 12);
        var q = db.Templates.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(t => t.Name.Contains(s) || t.Code.Contains(s) || t.Category.Contains(s) || t.Profession.Contains(s));
        }
        var total = await q.CountAsync();
        var list = await q.OrderBy(t => t.Category).ThenBy(t => t.Profession).ThenBy(t => t.Name).ThenBy(t => t.Id)
            .Skip((pg - 1) * size).Take(size).ToListAsync();
        return Ok(new Paged<object>(list.Select(t => (object)Summary(t)).ToList(), total, pg, size));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.TemplatesManage)]
    public async Task<IActionResult> Get(Guid id)
    {
        var t = await db.Templates.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return t is null ? NotFound() : Ok(Detail(t));
    }

    /// <summary>Vista previa de una plantilla guardada con datos de ejemplo.</summary>
    [HttpGet("{id:guid}/preview")]
    public async Task<IActionResult> PreviewSaved(Guid id)
    {
        var t = await db.Templates.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (t is null || (!t.IsActive && !IsAdmin)) return NotFound();
        var apiBase = $"{Request.Scheme}://{Request.Host}";
        var extras = new TemplateRenderer.Extras(Art: ArtCatalog.Find(env, t.Code, apiBase));
        return Ok(new { html = TemplateRenderer.Render(t.Html, t.Css, CardFields.Sample, apiBase, false, extras) });
    }

    /// <summary>Vistas previas de varias plantillas guardadas en una sola petición (panel del Admin).</summary>
    [HttpGet("previews")]
    [Authorize(Policy = Permissions.TemplatesManage)]
    public async Task<IActionResult> PreviewsBatch([FromQuery] string ids)
    {
        var wanted = (ids ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
            .Where(g => g != Guid.Empty).Distinct().Take(48).ToList();
        var items = await db.Templates.AsNoTracking().Where(t => wanted.Contains(t.Id)).ToListAsync();
        var apiBase = $"{Request.Scheme}://{Request.Host}";
        var result = items.ToDictionary(t => t.Id, t =>
            TemplateRenderer.Render(t.Html, t.Css, CardFields.Sample, apiBase, false,
                new TemplateRenderer.Extras(Art: ArtCatalog.Find(env, t.Code, apiBase))));
        Response.Headers.CacheControl = "private, max-age=120";
        return Ok(result);
    }

    /// <summary>Solo en Development: códigos de plantilla para que tools/make-thumbs.sh genere las miniaturas.</summary>
    [HttpGet("dev-render")]
    [AllowAnonymous]
    public async Task<IActionResult> DevRenderList() =>
        !env.IsDevelopment() ? NotFound() : Ok(await db.Templates.AsNoTracking().Select(t => t.Code).ToListAsync());

    /// <summary>Solo en Development: HTML de una plantilla con datos de ejemplo, para capturarlo con Chrome.</summary>
    [HttpGet("dev-render/{code}")]
    [AllowAnonymous]
    public async Task<IActionResult> DevRender(string code, bool wrap = false)
    {
        if (!env.IsDevelopment()) return NotFound();
        // Chrome headless no baja de ~500px de ancho: se encuadra la tarjeta en un iframe de 390px.
        if (wrap)
            return Content($"<!doctype html><body style=\"margin:0;background:#fff\"><iframe src=\"/api/templates/dev-render/{Uri.EscapeDataString(code)}\" style=\"display:block;margin:0 auto;width:390px;height:780px;border:0\"></iframe>", "text/html; charset=utf-8");
        var t = await db.Templates.AsNoTracking().FirstOrDefaultAsync(x => x.Code == code);
        if (t is null) return NotFound();
        var apiBase = $"{Request.Scheme}://{Request.Host}";
        var html = TemplateRenderer.Render(t.Html, t.Css, CardFields.Sample, apiBase, false,
            new TemplateRenderer.Extras(Art: ArtCatalog.Find(env, t.Code, apiBase)));
        return Content(html, "text/html; charset=utf-8");
    }

    /// <summary>Vista previa en vivo del editor (borrador sin guardar).</summary>
    [HttpPost("preview")]
    [Authorize(Policy = Permissions.TemplatesManage)]
    public IActionResult Preview(PreviewRequest r) =>
        Ok(new PreviewResponse(
            TemplateRenderer.Render(r.Html ?? "", r.Css ?? "", r.Data is { Count: > 0 } ? r.Data : CardFields.Sample),
            TemplateRenderer.ExtractFields(r.Html ?? ""),
            TemplateRenderer.UnknownFields(r.Html ?? "")));

    /// <summary>Imágenes de diseño (fondos, adornos) que el Admin usa dentro del HTML/CSS de una plantilla.</summary>
    [HttpPost("assets")]
    [Authorize(Policy = Permissions.TemplatesManage)]
    [RequestSizeLimit(4 * 1024 * 1024 + 64 * 1024)]
    public async Task<IActionResult> UploadAsset(IFormFile file)
    {
        var saved = await ImageSniffer.SaveAsync(file, Path.Combine(env.WebRootPath, "uploads", "templates"), 4 * 1024 * 1024);
        if (saved.Error is not null) return BadRequest(new { errors = new[] { saved.Error } });
        return Ok(new { url = $"{Request.Scheme}://{Request.Host}/uploads/templates/{saved.FileName}" });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.TemplatesManage)]
    public async Task<IActionResult> Create(TemplateRequest r)
    {
        if (await Validate(r) is { } error) return error;
        if (await db.Templates.AnyAsync(t => t.Code == r.Code))
            return BadRequest(new { errors = new[] { "Ya existe una plantilla con ese código." } });

        var t = new Template();
        Apply(t, r);
        db.Templates.Add(t);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = t.Id }, Detail(t));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.TemplatesManage)]
    public async Task<IActionResult> Update(Guid id, TemplateRequest r)
    {
        var t = await db.Templates.FirstOrDefaultAsync(x => x.Id == id);
        if (t is null) return NotFound();
        if (await Validate(r) is { } error) return error;
        if (await db.Templates.AnyAsync(x => x.Code == r.Code && x.Id != id))
            return BadRequest(new { errors = new[] { "Ya existe una plantilla con ese código." } });

        Apply(t, r);
        await db.SaveChangesAsync();
        return Ok(Detail(t));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.TemplatesManage)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var t = await db.Templates.FirstOrDefaultAsync(x => x.Id == id);
        if (t is null) return NotFound();
        if (await db.Cards.AnyAsync(c => c.TemplateId == id))
            return BadRequest(new { errors = new[] { "Hay tarjetas usando esta plantilla. Desactívala en lugar de borrarla." } });
        db.Templates.Remove(t);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<IActionResult?> Validate(TemplateRequest r)
    {
        if (!await db.TemplateCategories.AnyAsync(c => c.Name == r.Category))
            return BadRequest(new { errors = new[] { "Categoría no válida." } });
        var unknown = TemplateRenderer.UnknownFields(r.Html);
        return unknown.Length == 0
            ? null
            : BadRequest(new { errors = new[] { "Campos desconocidos en el HTML: " + string.Join(", ", unknown) } });
    }

    private static void Apply(Template t, TemplateRequest r)
    {
        t.Name = r.Name.Trim();
        t.Code = r.Code;
        t.Description = r.Description?.Trim() ?? "";
        t.Category = r.Category;
        t.Profession = r.Profession?.Trim() ?? "";
        t.Html = r.Html;
        t.Css = r.Css ?? "";
        t.IsActive = r.IsActive;
        t.FieldKeys = string.Join(',', TemplateRenderer.ExtractFields(r.Html));
        t.UpdatedAt = DateTime.UtcNow;
    }

    private static string[] Keys(Template t) =>
        t.FieldKeys.Split(',', StringSplitOptions.RemoveEmptyEntries);

    private static TemplateSummary Summary(Template t) =>
        new(t.Id, t.Name, t.Code, t.Description, t.Category, t.Profession, Keys(t), t.IsActive, ThemeRules.Detect(t.Css), TemplateRenderer.SectionKeys(t.Html), TemplateCategories.KindOf(t.Category), ProfessionThemes.IconsFor(t.Code));

    private static TemplateDetail Detail(Template t) =>
        new(t.Id, t.Name, t.Code, t.Description, t.Category, t.Profession, t.Html, t.Css, Keys(t), t.IsActive);
}
