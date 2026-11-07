namespace TarjetasPro.Domain.Entities;

public enum PromotionKind { Percent, Amount }

/// <summary>Promoción de un plan, definida por el Admin. Hay a lo sumo una por plan.</summary>
public class PlanPromotion
{
    /// <summary>Precio mínimo que se puede cobrar (centavos); evita montos que el proveedor de pagos rechaza.</summary>
    public const int MinFinalCents = 1000;

    public Guid Id { get; set; } = Guid.NewGuid();
    public CardPlan Plan { get; set; }
    public PromotionKind Kind { get; set; } = PromotionKind.Percent;
    /// <summary>Percent: 1–90 (%). Amount: centavos que se descuentan.</summary>
    public int Value { get; set; }
    public string Label { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }

    public bool AppliesAt(DateTime now) =>
        IsActive && (StartsAt is null || StartsAt <= now) && (EndsAt is null || EndsAt > now);

    /// <summary>Precio final en centavos tras el descuento (sin bajar de <see cref="MinFinalCents"/>).</summary>
    public int Apply(int priceCents)
    {
        var discount = Kind == PromotionKind.Percent ? (int)Math.Round(priceCents * (Value / 100.0)) : Value;
        return Math.Min(priceCents, Math.Max(MinFinalCents, priceCents - discount));
    }
}
