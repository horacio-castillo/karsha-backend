using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TarjetasPro.Application.Auth;
using TarjetasPro.Domain.Entities;

namespace TarjetasPro.Infrastructure.Auth;

/// <summary>
/// Manda los correos de la plataforma cuando algo pasa (pago, publicación, vencimiento…).
/// Un correo que falla nunca debe romper el flujo que lo originó: todo se registra en el log y sigue.
/// </summary>
public class NotificationService(AppDbContext db, IEmailSender email, IConfiguration config, ILogger<NotificationService> logger, TarjetasPro.Infrastructure.Logging.SystemLogger syslog)
{
    private string PublicUrl => (config["App:PublicUrl"] ?? "http://localhost:4200").TrimEnd('/');
    private string SupportUrl => config["App:SupportUrl"] ?? $"mailto:{config["Email:From"] ?? "soporte@karsha.mx"}";

    /// <summary>Los clientes de venta asistida sin correo propio usan uno ficticio: a ese no se escribe.</summary>
    private static bool IsReal(string? address) => !string.IsNullOrWhiteSpace(address) && !address.EndsWith("@clientes.karsha.mx", StringComparison.OrdinalIgnoreCase);

    private async Task SendAsync(Guid userId, Func<string, string> body, string subject)
    {
        try
        {
            var u = await db.Users.AsNoTracking().Where(x => x.Id == userId).Select(x => new { x.Email, x.FullName }).FirstOrDefaultAsync();
            if (u is null || !IsReal(u.Email)) return;
            await email.SendAsync(u.Email!, subject, body(u.FullName));
        }
        catch (Exception ex) { logger.LogError(ex, "No se pudo enviar «{Subject}» al usuario {UserId}", subject, userId); await syslog.ErrorAsync("correo", $"No se pudo enviar «{subject}»: {ex.Message}", ex.ToString(), "SMTP", null, userId); }
    }

    /// <summary>Recibo del pago y, si es la primera vez que se publica, el aviso de «tarjeta publicada».</summary>
    public async Task PaymentPaidAsync(Guid paymentId, bool firstPublish)
    {
        try
        {
            var p = await db.Payments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == paymentId);
            if (p is null) return;
            var card = await db.Cards.IgnoreQueryFilters().AsNoTracking().Where(c => c.Id == p.CardId).Select(c => new { c.Slug, c.Title }).FirstOrDefaultAsync();
            var folio = Payment.Folio(p.ReceiptNumber, p.PaidAt) ?? "—";
            await SendAsync(p.UserId, n => EmailCatalog.Receipt(n, folio, p.CardTitle, p.Plan.ToString(), p.AmountCents, p.MethodLabel ?? p.Provider, p.PaidAt ?? DateTime.UtcNow, p.PeriodEnd, $"{PublicUrl}/recibo/{p.Id}"), $"Recibo {folio} · KARSHA");
            if (firstPublish && card is not null)
                await SendAsync(p.UserId, n => EmailCatalog.CardPublished(n, card.Title, $"{PublicUrl}/{card.Slug}"), "Tu tarjeta ya está publicada · KARSHA");
        }
        catch (Exception ex) { logger.LogError(ex, "Aviso de pago {PaymentId}", paymentId); await syslog.ErrorAsync("correo", ex, "Aviso de pago"); }
    }

    /// <summary>Ficha de OXXO o cobro en efectivo pendiente.</summary>
    public async Task PaymentPendingAsync(Guid paymentId)
    {
        var p = await db.Payments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == paymentId);
        if (p is null || p.Status != PaymentStatus.Pending) return;
        await SendAsync(p.UserId, n => EmailCatalog.CashPending(n, p.CardTitle, p.AmountCents, p.OxxoReference, p.VoucherUrl, p.VoucherExpiresAt, $"{PublicUrl}/pagos"), "Tu pago está pendiente · KARSHA");
    }

    public async Task PaymentFailedAsync(Guid paymentId)
    {
        var p = await db.Payments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == paymentId);
        if (p is null) return;
        await SendAsync(p.UserId, n => EmailCatalog.PaymentFailed(n, p.CardTitle, p.AmountCents, $"{PublicUrl}/tarjetas/{p.CardId}"), "No se completó tu pago · KARSHA");
    }

    /// <param name="what">Ej. «tu tarjeta Despacho» o «tu cuenta».</param>
    public Task SuspendedAsync(Guid userId, string what) =>
        SendAsync(userId, n => EmailCatalog.Suspended(n, what, SupportUrl), $"Suspendimos {what} · KARSHA");

    public Task AssistedSaleAsync(Guid userId, string cardTitle, string slug, bool paid, string? folio) =>
        SendAsync(userId, n => EmailCatalog.AssistedSale(n, cardTitle, $"{PublicUrl}/{slug}", $"{PublicUrl}/login", paid, folio), "Tu tarjeta ya está lista · KARSHA");

    /// <summary>Revisa las tarjetas publicadas: avisa 7 días y 1 día antes de vencer, y cuando vencen. Cada aviso sale una sola vez por vencimiento.</summary>
    public async Task ExpiryNoticesAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        // Por vencer: escalón de 7 días y escalón de 1 día. ExpiryNoticeFor/Days recuerdan el último aviso de este vencimiento.
        var soon = await db.Cards.Where(c => c.Status == CardStatus.Published && c.ExpiresAt != null && c.ExpiresAt > now && c.ExpiresAt <= now.AddDays(7))
            .OrderBy(c => c.ExpiresAt).Take(300).ToListAsync(ct);
        var changed = false;
        foreach (var c in soon)
        {
            var left = Math.Max(1, (int)Math.Ceiling((c.ExpiresAt!.Value - now).TotalDays));
            var stage = left <= 1 ? 1 : 7;
            if (c.ExpiryNoticeFor == c.ExpiresAt && c.ExpiryNoticeDays <= stage) continue;
            await SendAsync(c.UserId, n => EmailCatalog.ExpiringSoon(n, c.Title, c.ExpiresAt!.Value, left, $"{PublicUrl}/tarjetas/{c.Id}"),
                $"Tu tarjeta vence {(left <= 1 ? "mañana" : $"en {left} días")} · KARSHA");
            c.ExpiryNoticeFor = c.ExpiresAt;
            c.ExpiryNoticeDays = stage;
            changed = true;
        }
        if (changed) await db.SaveChangesAsync(ct);

        // Vencidas: el servicio de vencimiento ya les puso Expired.
        var expired = await db.Cards.Where(c => c.Status == CardStatus.Expired && c.ExpiresAt != null && (c.ExpiredNoticeFor == null || c.ExpiredNoticeFor != c.ExpiresAt))
            .Take(200).ToListAsync(ct);
        foreach (var c in expired)
        {
            // Solo se avisa de las recién vencidas. Las viejas se marcan sin enviar nada: así, al activar este servicio, nadie recibe avisos de tarjetas que vencieron hace meses.
            if (c.ExpiresAt >= now.AddDays(-3))
                await SendAsync(c.UserId, n => EmailCatalog.Expired(n, c.Title, $"{PublicUrl}/tarjetas/{c.Id}"), "Tu tarjeta venció · KARSHA");
            c.ExpiredNoticeFor = c.ExpiresAt;
        }
        if (expired.Count > 0) await db.SaveChangesAsync(ct);
    }
}
