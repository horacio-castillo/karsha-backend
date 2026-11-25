using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using TarjetasPro.Domain.Entities;

namespace TarjetasPro.Infrastructure.Payments;

public record CheckoutResult(string? Url, string? Error);
public record ConfirmResult(PaymentStatus Status, Guid CardId);

public class PaymentService(AppDbContext db, IPaymentProvider provider, IOptions<PaymentOptions> options, IConfiguration config, TarjetasPro.Infrastructure.Auth.NotificationService notify)
{
    private readonly PaymentOptions _opt = options.Value;
    private string PublicUrl => (config["App:PublicUrl"] ?? "http://localhost:4200").TrimEnd('/');

    /// <summary>Precio que pagará el cliente. Siempre se calcula en el servidor, nunca viene del cliente.</summary>
    public record Quote(Card? Card, CardPlan Plan, int ListCents, int PromoCents, int FinalCents, Coupon? Coupon, string? Error)
    {
        public int DiscountCents => PromoCents - FinalCents;
    }

    /// <param name="allowReuse">Solo administradores: pueden usar el mismo cupón en varios pagos. Un cliente, una sola vez.</param>
    public async Task<Quote> QuoteAsync(Guid userId, Guid cardId, CardPlan? requested, string? couponCode, CancellationToken ct = default, bool allowReuse = false)
    {
        static Quote Fail(string msg) => new(null, CardPlan.Sencilla, 0, 0, 0, null, msg);

        var card = await db.Cards.AsNoTracking().FirstOrDefaultAsync(c => c.Id == cardId && c.UserId == userId, ct);
        if (card is null) return Fail("Tarjeta no encontrada.");
        if (card.Status == CardStatus.Suspended)
            return Fail("Esta tarjeta está suspendida. Contacta al administrador.");
        var plan = requested ?? card.Plan;
        if (!Enum.IsDefined(plan)) return Fail("Plan no válido.");

        // Mientras la tarjeta esté vigente solo se puede mantener o subir de plan (no bajar).
        var active = card.Status == CardStatus.Published && (card.ExpiresAt is null || card.ExpiresAt > DateTime.UtcNow);
        if (active && plan < card.Plan)
            return Fail($"Tu tarjeta ya tiene el plan {card.Plan}. Puedes renovarlo o mejorarlo.");

        var now = DateTime.UtcNow;
        var promo = (await db.PlanPromotions.AsNoTracking().FirstOrDefaultAsync(p => p.Product == card.Product && p.Plan == plan, ct)) is { } pr && pr.AppliesAt(now) ? pr : null;
        // Producto apagado: no se venden servicios nuevos. Quien ya pagó alguna vez puede seguir renovando.
        if (card.PaidAt is null && !await PlanCatalog.IsSellableAsync(db, card.Product, ct))
            return Fail("Este producto no está disponible por ahora.");
        var listPrice = await PlanCatalog.PriceAsync(db, plan, card.Product, ct);
        if (listPrice <= 0) return Fail("Este plan no está disponible.");
        var promoPrice = promo?.Apply(listPrice) ?? listPrice;

        Coupon? coupon = null;
        var final = promoPrice;
        var code = (couponCode ?? "").Trim().ToUpperInvariant();
        if (code.Length > 0)
        {
            coupon = await db.Coupons.AsNoTracking().FirstOrDefaultAsync(c => c.Code == code, ct);
            // Mismo mensaje para cupón inexistente o apagado: no revela qué códigos existen.
            if (coupon is null || !coupon.IsActive) return Fail("Ese cupón no es válido.");
            if (coupon.ExpiresAt is { } exp && exp <= now) return Fail("Ese cupón ya venció.");
            if (coupon.MaxUses is { } max && coupon.Uses >= max) return Fail("Ese cupón ya se agotó.");
            if (coupon.Product is { } cprod && cprod != card.Product) return Fail("Ese cupón no aplica a este producto.");
            if (coupon.Plan is { } cp && cp != plan) return Fail($"Ese cupón solo aplica al plan {cp}.");
            if (!allowReuse && await db.Payments.AnyAsync(p => p.UserId == userId && p.CouponCode == code && p.Status == PaymentStatus.Paid, ct))
                return Fail("Ya usaste ese cupón.");

            final = coupon.Percent >= 100
                ? 0
                : Math.Min(promoPrice, Math.Max(PlanPromotion.MinFinalCents, (int)Math.Round(promoPrice * (100 - coupon.Percent) / 100.0)));
        }
        return new(card, plan, listPrice, promoPrice, final, coupon, null);
    }

    public async Task<CheckoutResult> CreateCheckoutAsync(Guid userId, string email, Guid cardId, CardPlan? requested, string? couponCode = null, CancellationToken ct = default, bool allowReuse = false)
    {
        var q = await QuoteAsync(userId, cardId, requested, couponCode, ct, allowReuse);
        if (q.Error is not null) return new(null, q.Error);
        var card = q.Card!;
        var plan = q.Plan;

        var free = q.FinalCents == 0;
        var payment = new Payment
        {
            CardId = card.Id,
            UserId = userId,
            CardTitle = card.Title,
            Provider = free ? "Coupon" : provider.Name,
            Plan = plan,
            AmountCents = q.FinalCents,
            Months = await PlanMonthsAsync(card.Product, plan, ct),
            CouponCode = q.Coupon?.Code,
            DiscountCents = q.Coupon is null ? 0 : q.DiscountCents,
            Currency = _opt.Currency,
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync(ct);

        // Cupón del 100 %: no hay nada que cobrar, la tarjeta se activa de inmediato sin pasar por la pasarela.
        if (free)
        {
            await ApplyPaidAsync(payment.Id, ct);
            return new($"{PublicUrl}/pago/exito?payment={payment.Id}", null);
        }

        var session = await provider.CreateCheckoutAsync(new CheckoutRequest(
            payment.Id, $"{card.Title} · Plan {plan}", payment.AmountCents, payment.Currency, email,
            SuccessUrl: $"{PublicUrl}/pago/exito?payment={payment.Id}",
            CancelUrl: $"{PublicUrl}/dashboard"), ct);

        payment.ProviderSessionId = session.Id;
        await db.SaveChangesAsync(ct);
        return new(session.Url, null);
    }

    /// <summary>Consulta al proveedor y, si ya se pagó, activa la tarjeta. Sirve de respaldo cuando el webhook tarda.</summary>
    public async Task<ConfirmResult?> ConfirmAsync(Guid paymentId, Guid userId, CancellationToken ct = default)
    {
        var payment = await db.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.Id == paymentId && p.UserId == userId, ct);
        if (payment is null) return null;

        if (payment.Status == PaymentStatus.Pending && payment.ProviderSessionId is not null
            && await provider.IsPaidAsync(payment.ProviderSessionId, ct))
        {
            await ApplyPaidAsync(payment.Id, ct);
            await SaveDetailsAsync(payment.Id, ct);
        }

        var status = await db.Payments.Where(p => p.Id == paymentId).Select(p => p.Status).FirstAsync(ct);
        return new(status, payment.CardId);
    }

    public async Task HandleWebhookAsync(WebhookResult result, CancellationToken ct = default)
    {
        if (result.SessionId is null || result.Outcome == WebhookOutcome.Ignored) return;
        var id = await db.Payments.Where(p => p.ProviderSessionId == result.SessionId)
            .Select(p => (Guid?)p.Id).FirstOrDefaultAsync(ct);
        if (id is null) return;

        switch (result.Outcome)
        {
            case WebhookOutcome.Paid: await ApplyPaidAsync(id.Value, ct); await SaveDetailsAsync(id.Value, ct); break;
            case WebhookOutcome.Awaiting: await SaveDetailsAsync(id.Value, ct); await notify.PaymentPendingAsync(id.Value); break;
            case WebhookOutcome.Failed: if (await SetPendingToAsync(id.Value, PaymentStatus.Failed, ct) > 0) await notify.PaymentFailedAsync(id.Value); break;
            case WebhookOutcome.Expired: if (await SetPendingToAsync(id.Value, PaymentStatus.Expired, ct) > 0) await notify.PaymentFailedAsync(id.Value); break;
        }
    }

    /// <summary>Guarda cómo paga el cliente (tarjeta u OXXO). Si el proveedor falla, no se pierde el cobro: solo queda sin detalle.</summary>
    private async Task SaveDetailsAsync(Guid id, CancellationToken ct)
    {
        try
        {
            var sid = await db.Payments.Where(p => p.Id == id).Select(p => p.ProviderSessionId).FirstOrDefaultAsync(ct);
            if (sid is null || await provider.GetDetailsAsync(sid, ct) is not { } d) return;
            await db.Payments.Where(p => p.Id == id).ExecuteUpdateAsync(s => s
                .SetProperty(p => p.MethodType, d.Type).SetProperty(p => p.MethodLabel, d.Label)
                .SetProperty(p => p.OxxoReference, d.OxxoReference).SetProperty(p => p.VoucherUrl, d.VoucherUrl)
                .SetProperty(p => p.VoucherExpiresAt, d.VoucherExpiresAt), ct);
        }
        catch (Exception) { /* es informativo; el pago ya quedó registrado */ }
    }

    private Task<int> SetPendingToAsync(Guid id, PaymentStatus status, CancellationToken ct) =>
        db.Payments.Where(p => p.Id == id && p.Status == PaymentStatus.Pending)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, status), ct);

    /// <summary>Cobro registrado a mano por el Admin (efectivo, transferencia…). Si <paramref name="paid"/> activa la tarjeta ya; si no, queda pendiente de liquidar.</summary>
    public async Task<Payment> RegisterManualAsync(Card card, CardPlan plan, int amountCents, string method, bool paid, CancellationToken ct = default, bool sendEmails = true)
    {
        var payment = new Payment
        {
            CardId = card.Id, UserId = card.UserId, CardTitle = card.Title, Provider = method, Plan = plan,
            AmountCents = amountCents, Currency = _opt.Currency,
            Months = await PlanMonthsAsync(card.Product, plan, ct),
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync(ct);
        if (paid) await ApplyPaidAsync(payment.Id, ct, sendEmails);
        else if (sendEmails) await notify.PaymentPendingAsync(payment.Id);
        return payment;
    }

    private async Task<int> PlanMonthsAsync(string product, CardPlan plan, CancellationToken ct) =>
        await db.PlanSettings.AsNoTracking().Where(p => p.Product == product && p.Plan == plan).Select(p => (int?)p.Months).FirstOrDefaultAsync(ct) ?? _opt.Months;

    /// <summary>Liquida un cobro manual pendiente: activa la tarjeta y asigna el folio del recibo.</summary>
    public Task MarkPaidManualAsync(Guid paymentId, CancellationToken ct = default) => ApplyPaidAsync(paymentId, ct);

    /// <summary>Idempotente: webhook y confirmación pueden llegar a la vez y solo uno extiende la vigencia.</summary>
    private async Task ApplyPaidAsync(Guid paymentId, CancellationToken ct, bool sendEmails = true)
    {
        var payment = await db.Payments.AsNoTracking().FirstAsync(p => p.Id == paymentId, ct);
        var card = await db.Cards.FirstOrDefaultAsync(c => c.Id == payment.CardId, ct);
        if (card is null) return;

        var now = DateTime.UtcNow;
        // Renovar antes de vencer suma meses al periodo vigente en lugar de perder los días restantes.
        var start = card.ExpiresAt is { } e && e > now ? e : now;
        var renewing = card.ExpiresAt is { } cur && cur > now && card.Status == CardStatus.Published;
        var end = start.AddMonths(payment.Months);

        var claimed = await db.Payments.Where(p => p.Id == paymentId && p.Status == PaymentStatus.Pending)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Status, PaymentStatus.Paid)
                .SetProperty(p => p.PaidAt, now)
                .SetProperty(p => p.PeriodEnd, end), ct);
        if (claimed == 0) return;

        // Folio consecutivo del recibo. Si dos pagos coinciden, el índice único obliga a reintentar con el siguiente número.
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var next = (await db.Payments.MaxAsync(p => p.ReceiptNumber, ct) ?? 0) + 1;
            try
            {
                await db.Payments.Where(p => p.Id == paymentId).ExecuteUpdateAsync(s => s.SetProperty(p => p.ReceiptNumber, next), ct);
                break;
            }
            catch (DbUpdateException) when (attempt < 4) { }
        }

        if (payment.CouponCode is { } used)
            await db.Coupons.Where(c => c.Code == used).ExecuteUpdateAsync(s => s.SetProperty(c => c.Uses, c => c.Uses + 1), ct);

        var firstPublish = card.PaidAt is null && card.Status != CardStatus.Suspended;
        card.PaidAt ??= now;
        card.ExpiresAt = end;
        // Vigente: nunca baja de plan. Vencida: arranca con el plan que acaba de pagar.
        card.Plan = renewing ? (CardPlan)Math.Max((int)card.Plan, (int)payment.Plan) : payment.Plan;
        // Una tarjeta suspendida por el Admin no se reactiva sola con un pago.
        if (card.Status != CardStatus.Suspended) card.Status = CardStatus.Published;
        card.UpdatedAt = now;
        await db.SaveChangesAsync(ct);
        if (sendEmails) await notify.PaymentPaidAsync(paymentId, firstPublish);
    }
}
