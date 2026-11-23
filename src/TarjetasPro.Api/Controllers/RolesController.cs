using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TarjetasPro.Domain.Security;
using TarjetasPro.Infrastructure;
using TarjetasPro.Infrastructure.Identity;

namespace TarjetasPro.Api.Controllers;

public record RolePermissionsRequest(string[] Permissions);
public record CreateRoleRequest(string Name, string[]? Permissions);

/// <summary>
/// Roles y permisos (solo Super Admin). Los cambios aplican cuando la persona vuelve a entrar
/// o se renueva su sesión, porque los permisos viajan dentro del token.
/// </summary>
[ApiController]
[Route("api/admin/roles")]
[Authorize(Policy = Permissions.RolesManage)]
public partial class RolesController(RoleManager<ApplicationRole> roles, AppDbContext db) : ControllerBase
{
    [GeneratedRegex(@"^[\p{L}\p{N} ]{2,40}$")]
    private static partial Regex NameRule();

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var all = await roles.Roles.AsNoTracking().OrderBy(r => r.Name).ToListAsync();
        var counts = await db.UserRoles.GroupBy(ur => ur.RoleId).Select(g => new { g.Key, N = g.Count() }).ToListAsync();
        var rows = new List<object>();
        foreach (var r in all.OrderBy(r => Array.IndexOf(Roles.System, r.Name) is var i && i < 0 ? 99 : i).ThenBy(r => r.Name))
        {
            var perms = (await roles.GetClaimsAsync(r)).Where(c => c.Type == Permissions.ClaimType).Select(c => c.Value).ToArray();
            rows.Add(new
            {
                r.Name,
                System = Roles.System.Contains(r.Name),
                Locked = r.Name == Roles.SuperAdmin,
                Users = counts.FirstOrDefault(c => c.Key == r.Id)?.N ?? 0,
                Permissions = perms,
                Blocked = r.Name == Roles.SuperAdmin ? [] : Permissions.Blocked(r.Name!),
            });
        }
        return Ok(new { permissions = Permissions.Catalog, roles = rows });
    }

    [HttpPut("{name}/permissions")]
    public async Task<IActionResult> SetPermissions(string name, RolePermissionsRequest r)
    {
        if (name == Roles.SuperAdmin) return Bad("El rol Super Admin siempre tiene todos los permisos.");
        var role = await roles.FindByNameAsync(name);
        if (role is null) return NotFound();
        var wanted = (r.Permissions ?? []).Distinct().ToHashSet();
        if (wanted.Any(p => !Permissions.All.Contains(p))) return Bad("Hay un permiso que no existe.");
        if (wanted.Contains(Permissions.RolesManage)) return Bad("«Roles y permisos» es solo del Super Admin.");
        if (wanted.Overlaps(Permissions.Blocked(name))) return Bad("El rol Cliente no puede tener permisos de administración.");

        var current = (await roles.GetClaimsAsync(role)).Where(c => c.Type == Permissions.ClaimType).ToList();
        foreach (var c in current.Where(c => !wanted.Contains(c.Value))) await roles.RemoveClaimAsync(role, c);
        foreach (var p in wanted.Where(p => current.All(c => c.Value != p))) await roles.AddClaimAsync(role, new Claim(Permissions.ClaimType, p));
        return NoContent();
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateRoleRequest r)
    {
        var name = (r.Name ?? "").Trim();
        if (!NameRule().IsMatch(name)) return Bad("El nombre debe tener de 2 a 40 letras, números o espacios.");
        if (await roles.RoleExistsAsync(name)) return Bad("Ya existe un rol con ese nombre.");
        var perms = (r.Permissions ?? []).Distinct().ToArray();
        if (perms.Any(p => !Permissions.All.Contains(p) || p == Permissions.RolesManage)) return Bad("Hay un permiso que no se puede dar.");

        var role = new ApplicationRole(name);
        var res = await roles.CreateAsync(role);
        if (!res.Succeeded) return Bad(string.Join(" ", res.Errors.Select(e => e.Description)));
        foreach (var p in perms) await roles.AddClaimAsync(role, new Claim(Permissions.ClaimType, p));
        return Ok(new { role.Name });
    }

    [HttpDelete("{name}")]
    public async Task<IActionResult> Delete(string name)
    {
        if (Roles.System.Contains(name)) return Bad("Los roles del sistema no se pueden borrar.");
        var role = await roles.FindByNameAsync(name);
        if (role is null) return NotFound();
        if (await db.UserRoles.AnyAsync(ur => ur.RoleId == role.Id)) return Bad("Ese rol tiene usuarios. Cámbiales el rol antes de borrarlo.");
        await roles.DeleteAsync(role);
        return NoContent();
    }

    private BadRequestObjectResult Bad(string msg) => BadRequest(new { errors = new[] { msg } });
}
