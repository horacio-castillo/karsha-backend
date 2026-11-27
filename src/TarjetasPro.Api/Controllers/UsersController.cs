using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TarjetasPro.Application.Common;
using TarjetasPro.Domain.Entities;
using TarjetasPro.Domain.Security;
using TarjetasPro.Infrastructure;
using TarjetasPro.Infrastructure.Identity;

namespace TarjetasPro.Api.Controllers;

public record ActiveRequest(bool IsActive);
public record RoleRequest(string Role);
public record NewUserRequest(string FullName, string? Email, string? Phone, string? Role, bool SendInvite);
public record UpdateUserRequest(string FullName, string? Phone);
public record VerifyEmailRequest(string? Reason, string? Password);

[ApiController]
[Route("api/users")]
[Authorize(Policy = Permissions.UsersManage)]
public class UsersController(UserManager<ApplicationUser> users, AppDbContext db, TarjetasPro.Infrastructure.Auth.NotificationService notify, TarjetasPro.Application.Auth.IAuthService auth) : ControllerBase
{
    private Guid CurrentId => Guid.Parse(User.FindFirstValue("sub")!);

    [HttpGet]
    public async Task<IActionResult> List(string? search, int? page, int? pageSize, string? sort = null, string? dir = null)
    {
        var (pg, size) = Paged<object>.Normalize(page, pageSize);
        var q = users.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(u => u.Email!.Contains(s) || u.FullName.Contains(s));
        }

        var total = await q.CountAsync();
        var desc = Sorting.IsDesc(dir);
        var ordered = sort switch
        {
            "name" => q.By(desc, u => u.FullName),
            "role" => q.By(desc, u => db.UserRoles.Where(x => x.UserId == u.Id).Join(db.Roles, x => x.RoleId, r => r.Id, (x, r) => r.Name).Min()),
            // Las cuentas sin correo propio no se verifican: van al final.
            "verified" => q.ByEmptyLast(desc, u => u.Email!.EndsWith("@clientes.karsha.mx"), u => u.EmailConfirmed),
            // Por la vigencia más lejana de sus tarjetas, la misma que muestra la columna.
            "service" => q.ByEmptyLast(desc, u => !db.Cards.Any(c => c.UserId == u.Id && c.ExpiresAt != null), u => db.Cards.Where(c => c.UserId == u.Id).Max(c => c.ExpiresAt)),
            "seen" => q.ByEmptyLast(desc, u => u.LastLoginAt == null, u => u.LastLoginAt),
            "created" => q.By(desc, u => u.CreatedAt),
            // «Activo» va antes que «Inactivo» al ordenar de la A a la Z.
            "state" => q.By(desc, u => !u.IsActive),
            _ => q.OrderByDescending(u => u.CreatedAt),
        };
        var list = await ordered.ThenBy(u => u.Id).Skip((pg - 1) * size).Take(size)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.FullName,
                u.IsActive,
                u.CreatedAt,
                Phone = u.PhoneNumber,
                u.EmailConfirmed,
                u.LastLoginAt,
                Cards = db.Cards.Count(c => c.UserId == u.Id),
                // Su servicio principal: la tarjeta con vigencia más lejana.
                Best = db.Cards.Where(c => c.UserId == u.Id).OrderByDescending(c => c.ExpiresAt)
                    .Select(c => new { Plan = c.Plan.ToString(), Status = c.Status.ToString(), c.ExpiresAt }).FirstOrDefault(),
            }).ToListAsync();

        var ids = list.Select(u => u.Id).ToList();
        var roleRows = await (from ur in db.UserRoles
                              join r in db.Roles on ur.RoleId equals r.Id
                              where ids.Contains(ur.UserId)
                              select new { ur.UserId, r.Name }).ToListAsync();

        var items = list.Select(u => (object)new
        {
            u.Id, u.Email, u.FullName, u.IsActive, u.CreatedAt, u.Cards, u.Phone, u.EmailConfirmed, u.LastLoginAt, HasOwnEmail = !IsPlaceholder(u.Email), Best = u.Best,
            Roles = roleRows.Where(r => r.UserId == u.Id).Select(r => r.Name).ToArray(),
        }).ToList();
        return Ok(new Paged<object>(items, total, pg, size));
    }

    private static bool IsPlaceholder(string? email) => email is null || email.EndsWith("@clientes.karsha.mx", StringComparison.OrdinalIgnoreCase);

    /// <summary>Ficha completa de una persona: datos, tarjetas y pagos.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id)
    {
        var u = await users.FindByIdAsync(id.ToString());
        if (u is null) return NotFound();
        var cards = await db.Cards.AsNoTracking().Where(c => c.UserId == id).OrderByDescending(c => c.CreatedAt)
            .Select(c => new { c.Id, c.Title, c.Slug, c.Product, Plan = c.Plan.ToString(), Status = c.Status.ToString(), c.ExpiresAt, c.CreatedAt }).ToListAsync();
        var pays = await db.Payments.AsNoTracking().Where(p => p.UserId == id).OrderByDescending(p => p.CreatedAt).Take(50)
            .Select(p => new { p.Id, p.CardTitle, Plan = p.Plan.ToString(), p.AmountCents, Status = p.Status.ToString(), Method = p.MethodLabel ?? p.Provider, p.PaidAt, p.CreatedAt, p.ReceiptNumber }).ToListAsync();
        return Ok(new
        {
            u.Id, u.Email, u.FullName, Phone = u.PhoneNumber, u.IsActive, u.EmailConfirmed, u.CreatedAt, u.LastLoginAt,
            HasOwnEmail = !IsPlaceholder(u.Email),
            Roles = await users.GetRolesAsync(u),
            Cards = cards,
            Payments = pays.Select(p => new { p.Id, p.CardTitle, p.Plan, p.AmountCents, p.Status, p.Method, p.PaidAt, p.CreatedAt, Folio = TarjetasPro.Domain.Entities.Payment.Folio(p.ReceiptNumber, p.PaidAt) }),
            Paid = pays.Where(p => p.Status == "Paid").Sum(p => p.AmountCents),
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateUserRequest r)
    {
        var name = (r.FullName ?? "").Trim();
        if (name.Length < 2) return BadRequest(new { errors = new[] { "Escribe el nombre." } });
        var u = await users.FindByIdAsync(id.ToString());
        if (u is null) return NotFound();
        var phone = new string((r.Phone ?? "").Where(char.IsDigit).ToArray());
        u.FullName = name[..Math.Min(name.Length, 100)];
        u.PhoneNumber = phone.Length > 0 ? phone : null;
        await users.UpdateAsync(u);
        return NoContent();
    }

    /// <summary>Alta de una persona desde el panel. Sin correo propio se crea con uno interno (como en la venta asistida).</summary>
    [HttpPost]
    public async Task<IActionResult> Create(NewUserRequest r)
    {
        var name = (r.FullName ?? "").Trim();
        if (name.Length < 2) return BadRequest(new { errors = new[] { "Escribe el nombre." } });
        var role = string.IsNullOrWhiteSpace(r.Role) ? Roles.Cliente : r.Role!;
        if (!await db.Roles.AnyAsync(x => x.Name == role)) return BadRequest(new { errors = new[] { "Rol no válido." } });
        if (Roles.IsPrivileged(role) && !User.HasClaim(Permissions.ClaimType, Permissions.RolesManage))
            return StatusCode(403, new { errors = new[] { "Solo el Super Admin puede dar de alta a un administrador." } });

        var email = (r.Email ?? "").Trim().ToLowerInvariant();
        if (email.Length > 0)
        {
            if (!System.Net.Mail.MailAddress.TryCreate(email, out _)) return BadRequest(new { errors = new[] { "El correo no es válido." } });
            if (await users.FindByEmailAsync(email) is not null) return BadRequest(new { errors = new[] { "Ese correo ya tiene cuenta." } });
        }
        else
        {
            if (role != Roles.Cliente) return BadRequest(new { errors = new[] { "Un administrador necesita correo." } });
            email = $"cliente-{Guid.NewGuid():N}"[..16] + "@clientes.karsha.mx";
        }
        var phone = new string((r.Phone ?? "").Where(char.IsDigit).ToArray());
        var u = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, FullName = name[..Math.Min(name.Length, 100)], PhoneNumber = phone.Length > 0 ? phone : null };
        var made = await users.CreateAsync(u);
        if (!made.Succeeded) return BadRequest(new { errors = made.Errors.Select(e => e.Description).ToArray() });
        await users.AddToRoleAsync(u, role);

        string? invite = null;
        if (r.SendInvite && !IsPlaceholder(email))
        {
            try { invite = (await auth.InviteAsync(u.Id)).Message; }
            catch { invite = null; }
        }
        return Ok(new { id = u.Id, invited = invite is not null });
    }

    /// <summary>Manda al cliente el enlace para crear una contraseña nueva.</summary>
    [HttpPost("{id:guid}/send-reset")]
    public async Task<IActionResult> SendReset(Guid id)
    {
        var u = await users.FindByIdAsync(id.ToString());
        if (u is null) return NotFound();
        if (IsPlaceholder(u.Email)) return BadRequest(new { errors = new[] { "Esta persona no tiene correo propio." } });
        await auth.ForgotPasswordAsync(u.Email!);
        return Ok(new { message = $"Enviamos el enlace a {u.Email}." });
    }

    [HttpPost("{id:guid}/resend-verification")]
    public async Task<IActionResult> ResendVerification(Guid id)
    {
        var u = await users.FindByIdAsync(id.ToString());
        if (u is null) return NotFound();
        if (IsPlaceholder(u.Email)) return BadRequest(new { errors = new[] { "Esta persona no tiene correo propio." } });
        if (u.EmailConfirmed) return BadRequest(new { errors = new[] { "Su correo ya está verificado." } });
        await auth.ResendConfirmationAsync(u.Email!);
        return Ok(new { message = $"Reenviamos la verificación a {u.Email}." });
    }

    /// <summary>
    /// Da por bueno el correo de alguien que no pudo confirmarlo. Pide el motivo y la contraseña de quien lo hace.
    /// En usuarios solo cambia «correo confirmado»: el motivo queda en la bitácora, guardado junto con ese cambio.
    /// </summary>
    [HttpPost("{id:guid}/verify-email")]
    public async Task<IActionResult> VerifyEmail(Guid id, VerifyEmailRequest r)
    {
        IActionResult Wrong(string message, int status = 400) => StatusCode(status, new { errors = new[] { message } });

        var u = await users.FindByIdAsync(id.ToString());
        if (u is null) return NotFound();
        if (IsPlaceholder(u.Email)) return Wrong("Esta persona no tiene correo propio.");
        if (u.EmailConfirmed) return Wrong("Su correo ya está verificado.");
        if (!u.IsActive) return Wrong("La cuenta está desactivada. Actívala antes de verificar su correo.");
        if ((await users.GetRolesAsync(u)).Any(Roles.IsPrivileged) && !User.HasClaim(Permissions.ClaimType, Permissions.RolesManage))
            return Wrong("Solo el Super Admin puede verificar el correo de un administrador.", 403);

        var reason = (r.Reason ?? "").Trim();
        if (reason.Length is < 10 or > 300) return Wrong("Escribe el motivo (de 10 a 300 letras).");

        // Contraseña de quien verifica. Nunca 401: la web lo tomaría como sesión vencida.
        var me = await users.FindByIdAsync(CurrentId.ToString());
        if (me is not { IsActive: true }) return Wrong("Tu cuenta no puede hacer esto.", 403);
        if (!await users.HasPasswordAsync(me))
            return Wrong("Tu cuenta no tiene contraseña. Créala desde «¿Olvidaste tu contraseña?» y vuelve a intentar.");
        const string locked = "Demasiados intentos con la contraseña. Espera 5 minutos y vuelve a probar.";
        if (await users.IsLockedOutAsync(me)) return Wrong(locked);
        if (!await users.CheckPasswordAsync(me, r.Password ?? ""))
        {
            await users.AccessFailedAsync(me);
            return Wrong(await users.IsLockedOutAsync(me) ? locked : "Tu contraseña no es correcta.");
        }
        await users.ResetAccessFailedCountAsync(me);

        // La bitácora se agrega antes de actualizar: los dos cambios se guardan juntos o ninguno.
        u.EmailConfirmed = true;
        db.AuditLogs.Add(new AuditLog
        {
            ActorId = me.Id, ActorName = me.FullName, Area = "usuarios", Action = "Verificó un correo manualmente",
            Target = Cut($"{u.FullName} · {u.Email}", 200), Detail = Cut($"Motivo: {reason}", 500),
            Ip = Cut(HttpContext.Connection.RemoteIpAddress?.ToString() ?? "", 45),
        });
        var saved = await users.UpdateAsync(u);
        if (!saved.Succeeded) return BadRequest(new { errors = saved.Errors.Select(e => e.Description).ToArray() });

        var told = await notify.EmailVerifiedByTeamAsync(u.Id);
        return Ok(new { message = told
            ? $"Correo verificado. Le avisamos a {u.Email}."
            : $"Correo verificado, pero no pudimos mandar el aviso a {u.Email}." });
    }

    private static string Cut(string value, int max) => value.Length <= max ? value : value[..max];

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
        if (!r.IsActive) await notify.SuspendedAsync(id, "tu cuenta");
        return NoContent();
    }

    /// <summary>Roles que puede asignar quien consulta (Admin y Super Admin solo si tiene «roles.manage»).</summary>
    [HttpGet("roles")]
    public async Task<IActionResult> AssignableRoles()
    {
        var canPrivileged = User.HasClaim(Permissions.ClaimType, Permissions.RolesManage);
        var names = await db.Roles.AsNoTracking().Select(r => r.Name!).ToListAsync();
        return Ok(names
            .OrderBy(n => Array.IndexOf(Roles.System, n) is var i && i < 0 ? 99 : i).ThenBy(n => n)
            .Select(n => new { name = n, assignable = canPrivileged || !Roles.IsPrivileged(n) }));
    }

    [HttpPut("{id:guid}/role")]
    public async Task<IActionResult> SetRole(Guid id, RoleRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.Role) || !await db.Roles.AnyAsync(x => x.Name == r.Role))
            return BadRequest(new { errors = new[] { "Rol no válido." } });
        if (id == CurrentId)
            return BadRequest(new { errors = new[] { "No puedes cambiar tu propio rol." } });
        var user = await users.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();

        var current = await users.GetRolesAsync(user);
        var touchesPrivileged = Roles.IsPrivileged(r.Role) || current.Any(Roles.IsPrivileged);
        if (touchesPrivileged && !User.HasClaim(Permissions.ClaimType, Permissions.RolesManage))
            return StatusCode(403, new { errors = new[] { "Solo el Super Admin puede dar o quitar acceso de administrador." } });
        if (current.Contains(Roles.SuperAdmin) && r.Role != Roles.SuperAdmin
            && (await users.GetUsersInRoleAsync(Roles.SuperAdmin)).Count <= 1)
            return BadRequest(new { errors = new[] { "Debe quedar al menos un Super Admin." } });

        await users.RemoveFromRolesAsync(user, current);
        await users.AddToRoleAsync(user, r.Role);
        // Cierra sus sesiones para que el nuevo rol aplique de inmediato.
        await db.RefreshTokens.Where(t => t.UserId == id && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, DateTime.UtcNow));
        return NoContent();
    }
}
