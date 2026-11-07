namespace TarjetasPro.Domain.Entities;

public enum PaymentStatus { Pending, Paid, Failed, Expired }

public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CardId { get; set; }
    public Guid UserId { get; set; }
    /// <summary>Copia del título al cobrar: el historial se conserva aunque la tarjeta se borre.</summary>
    public string CardTitle { get; set; } = "";
    /// <summary>"Stripe" o "Simulated".</summary>
    public string Provider { get; set; } = "";
    /// <summary>Id de la sesión de checkout en el proveedor; sirve para conciliar el webhook.</summary>
    public string? ProviderSessionId { get; set; }
    /// <summary>Plan que se contrata con este pago.</summary>
    public CardPlan Plan { get; set; } = CardPlan.Sencilla;
    public int AmountCents { get; set; }
    /// <summary>Cupón usado (copia del código) y cuánto descontó; 0 si no hubo.</summary>
    public string? CouponCode { get; set; }
    public int DiscountCents { get; set; }
    public string Currency { get; set; } = "mxn";
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
    /// <summary>Hasta cuándo queda cubierta la tarjeta con este pago.</summary>
    public DateTime? PeriodEnd { get; set; }
}
