using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using TarjetasPro.Domain.Cards;
using TarjetasPro.Domain.Entities;
using TarjetasPro.Domain.Security;
using TarjetasPro.Infrastructure;

namespace TarjetasPro.Api.Controllers;

public record CategoryRequest([Required, StringLength(50, MinimumLength = 2)] string Name);

[ApiController]
[Route("api/categories")]
[Authorize(Policy = Permissions.TemplatesManage)]
public class CategoriesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var rows = await db.TemplateCategories.AsNoTracking().OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(c => new { c.Id, c.Name, Templates = db.Templates.Count(t => t.Category == c.Name) })
            .ToListAsync();
        return Ok(rows);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CategoryRequest r)
    {
        var name = r.Name.Trim();
        if (await db.TemplateCategories.AnyAsync(c => c.Name == name))
            return BadRequest(new { errors = new[] { "Esa categoría ya existe." } });
        var order = await db.TemplateCategories.AnyAsync() ? await db.TemplateCategories.MaxAsync(c => c.SortOrder) + 1 : 0;
        db.TemplateCategories.Add(new TemplateCategory { Name = name, SortOrder = order });
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Rename(Guid id, CategoryRequest r)
    {
        var cat = await db.TemplateCategories.FirstOrDefaultAsync(c => c.Id == id);
        if (cat is null) return NotFound();
        if (cat.Name == TemplateCategories.General)
            return BadRequest(new { errors = new[] { "La categoría General no se puede renombrar." } });
        var name = r.Name.Trim();
        if (await db.TemplateCategories.AnyAsync(c => c.Name == name && c.Id != id))
            return BadRequest(new { errors = new[] { "Esa categoría ya existe." } });

        var old = cat.Name;
        cat.Name = name;
        await db.Templates.Where(t => t.Category == old)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.Category, name));
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var cat = await db.TemplateCategories.FirstOrDefaultAsync(c => c.Id == id);
        if (cat is null) return NotFound();
        if (cat.Name == TemplateCategories.General)
            return BadRequest(new { errors = new[] { "La categoría General no se puede borrar." } });
        if (await db.Templates.AnyAsync(t => t.Category == cat.Name))
            return BadRequest(new { errors = new[] { "Hay plantillas en esta categoría. Muévelas antes de borrarla." } });
        db.TemplateCategories.Remove(cat);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
