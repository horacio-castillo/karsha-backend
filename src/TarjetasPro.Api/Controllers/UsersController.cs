using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TarjetasPro.Application.Common;
using TarjetasPro.Domain.Security;
using TarjetasPro.Infrastructure;
using TarjetasPro.Infrastructure.Identity;

namespace TarjetasPro.Api.Controllers;

public record ActiveRequest(bool IsActive);
public record RoleRequest(string Role);

[ApiController]
[Route("api/users")]
[Authorize(Policy = Permissions.UsersManage)]
public class UsersController(UserManager<ApplicationUser> users, AppDbContext db) : ControllerBase
{
    private Guid CurrentId => Guid.Parse(User.FindFirstValue("sub")!);

    [HttpGet]
    public async Task<IActionResult> List(string? search, int? page, int? pageSize)
    {
        var (pg, size) = Paged<object>.Normalize(page, pageSize);
        var q = users.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(u => u.Email!.Contains(s) || u.FullName.Contains(s));
        }

        var total = await q.CountAsync();
        var list = await q.OrderByDescending(u => u.CreatedAt).ThenBy(u => u.Id).Skip((pg - 1) * size).Take(size)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.FullName,
                u.IsActive,
                u.CreatedAt,
                Cards = db.Cards.Count(c => c.UserId == u.Id),
            }).ToListAsync();

        var ids = list.Select(u => u.Id).ToList();
        var roleRows = await (from ur in db.UserRoles
                              join r in db.Roles on ur.RoleId equals r.Id
                              where ids.Contains(ur.UserId)
                              select new { ur.UserId, r.Name }).ToListAsync();

        var items = list.Select(u => (object)new
        {
            u.Id, u.Email, u.FullName, u.IsActive, u.CreatedAt, u.Cards,
            Roles = roleRows.Where(r => r.UserId == u.Id).Select(r => r.Name).ToArray(),
        }).ToList();
        return Ok(new Paged<object>(items, total, pg, size));
    }

    [HttpPut("{id:guid}/active")]
    public async Task<IActionResult> SetActive(Guid id, ActiveRequest r)
    {
        if (id == CurrentId && !r.IsActive)
            return BadRequest(new { errors = new[] { "No puedes desactivar tu propia cuenta." } });
        var user = await users.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();

        user.IsActive = r.IsActive;
        await users.UpdateAsync(user);

        // Al desactivar, se cierran todas sus sesiones.
        if (!r.IsActive)
            await db.RefreshTokens.Where(t => t.UserId == id && t.RevokedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, DateTime.UtcNow));
        return NoContent();
    }

    [HttpPut("{id:guid}/role")]
    public async Task<IActionResult> SetRole(Guid id, RoleRequest r)
    {
        if (r.Role is not (Roles.Admin or Roles.Cliente))
            return BadRequest(new { errors = new[] { "Rol no válido." } });
        if (id == CurrentId)
            return BadRequest(new { errors = new[] { "No puedes cambiar tu propio rol." } });
        var user = await users.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();

        var current = await users.GetRolesAsync(user);
        await users.RemoveFromRolesAsync(user, current);
        await users.AddToRoleAsync(user, r.Role);
        return NoContent();
    }
}
