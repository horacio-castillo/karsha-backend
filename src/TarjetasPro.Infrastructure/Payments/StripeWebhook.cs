using Stripe;
using Stripe.Checkout;

namespace TarjetasPro.Infrastructure.Payments;

public enum WebhookOutcome { Ignored, Paid, Failed, Expired }

public record WebhookResult(WebhookOutcome Outcome, string? SessionId);

public static class StripeWebhook
{
    /// <summary>Verifica la firma del webhook (lanza StripeException si no es válida) y lo traduce a un resultado.</summary>
    public static WebhookResult Parse(string json, string signatureHeader, string secret)
    {
        var ev = EventUtility.ConstructEvent(json, signatureHeader, secret, throwOnApiVersionMismatch: false);
        if (ev.Data.Object is not Session session) return new(WebhookOutcome.Ignored, null);

        return ev.Type switch
        {
            "checkout.session.completed" or "checkout.session.async_payment_succeeded"
                when session.PaymentStatus == "paid" => new(WebhookOutcome.Paid, session.Id),
            "checkout.session.async_payment_failed" => new(WebhookOutcome.Failed, session.Id),
            "checkout.session.expired" => new(WebhookOutcome.Expired, session.Id),
            _ => new(WebhookOutcome.Ignored, session.Id),
        };
    }
}
