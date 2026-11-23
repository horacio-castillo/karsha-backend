namespace TarjetasPro.Infrastructure.Payments;

public record CheckoutRequest(Guid PaymentId, string CardTitle, int AmountCents, string Currency, string CustomerEmail,
    string SuccessUrl, string CancelUrl);

public record CheckoutSession(string Id, string Url);

/// <summary>Cómo se está pagando o se pagó: tarjeta (marca y últimos 4) u OXXO (referencia y ficha).</summary>
public record PaymentDetails(string Type, string Label, string? OxxoReference = null, string? VoucherUrl = null, DateTime? VoucherExpiresAt = null);

public interface IPaymentProvider
{
    string Name { get; }
    Task<CheckoutSession> CreateCheckoutAsync(CheckoutRequest request, CancellationToken ct = default);
    /// <summary>Consulta al proveedor si la sesión ya está pagada (no se confía en la redirección del navegador).</summary>
    Task<bool> IsPaidAsync(string sessionId, CancellationToken ct = default);
    /// <summary>Método elegido por el cliente, si el proveedor ya lo conoce.</summary>
    Task<PaymentDetails?> GetDetailsAsync(string sessionId, CancellationToken ct = default);
}
