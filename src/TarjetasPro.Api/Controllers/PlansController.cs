using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TarjetasPro.Domain.Entities;
using TarjetasPro.Domain.Security;
using TarjetasPro.Infrastructure;

namespace TarjetasPro.Api.Controllers;

public record ProductRequest(string? Key, string? Name, string? Description, bool IsActive, int SortOrder);
public record PlanRequest(string? Product, string? Code, string? Name, string? Tagline, int PriceCents, string[]? Features, bool IsHighlighted, bool IsActive, int SortOrder);

/// <summary>Planes y precios por producto, administrados desde el panel.</summary>
[ApiController]
[Route("api/admin/plans")]
[Authorize(Policy = Permissions.CardsManageAll)]
public partial class PlansController(AppDbContext db) : ControllerBase
{
    [GeneratedRegex("^[a-z0-9-]{2,30}$")] private static partial Regex CodeRx();

    private static object Row(PlanSetting p, string productName) => new
    {
        p.Id, p.Product, ProductName = productName, p.Code,
        Plan = p.Plan?.ToString(), p.Name, p.Tagline, p.PriceCents, Features = p.FeatureList(),
        p.IsHighlighted, p.IsActive, p.SortOrder, p.UpdatedAt,
        // Los planes ligados a un nivel del sistema (Sencilla, Media, Pro) se editan, no se borran.
        CanDelete = p.Plan is null,
    };

    private async Task<string> NameOf(string product) =>
        await db.ProductSettings.Where(x => x.Key == product).Select(x => x.Name).FirstOrDefaultAsync() ?? product;

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var products = await db.ProductSettings.AsNoTracking().OrderBy(p => p.SortOrder).ThenBy(p => p.Name).ToListAsync();
        var names = products.ToDictionary(p => p.Key, p => p.Name);
        var list = await db.PlanSettings.AsNoTracking().OrderBy(p => p.Product).ThenBy(p => p.SortOrder).ToListAsync();
        return Ok(new
        {
            Products = products.Select(p => new { p.Key, p.Name, p.Description, p.IsActive, p.SortOrder, IsSystem = p.Key == Products.Cards, Plans = list.Count(x => x.Product == p.Key) }),
            Plans = list.Select(x => Row(x, names.GetValueOrDefault(x.Product, x.Product))),
        });
    }

    [HttpPost("products")]
    public async Task<IActionResult> CreateProduct(ProductRequest r)
    {
        var key = (r.Key ?? "").Trim().ToLowerInvariant();
        if (!CodeRx().IsMatch(key)) return Bad("La clave debe tener de 2 a 30 letras minúsculas, números o guiones.");
        if (await db.ProductSettings.AnyAsync(p => p.Key == key)) return Bad("Ya existe un producto con esa clave.");
        var p = new ProductSetting { Key = key };
        if (ApplyProduct(p, r) is { } error) return error;
        db.ProductSettings.Add(p);
        await db.SaveChangesAsync();
        return Ok(new { p.Key, p.Name });
    }

    [HttpPut("products/{key}")]
    public async Task<IActionResult> UpdateProduct(string key, ProductRequest r)
    {
        var p = await db.ProductSettings.FirstOrDefaultAsync(x => x.Key == key);
        if (p is null) return NotFound();
        if (key == Products.Cards && !r.IsActive) return Bad("Tarjetas digitales no se puede apagar: es el producto principal.");
        if (ApplyProduct(p, r) is { } error) return error;
        await db.SaveChangesAsync();
        return Ok(new { p.Key, p.Name });
    }

    [HttpDelete("products/{key}")]
    public async Task<IActionResult> DeleteProduct(string key)
    {
        var p = await db.ProductSettings.FirstOrDefaultAsync(x => x.Key == key);
        if (p is null) return NotFound();
        if (key == Products.Cards) return Bad("Tarjetas digitales no se puede borrar.");
        if (await db.PlanSettings.AnyAsync(x => x.Product == key)) return Bad("Borra primero los planes de este producto.");
        db.ProductSettings.Remove(p);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private IActionResult? ApplyProduct(ProductSetting p, ProductRequest r)
    {
        var name = (r.Name ?? "").Trim();
        if (name.Length is < 2 or > 60) return Bad("El nombre debe tener entre 2 y 60 caracteres.");
        if ((r.Description ?? "").Trim().Length > 200) return Bad("La descripción admite hasta 200 caracteres.");
        p.Name = name;
        p.Description = (r.Description ?? "").Trim();
        p.IsActive = r.IsActive;
        p.SortOrder = r.SortOrder;
        return null;
    }

    [HttpPost]
    public async Task<IActionResult> Create(PlanRequest r)
    {
        var product = (r.Product ?? "").Trim();
        if (!await db.ProductSettings.AnyAsync(x => x.Key == product)) return Bad("Producto no válido.");
        if (Products.IsCardLike(product)) return Bad("Los planes de este producto son fijos (Sencilla, Media y Pro); edítalos en la lista.");
        var code = (r.Code ?? "").Trim().ToLowerInvariant();
        if (!CodeRx().IsMatch(code)) return Bad("El código debe tener de 2 a 30 letras minúsculas, números o guiones.");
        if (await db.PlanSettings.AnyAsync(p => p.Product == product && p.Code == code)) return Bad("Ya existe un plan con ese código en este producto.");
        var p = new PlanSetting { Product = product, Code = code };
        if (Apply(p, r) is { } error) return error;
        db.PlanSettings.Add(p);
        await db.SaveChangesAsync();
        return Ok(Row(p, await NameOf(p.Product)));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, PlanRequest r)
    {
        var p = await db.PlanSettings.FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();
        if (Apply(p, r) is { } error) return error;
        await db.SaveChangesAsync();
        return Ok(Row(p, await NameOf(p.Product)));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var p = await db.PlanSettings.FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();
        if (p.Plan is not null) return Bad("Los planes de Tarjetas no se pueden borrar. Apágalo para dejar de ofrecerlo.");
        db.PlanSettings.Remove(p);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private IActionResult? Apply(PlanSetting p, PlanRequest r)
    {
        var name = (r.Name ?? "").Trim();
        if (name.Length is < 2 or > 40) return Bad("El nombre debe tener entre 2 y 40 caracteres.");
        if (r.PriceCents is < PlanSetting.MinPriceCents or > PlanSetting.MaxPriceCents)
            return Bad($"El precio debe estar entre ${PlanSetting.MinPriceCents / 100} y ${PlanSetting.MaxPriceCents / 100:N0}.");
        var features = (r.Features ?? []).Select(f => f.Trim()).Where(f => f.Length > 0).ToArray();
        if (features.Any(f => f.Length > 120) || features.Length > 10) return Bad("Máximo 10 ventajas de hasta 120 caracteres.");
        if ((r.Tagline ?? "").Trim().Length > 80) return Bad("La frase corta admite hasta 80 caracteres.");
        if (p.Plan is not null && !r.IsActive && p.Plan == CardPlan.Sencilla)
            return Bad("El plan Sencilla debe seguir activo: es el plan base de toda tarjeta.");

        // Un precio nuevo no toca promociones ni cobros anteriores: la promoción vigente se recalcula sobre el nuevo precio.
        p.Name = name;
        p.Tagline = (r.Tagline ?? "").Trim();
        p.PriceCents = r.PriceCents;
        p.Features = string.Join('\n', features);
        p.IsHighlighted = r.IsHighlighted;
        p.IsActive = r.IsActive;
        p.SortOrder = r.SortOrder;
        p.UpdatedAt = DateTime.UtcNow;
        return null;
    }

    private BadRequestObjectResult Bad(string msg) => BadRequest(new { errors = new[] { msg } });
}
