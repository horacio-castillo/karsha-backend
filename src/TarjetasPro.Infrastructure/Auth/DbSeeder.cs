using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TarjetasPro.Domain.Security;
using TarjetasPro.Infrastructure.Identity;

namespace TarjetasPro.Infrastructure.Auth;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await TemplateSeeder.SeedAsync(db);
        await ExecutiveTemplates.SeedAsync(db);
        await PortraitTemplates.SeedAsync(db);
        await InvitationTemplates.SeedAsync(db);
        await MenuTemplates.SeedAsync(db);
        await CatalogTemplates.SeedAsync(db);
        await CanvasTemplateSeed.SeedAsync(db);
        await SyncFieldKeysAsync(db);
        await PlanSeeder.SeedAsync(db, sp.GetRequiredService<IOptions<TarjetasPro.Infrastructure.Payments.PaymentOptions>>().Value);

        var roles = sp.GetRequiredService<RoleManager<ApplicationRole>>();
        foreach (var (roleName, perms) in Permissions.ByRole)
        {
            var role = await roles.FindByNameAsync(roleName);
            if (role is null)
            {
                role = new ApplicationRole(roleName);
                await roles.CreateAsync(role);
            }
            var claims = await roles.GetClaimsAsync(role);
            var has = claims.Where(c => c.Type == Permissions.ClaimType).Select(c => c.Value).ToHashSet();
            var seeded = claims.Where(c => c.Type == Permissions.SeededClaimType).Select(c => c.Value).ToHashSet();
            // Solo se agrega lo que el rol nunca ha recibido: si el Super Admin quitó un permiso, no regresa al reiniciar.
            foreach (var p in perms.Where(p => !seeded.Contains(p)))
            {
                if (!has.Contains(p)) await roles.AddClaimAsync(role, new Claim(Permissions.ClaimType, p));
                await roles.AddClaimAsync(role, new Claim(Permissions.SeededClaimType, p));
            }
        }

        var admin = sp.GetRequiredService<IOptions<SeedAdminOptions>>().Value;
        if (string.IsNullOrWhiteSpace(admin.Email) || string.IsNullOrWhiteSpace(admin.Password)) return;

        var users = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var existing = await users.FindByEmailAsync(admin.Email);
        if (existing is not null)
        {
            // Si nadie es Super Admin todavía, el admin inicial lo es.
            if ((await users.GetUsersInRoleAsync(Roles.SuperAdmin)).Count == 0)
            {
                await users.RemoveFromRolesAsync(existing, await users.GetRolesAsync(existing));
                await users.AddToRoleAsync(existing, Roles.SuperAdmin);
            }
            return;
        }

        var user = new ApplicationUser
        {
            UserName = admin.Email,
            Email = admin.Email,
            EmailConfirmed = true,
            FullName = admin.FullName,
        };
        var result = await users.CreateAsync(user, admin.Password);
        if (result.Succeeded) await users.AddToRoleAsync(user, Roles.SuperAdmin);
        else throw new InvalidOperationException(
            "No se pudo crear el admin: " + string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    /// <summary>Mantiene al día los campos de cada plantilla (p. ej. al sumarse «Dirección 2 y 3») sin tocar su diseño.</summary>
    private static async Task SyncFieldKeysAsync(AppDbContext db)
    {
        var changed = false;
        foreach (var t in await db.Templates.ToListAsync())
        {
            var keys = string.Join(',', TarjetasPro.Application.Templates.TemplateRenderer.ExtractFields(t.Html));
            if (t.FieldKeys == keys) continue;
            t.FieldKeys = keys;
            changed = true;
        }
        if (changed) await db.SaveChangesAsync();
    }
}
