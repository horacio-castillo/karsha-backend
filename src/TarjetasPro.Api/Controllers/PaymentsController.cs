using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;
using TarjetasPro.Domain.Entities;
using TarjetasPro.Domain.Security;
using TarjetasPro.Infrastructure;
using TarjetasPro.Infrastructure.Payments;

namespace TarjetasPro.Api.Controllers;

public record CheckoutRequestDto(Guid CardId, CardPlan? Plan = null, string? Coupon = null);
public record QuoteRequestDto(Guid CardId, CardPlan? Plan = null, string? Coupon = null);

[ApiController]
[Route("api/payments")]
public class PaymentsController(PaymentService service, AppDbContext db, IOptions<PaymentOptions> options,
    ILogger<PaymentsController> logger) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue("sub")!);

    /// <summary>Crea el pago y devuelve la URL a la que hay que llevar al cliente.</summary>
    [HttpPost("checkout")]
    [Authorize(Policy = Permissions.CardsEditOwn)]
    public async Task<IActionResult> Checkout(CheckoutRequestDto r, CancellationToken ct)
    {
        try
        {
            var email = User.FindFirstValue("email") ?? "";
            var result = await service.CreateCheckoutAsync(UserId, email, r.CardId, r.Plan, r.Coupon, ct);
            return result.Error is not null ? BadRequest(new { errors = new[] { result.Error } }) : Ok(new { url = result.Url });
        }
        catch (Exception ex) when (ex is StripeException or InvalidOperationException)
        {
            logger.LogError(ex, "Error al crear el checkout");
            return StatusCode(502, new { errors = new[] { "No se pudo iniciar el pago. Intenta de nuevo más tarde." } });
        }
    }

    /// <summary>Muestra cuánto pagaría el cliente con un cupón, sin crear ningún pago.</summary>
    [HttpPost("quote")]
    [Authorize(Policy = Permissions.CardsEditOwn)]
    public async Task<IActionResult> Quote(QuoteRequestDto r, CancellationToken ct)
    {
        var q = await service.QuoteAsync(UserId, r.CardId, r.Plan, r.Coupon, ct);
        if (q.Error is not null) return BadRequest(new { errors = new[] { q.Error } });
        return Ok(new
        {
            listPriceCents = q.PromoCents,
            finalPriceCents = q.FinalCents,
            discountCents = q.DiscountCents,
            percent = q.Coupon?.Percent,
            free = q.FinalCents == 0,
            currency = options.Value.Currency,
        });
    }

    /// <summary>La página de resultado la llama al volver del proveedor; no confía en la redirección, consulta el pago real.</summary>
    [HttpPost("{id:guid}/confirm")]
    [Authorize]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken ct)
    {
        var result = await service.ConfirmAsync(id, UserId, ct);
        return result is null ? NotFound() : Ok(new { status = result.Status.ToString(), cardId = result.CardId });
    }

    [HttpGet]
    [Authorize(Policy = Permissions.CardsViewOwn)]
    public async Task<IActionResult> Mine([FromQuery] int? page = null, [FromQuery] int pageSize = 10)
    {
        var uid = UserId;
        var q = db.Payments.AsNoTracking().Where(p => p.UserId == uid).OrderByDescending(p => p.CreatedAt);
        var proj = q.Select(p => new PayRow(p.Id, p.Status.ToString(), p.AmountCents, p.Currency, p.CreatedAt, p.PaidAt, p.PeriodEnd, p.CardTitle, p.Plan.ToString(), p.ReceiptNumber));
        // Sin "page" se conserva la lista simple (la usa la pantalla de pago simulado).
        if (page is null) return Ok((await proj.Take(100).ToListAsync()).Select(Shape));
        pageSize = Math.Clamp(pageSize, 1, 50);
        var total = await q.CountAsync();
        var pages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        var pg = Math.Clamp(page.Value, 1, pages);
        var items = (await proj.Skip((pg - 1) * pageSize).Take(pageSize).ToListAsync()).Select(Shape);
        return Ok(new { items, total, page = pg, pageSize, pages });
    }

    private record PayRow(Guid Id, string Status, int AmountCents, string Currency, DateTime CreatedAt, DateTime? PaidAt, DateTime? PeriodEnd, string Card, string Plan, int? ReceiptNumber);

    private static object Shape(PayRow p) => new
    {
        p.Id, p.Status, p.AmountCents, p.Currency, p.CreatedAt, p.PaidAt, p.PeriodEnd, p.Card, p.Plan,
        Folio = Payment.Folio(p.ReceiptNumber, p.PaidAt),
    };

    /// <summary>Recibo de un pago cobrado. Solo lo ve su dueño.</summary>
    [HttpGet("{id:guid}/receipt")]
    [Authorize]
    public async Task<IActionResult> Receipt(Guid id, CancellationToken ct)
    {
        var uid = UserId;
        var p = await db.Payments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid && x.Status == PaymentStatus.Paid, ct);
        if (p is null) return NotFound();
        var u = await db.Users.AsNoTracking().Where(x => x.Id == uid).Select(x => new { x.FullName, x.Email }).FirstOrDefaultAsync(ct);
        var months = options.Value.Months;
        return Ok(new
        {
            folio = Payment.Folio(p.ReceiptNumber, p.PaidAt),
            paymentId = p.Id,
            paidAt = p.PaidAt,
            customerName = u?.FullName ?? "",
            customerEmail = u?.Email ?? "",
            card = p.CardTitle,
            plan = p.Plan.ToString(),
            periodStart = p.PaidAt,
            periodEnd = p.PeriodEnd,
            months,
            subtotalCents = p.AmountCents + p.DiscountCents,
            discountCents = p.DiscountCents,
            couponCode = p.CouponCode,
            totalCents = p.AmountCents,
            currency = p.Currency,
            method = p.AmountCents == 0 ? "Cupón" : p.Provider == "Stripe" ? "Tarjeta (Stripe)" : "Pago de prueba",
        });
    }

    /// <summary>Webhook de Stripe: anónimo, pero solo se acepta con firma válida.</summary>
    [HttpPost("webhook/stripe")]
    [AllowAnonymous]
    public async Task<IActionResult> StripeWebhookEndpoint(CancellationToken ct)
    {
        var secret = options.Value.Stripe.WebhookSecret;
        if (string.IsNullOrWhiteSpace(secret)) return NotFound();

        string json;
        using (var reader = new StreamReader(Request.Body)) json = await reader.ReadToEndAsync(ct);

        WebhookResult parsed;
        try
        {
            parsed = StripeWebhook.Parse(json, Request.Headers["Stripe-Signature"].ToString(), secret);
        }
        catch (StripeException)
        {
            return BadRequest();
        }

        await service.HandleWebhookAsync(parsed, ct);
        return Ok();
    }
}
