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
        var options = new SessionCreateOptions
        {
            Mode = "payment",
            Locale = "es",
            // Los métodos de pago (tarjeta, OXXO…) se activan en el Dashboard de Stripe. OXXO es diferido: el webhook llega cuando el cliente paga en tienda.
            PaymentMethodOptions = new SessionPaymentMethodOptionsOptions { Oxxo = new SessionPaymentMethodOptionsOxxoOptions { ExpiresAfterDays = 3 } },
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
        };
        var session = await Sessions().CreateAsync(options, cancellationToken: ct);

        return new CheckoutSession(session.Id, session.Url);
    }

    public async Task<bool> IsPaidAsync(string sessionId, CancellationToken ct = default)
    {
        var session = await Sessions().GetAsync(sessionId, cancellationToken: ct);
        return session.PaymentStatus == "paid";
    }

    public async Task<PaymentDetails?> GetDetailsAsync(string sessionId, CancellationToken ct = default)
    {
        var session = await Sessions().GetAsync(sessionId, new SessionGetOptions { Expand = ["payment_intent", "payment_intent.latest_charge"] }, cancellationToken: ct);
        var pi = session.PaymentIntent;
        if (pi is null) return null;

        // OXXO pendiente: la referencia y la ficha viven en la siguiente acción del PaymentIntent.
        if (pi.NextAction?.OxxoDisplayDetails is { } ox)
            return new("oxxo", "OXXO", ox.Number, ox.HostedVoucherUrl, ox.ExpiresAfter);

        var details = (pi.LatestCharge as Charge)?.PaymentMethodDetails;
        if (details?.Card is { } card)
            return new("card", $"{Brand(card.Brand)} ···· {card.Last4}");
        if (details?.Oxxo is not null) return new("oxxo", "OXXO");
        return null;
    }

    private static string Brand(string? b) => (b ?? "").ToLowerInvariant() switch
    {
        "visa" => "Visa", "mastercard" => "Mastercard", "amex" => "American Express", "" => "Tarjeta",
        var x => char.ToUpperInvariant(x[0]) + x[1..],
    };
}
