using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TarjetasPro.Domain.Entities;
using TarjetasPro.Domain.Security;
using TarjetasPro.Infrastructure;

namespace TarjetasPro.Api.Controllers;

public record HomeSettingRequest(bool Picker);

/// <summary>Ajustes globales de la plataforma.</summary>
[ApiController]
[Route("api/settings")]
public class SettingsController(AppDbContext db) : ControllerBase
{
    private async Task<bool> PickerOn() =>
        (await db.AppSettings.AsNoTracking().Where(a => a.Key == AppSettingKeys.HomePicker).Select(a => a.Value).FirstOrDefaultAsync()) != "0";

    /// <summary>Pantalla de inicio tras entrar: si está encendida, los productos activos entre los que elegir.</summary>
    [Authorize]
    [HttpGet("home")]
    public async Task<IActionResult> Home()
    {
        var products = await db.ProductSettings.AsNoTracking()
            .Where(p => p.IsActive && (p.Key == Products.Cards || p.Key == Products.Menu || p.Key == Products.Catalog || p.Key == Products.Invitations))
            .OrderBy(p => p.SortOrder).Select(p => new { p.Key, p.Name, p.Description, p.ComingSoon }).ToListAsync();
        return Ok(new { picker = await PickerOn(), products });
    }

    [Authorize(Policy = Permissions.CardsManageAll)]
    [HttpPut("home")]
    public async Task<IActionResult> SetHome(HomeSettingRequest r)
    {
        var row = await db.AppSettings.FirstOrDefaultAsync(a => a.Key == AppSettingKeys.HomePicker);
        if (row is null) db.AppSettings.Add(new AppSetting { Key = AppSettingKeys.HomePicker, Value = r.Picker ? "1" : "0" });
        else row.Value = r.Picker ? "1" : "0";
        await db.SaveChangesAsync();
        return NoContent();
    }
}
