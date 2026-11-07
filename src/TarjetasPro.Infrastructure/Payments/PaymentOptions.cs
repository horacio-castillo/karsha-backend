using TarjetasPro.Domain.Entities;

namespace TarjetasPro.Infrastructure.Payments;

public class PaymentOptions
{
    public const string Section = "Payments";

    /// <summary>"Stripe" o "Simulated" (solo desarrollo).</summary>
    public string Provider { get; set; } = "Stripe";
    /// <summary>Precio por tarjeta y periodo, en centavos, según el plan.</summary>
    public PlanPrices Prices { get; set; } = new();
    public string Currency { get; set; } = "mxn";
    public int Months { get; set; } = 12;
    public StripeOptions Stripe { get; set; } = new();
}

public class PlanPrices
{
    public int Sencilla { get; set; } = 9900;
    public int Media { get; set; } = 14900;
    public int Pro { get; set; } = 19900;

    public int For(CardPlan plan) => plan switch
    {
        CardPlan.Media => Media,
        CardPlan.Pro => Pro,
        _ => Sencilla,
    };
}

public class StripeOptions
{
    public string SecretKey { get; set; } = "";
    public string WebhookSecret { get; set; } = "";
}
