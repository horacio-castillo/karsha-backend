namespace TarjetasPro.Domain.Entities;

/// <summary>Cupón creado por el Admin. Descuenta un porcentaje (100 = gratis) al pagar.</summary>
public class Coupon
{
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Siempre en mayúsculas; el cliente lo escribe sin importar mayúsculas.</summary>
    public string Code { get; set; } = "";
    /// <summary>1–100. 100 = la tarjeta se activa sin pasar por la pasarela de pago.</summary>
    public int Percent { get; set; }
    /// <summary>Null = sirve para cualquier plan.</summary>
    public CardPlan? Plan { get; set; }
    /// <summary>Null = sin límite de usos.</summary>
    public int? MaxUses { get; set; }
    public int Uses { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    /// <summary>Nota interna (a quién se le dio, para qué).</summary>
    public string Note { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
