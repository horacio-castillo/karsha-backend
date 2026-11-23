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
    /// <summary>Vigencia comprada (meses), congelada al crear el cobro: si el Admin cambia el plan después, no afecta lo ya comprado.</summary>
    public int Months { get; set; } = 12;
    /// <summary>Cupón usado (copia del código) y cuánto descontó; 0 si no hubo.</summary>
    public string? CouponCode { get; set; }
    public int DiscountCents { get; set; }
    public string Currency { get; set; } = "mxn";
    /// <summary>Cómo pagó el cliente: "card" u "oxxo" (vacío si aún no se sabe o es un cobro manual).</summary>
    public string? MethodType { get; set; }
    /// <summary>Texto para mostrar: "Visa ···· 4242" u "OXXO". Nunca guarda el número completo de la tarjeta.</summary>
    public string? MethodLabel { get; set; }
    /// <summary>OXXO: número de referencia, enlace a la ficha y fecha en que vence.</summary>
    public string? OxxoReference { get; set; }
    public string? VoucherUrl { get; set; }
    public DateTime? VoucherExpiresAt { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
    /// <summary>Hasta cuándo queda cubierta la tarjeta con este pago.</summary>
    public DateTime? PeriodEnd { get; set; }
    /// <summary>Número consecutivo del recibo; se asigna al confirmarse el pago y no cambia nunca.</summary>
    public int? ReceiptNumber { get; set; }

    /// <summary>Folio visible del recibo, por ejemplo KR-2026-000012.</summary>
    public static string? Folio(int? number, DateTime? paidAt) =>
        number is { } n ? $"KR-{(paidAt ?? DateTime.UtcNow):yyyy}-{n:D6}" : null;
}
