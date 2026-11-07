namespace TarjetasPro.Infrastructure.Payments;

public record CheckoutRequest(Guid PaymentId, string CardTitle, int AmountCents, string Currency, string CustomerEmail,
    string SuccessUrl, string CancelUrl);

public record CheckoutSession(string Id, string Url);

public interface IPaymentProvider
{
    string Name { get; }
    Task<CheckoutSession> CreateCheckoutAsync(CheckoutRequest request, CancellationToken ct = default);
    /// <summary>Consulta al proveedor si la sesión ya está pagada (no se confía en la redirección del navegador).</summary>
    Task<bool> IsPaidAsync(string sessionId, CancellationToken ct = default);
}
