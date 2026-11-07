using Microsoft.Extensions.Configuration;

namespace TarjetasPro.Infrastructure.Payments;

/// <summary>Solo para desarrollo: la "pasarela" es una página de la propia app y cualquier confirmación cuenta como pago.</summary>
public class SimulatedPaymentProvider(IConfiguration config) : IPaymentProvider
{
    public string Name => "Simulated";

    public Task<CheckoutSession> CreateCheckoutAsync(CheckoutRequest r, CancellationToken ct = default)
    {
        var baseUrl = (config["App:PublicUrl"] ?? "http://localhost:4200").TrimEnd('/');
        return Task.FromResult(new CheckoutSession($"sim_{r.PaymentId:N}", $"{baseUrl}/pago/simulado/{r.PaymentId}"));
    }

    public Task<bool> IsPaidAsync(string sessionId, CancellationToken ct = default) => Task.FromResult(true);
}
