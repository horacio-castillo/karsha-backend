using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace TarjetasPro.Infrastructure.Payments;

public class StripePaymentProvider(IOptions<PaymentOptions> options) : IPaymentProvider
{
    private readonly StripeOptions _stripe = options.Value.Stripe;

    public string Name => "Stripe";

    private SessionService Sessions()
    {
        if (string.IsNullOrWhiteSpace(_stripe.SecretKey))
            throw new InvalidOperationException("Stripe no está configurado (Payments:Stripe:SecretKey).");
        return new SessionService(new StripeClient(_stripe.SecretKey));
    }

    public async Task<CheckoutSession> CreateCheckoutAsync(CheckoutRequest r, CancellationToken ct = default)
    {
        var session = await Sessions().CreateAsync(new SessionCreateOptions
        {
            Mode = "payment",
            ClientReferenceId = r.PaymentId.ToString(),
            CustomerEmail = r.CustomerEmail,
            SuccessUrl = r.SuccessUrl,
            CancelUrl = r.CancelUrl,
            Metadata = new Dictionary<string, string> { ["payment_id"] = r.PaymentId.ToString() },
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = r.Currency,
                        UnitAmount = r.AmountCents,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"Tarjeta digital: {r.CardTitle}",
                        },
                    },
                },
            ],
        }, cancellationToken: ct);

        return new CheckoutSession(session.Id, session.Url);
    }

    public async Task<bool> IsPaidAsync(string sessionId, CancellationToken ct = default)
    {
        var session = await Sessions().GetAsync(sessionId, cancellationToken: ct);
        return session.PaymentStatus == "paid";
    }
}
