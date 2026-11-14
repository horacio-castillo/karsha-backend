using Microsoft.EntityFrameworkCore;
using TarjetasPro.Domain.Entities;

namespace TarjetasPro.Infrastructure.Payments;

/// <summary>Planes de cada producto tal como los define el Admin en la base de datos.</summary>
public static class PlanCatalog
{
    public static async Task<Dictionary<CardPlan, PlanSetting>> CardPlansAsync(AppDbContext db, string product = Products.Cards, CancellationToken ct = default) =>
        (await db.PlanSettings.AsNoTracking().Where(p => p.Product == product && p.Plan != null).ToListAsync(ct))
            .ToDictionary(p => p.Plan!.Value);

    public static async Task<int> PriceAsync(AppDbContext db, CardPlan plan, string product = Products.Cards, CancellationToken ct = default) =>
        (await db.PlanSettings.AsNoTracking().Where(p => p.Product == product && p.Plan == plan).Select(p => (int?)p.PriceCents).FirstOrDefaultAsync(ct)) ?? 0;

    /// <summary>¿Se puede vender hoy a clientes nuevos? Un producto apagado deja de ofrecerse, pero el servicio activo sigue.</summary>
    public static async Task<bool> IsSellableAsync(AppDbContext db, string product, CancellationToken ct = default) =>
        await db.ProductSettings.AsNoTracking().AnyAsync(p => p.Key == product && p.IsActive, ct);
}
