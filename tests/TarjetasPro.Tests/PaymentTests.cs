using Microsoft.EntityFrameworkCore;
using TarjetasPro.Domain.Entities;
using TarjetasPro.Infrastructure.Payments;

namespace TarjetasPro.Tests;

public class QuoteTests
{
    [Fact]
    public async Task El_precio_sale_del_plan_del_producto()
    {
        using var f = new Fixture();
        var card = f.NewCard();
        var q = await f.Service().QuoteAsync(f.UserId, card.Id, CardPlan.Pro, null);
        Assert.Null(q.Error);
        Assert.Equal(12000, q.FinalCents);
    }

    [Fact]
    public async Task La_promocion_solo_aplica_a_su_producto()
    {
        using var f = new Fixture();
        f.Db.PlanPromotions.Add(new PlanPromotion { Product = Products.Catalog, Plan = CardPlan.Media, Kind = PromotionKind.Percent, Value = 50 });
        f.Db.SaveChanges();

        var tarjeta = await f.Service().QuoteAsync(f.UserId, f.NewCard(Products.Cards).Id, CardPlan.Media, null);
        var catalogo = await f.Service().QuoteAsync(f.UserId, f.NewCard(Products.Catalog).Id, CardPlan.Media, null);

        Assert.Equal(10000, tarjeta.FinalCents);   // sin descuento
        Assert.Equal(5000, catalogo.FinalCents);   // 50 %
    }

    [Fact]
    public async Task Cupon_de_porcentaje_descuenta()
    {
        using var f = new Fixture();
        f.Db.Coupons.Add(new Coupon { Code = "MITAD", Percent = 50 });
        f.Db.SaveChanges();
        var q = await f.Service().QuoteAsync(f.UserId, f.NewCard().Id, CardPlan.Media, "mitad");
        Assert.Null(q.Error);
        Assert.Equal(5000, q.FinalCents);
    }

    [Fact]
    public async Task Cupon_de_otro_producto_se_rechaza()
    {
        using var f = new Fixture();
        f.Db.Coupons.Add(new Coupon { Code = "SOLOCAT", Percent = 20, Product = Products.Catalog });
        f.Db.SaveChanges();
        var q = await f.Service().QuoteAsync(f.UserId, f.NewCard(Products.Cards).Id, CardPlan.Media, "SOLOCAT");
        Assert.NotNull(q.Error);
    }

    [Theory]
    [InlineData("NOEXISTE")]
    public async Task Cupon_inexistente_se_rechaza(string code)
    {
        using var f = new Fixture();
        var q = await f.Service().QuoteAsync(f.UserId, f.NewCard().Id, CardPlan.Media, code);
        Assert.NotNull(q.Error);
    }

    [Fact]
    public async Task Cupon_agotado_o_vencido_se_rechaza()
    {
        using var f = new Fixture();
        f.Db.Coupons.Add(new Coupon { Code = "AGOTADO", Percent = 10, MaxUses = 1, Uses = 1 });
        f.Db.Coupons.Add(new Coupon { Code = "VIEJO", Percent = 10, ExpiresAt = DateTime.UtcNow.AddDays(-1) });
        f.Db.SaveChanges();
        var card = f.NewCard();
        Assert.NotNull((await f.Service().QuoteAsync(f.UserId, card.Id, CardPlan.Media, "AGOTADO")).Error);
        Assert.NotNull((await f.Service().QuoteAsync(f.UserId, card.Id, CardPlan.Media, "VIEJO")).Error);
    }

    [Fact]
    public async Task No_se_puede_bajar_de_plan_con_la_tarjeta_vigente()
    {
        using var f = new Fixture();
        var card = f.NewCard(status: CardStatus.Published, plan: CardPlan.Pro, expires: DateTime.UtcNow.AddMonths(3));
        var q = await f.Service().QuoteAsync(f.UserId, card.Id, CardPlan.Sencilla, null);
        Assert.NotNull(q.Error);
    }

    [Fact]
    public async Task Tarjeta_suspendida_no_se_cobra()
    {
        using var f = new Fixture();
        var q = await f.Service().QuoteAsync(f.UserId, f.NewCard(status: CardStatus.Suspended).Id, CardPlan.Media, null);
        Assert.NotNull(q.Error);
    }

    [Fact]
    public async Task Tarjeta_de_otro_usuario_no_se_puede_cotizar()
    {
        using var f = new Fixture();
        var q = await f.Service().QuoteAsync(Guid.NewGuid(), f.NewCard().Id, CardPlan.Media, null);
        Assert.NotNull(q.Error);
    }
}

public class PagoTests
{
    private static async Task<Payment> Cobrar(Fixture f, Card card, CardPlan plan, string? coupon = null)
    {
        var res = await f.Service().CreateCheckoutAsync(f.UserId, "c@test.mx", card.Id, plan, coupon);
        Assert.Null(res.Error);
        return f.Db.Payments.AsNoTracking().OrderByDescending(p => p.CreatedAt).First(p => p.CardId == card.Id);
    }

    private static Task Webhook(Fixture f, Payment p, WebhookOutcome o) =>
        f.Service().HandleWebhookAsync(new WebhookResult(o, p.ProviderSessionId));

    [Fact]
    public async Task Pago_exitoso_publica_la_tarjeta_por_la_vigencia_del_plan()
    {
        using var f = new Fixture();
        f.SetMonths(Products.Cards, CardPlan.Media, 6);
        var card = f.NewCard();
        var p = await Cobrar(f, card, CardPlan.Media);
        Assert.Equal(6, p.Months);

        await Webhook(f, p, WebhookOutcome.Paid);

        var c = f.Reload(card.Id);
        Assert.Equal(CardStatus.Published, c.Status);
        Assert.Equal(CardPlan.Media, c.Plan);
        Assert.InRange(c.ExpiresAt!.Value, DateTime.UtcNow.AddMonths(6).AddMinutes(-1), DateTime.UtcNow.AddMonths(6).AddMinutes(1));
        Assert.Equal(PaymentStatus.Paid, f.ReloadPayment(p.Id).Status);
    }

    [Fact]
    public async Task Cambiar_la_vigencia_despues_no_afecta_lo_ya_cobrado()
    {
        using var f = new Fixture();
        var card = f.NewCard();
        var p = await Cobrar(f, card, CardPlan.Media);       // se cobra con 12 meses
        f.SetMonths(Products.Cards, CardPlan.Media, 1);       // el admin cambia a 1 mes
        await Webhook(f, p, WebhookOutcome.Paid);

        var c = f.Reload(card.Id);
        Assert.True(c.ExpiresAt > DateTime.UtcNow.AddMonths(11));   // conserva el año
    }

    [Fact]
    public async Task El_webhook_repetido_no_cobra_ni_extiende_dos_veces()
    {
        using var f = new Fixture();
        f.Db.Coupons.Add(new Coupon { Code = "UNA", Percent = 10 });
        f.Db.SaveChanges();
        var card = f.NewCard();
        var p = await Cobrar(f, card, CardPlan.Media, "UNA");

        await Webhook(f, p, WebhookOutcome.Paid);
        var primera = f.Reload(card.Id).ExpiresAt;
        await Webhook(f, p, WebhookOutcome.Paid);

        Assert.Equal(primera, f.Reload(card.Id).ExpiresAt);
        Assert.Equal(1, f.Db.Coupons.AsNoTracking().First(c => c.Code == "UNA").Uses);
        Assert.Equal(1, f.ReloadPayment(p.Id).ReceiptNumber);
    }

    [Fact]
    public async Task Los_folios_de_recibo_son_consecutivos()
    {
        using var f = new Fixture();
        var a = await Cobrar(f, f.NewCard(), CardPlan.Sencilla);
        var b = await Cobrar(f, f.NewCard(), CardPlan.Sencilla);
        await Webhook(f, a, WebhookOutcome.Paid);
        await Webhook(f, b, WebhookOutcome.Paid);
        Assert.Equal(1, f.ReloadPayment(a.Id).ReceiptNumber);
        Assert.Equal(2, f.ReloadPayment(b.Id).ReceiptNumber);
    }

    [Fact]
    public async Task Renovar_antes_de_vencer_suma_a_la_vigencia_restante()
    {
        using var f = new Fixture();
        var vence = DateTime.UtcNow.AddMonths(2);
        var card = f.NewCard(status: CardStatus.Published, plan: CardPlan.Media, expires: vence);
        var p = await Cobrar(f, card, CardPlan.Media);
        await Webhook(f, p, WebhookOutcome.Paid);

        var c = f.Reload(card.Id);
        Assert.InRange(c.ExpiresAt!.Value, vence.AddMonths(12).AddMinutes(-1), vence.AddMonths(12).AddMinutes(1));
    }

    [Fact]
    public async Task Renovar_vigente_nunca_baja_de_plan()
    {
        using var f = new Fixture();
        var card = f.NewCard(status: CardStatus.Published, plan: CardPlan.Pro, expires: DateTime.UtcNow.AddMonths(2));
        var p = await Cobrar(f, card, CardPlan.Pro);
        await Webhook(f, p, WebhookOutcome.Paid);
        Assert.Equal(CardPlan.Pro, f.Reload(card.Id).Plan);
    }

    [Fact]
    public async Task Un_pago_no_reactiva_una_tarjeta_suspendida()
    {
        using var f = new Fixture();
        var card = f.NewCard();
        var p = await Cobrar(f, card, CardPlan.Media);
        f.Db.Cards.Where(c => c.Id == card.Id).ExecuteUpdate(s => s.SetProperty(c => c.Status, CardStatus.Suspended));
        f.Db.ChangeTracker.Clear();   // en producción cada petición tiene su propio contexto
        await Webhook(f, p, WebhookOutcome.Paid);
        Assert.Equal(CardStatus.Suspended, f.Reload(card.Id).Status);
    }

    [Fact]
    public async Task Cupon_de_100_porciento_activa_sin_pasar_por_la_pasarela()
    {
        using var f = new Fixture();
        f.Db.Coupons.Add(new Coupon { Code = "GRATIS", Percent = 100 });
        f.Db.SaveChanges();
        var card = f.NewCard();
        var res = await f.Service().CreateCheckoutAsync(f.UserId, "c@test.mx", card.Id, CardPlan.Media, "GRATIS");
        Assert.Null(res.Error);
        Assert.Equal(CardStatus.Published, f.Reload(card.Id).Status);
    }

    [Fact]
    public async Task Fallido_y_vencido_solo_afectan_cobros_pendientes()
    {
        using var f = new Fixture();
        var a = await Cobrar(f, f.NewCard(), CardPlan.Sencilla);
        var b = await Cobrar(f, f.NewCard(), CardPlan.Sencilla);
        await Webhook(f, a, WebhookOutcome.Failed);
        await Webhook(f, b, WebhookOutcome.Paid);
        await Webhook(f, b, WebhookOutcome.Expired);   // llega tarde: no debe tumbar un pago ya cobrado

        Assert.Equal(PaymentStatus.Failed, f.ReloadPayment(a.Id).Status);
        Assert.Equal(PaymentStatus.Paid, f.ReloadPayment(b.Id).Status);
    }

    [Fact]
    public async Task OXXO_pendiente_guarda_la_referencia_y_no_activa_la_tarjeta()
    {
        using var f = new Fixture();
        f.Provider.Details = new PaymentDetails("oxxo", "OXXO", "12345678901234", "https://voucher.test/x", DateTime.UtcNow.AddDays(3));
        var card = f.NewCard();
        var p = await Cobrar(f, card, CardPlan.Media);

        await Webhook(f, p, WebhookOutcome.Awaiting);

        var pay = f.ReloadPayment(p.Id);
        Assert.Equal(PaymentStatus.Pending, pay.Status);
        Assert.Equal("oxxo", pay.MethodType);
        Assert.Equal("12345678901234", pay.OxxoReference);
        Assert.Equal("https://voucher.test/x", pay.VoucherUrl);
        Assert.Equal(CardStatus.Draft, f.Reload(card.Id).Status);
    }

    [Fact]
    public async Task OXXO_pagado_activa_la_tarjeta_y_conserva_el_metodo()
    {
        using var f = new Fixture();
        f.Provider.Details = new PaymentDetails("oxxo", "OXXO");
        var card = f.NewCard();
        var p = await Cobrar(f, card, CardPlan.Media);
        await Webhook(f, p, WebhookOutcome.Paid);

        Assert.Equal(CardStatus.Published, f.Reload(card.Id).Status);
        Assert.Equal("OXXO", f.ReloadPayment(p.Id).MethodLabel);
    }

    [Fact]
    public async Task Pago_con_tarjeta_guarda_marca_y_ultimos_4_pero_no_el_numero()
    {
        using var f = new Fixture();
        f.Provider.Details = new PaymentDetails("card", "Visa ···· 4242");
        var p = await Cobrar(f, f.NewCard(), CardPlan.Media);
        await Webhook(f, p, WebhookOutcome.Paid);
        Assert.Equal("Visa ···· 4242", f.ReloadPayment(p.Id).MethodLabel);
        Assert.Equal("card", f.ReloadPayment(p.Id).MethodType);
    }

    [Fact]
    public async Task Si_Stripe_falla_al_dar_detalles_el_pago_igual_se_aplica()
    {
        using var f = new Fixture();
        f.Provider.Details = null;
        var card = f.NewCard();
        var p = await Cobrar(f, card, CardPlan.Media);
        await Webhook(f, p, WebhookOutcome.Paid);
        Assert.Equal(CardStatus.Published, f.Reload(card.Id).Status);
        Assert.Null(f.ReloadPayment(p.Id).MethodLabel);
    }

    [Fact]
    public async Task Webhook_de_una_sesion_desconocida_se_ignora()
    {
        using var f = new Fixture();
        await f.Service().HandleWebhookAsync(new WebhookResult(WebhookOutcome.Paid, "cs_desconocida"));
        Assert.Empty(f.Db.Payments);
    }

    [Fact]
    public async Task Confirmar_consulta_al_proveedor_y_no_a_la_redireccion()
    {
        using var f = new Fixture();
        var card = f.NewCard();
        var p = await Cobrar(f, card, CardPlan.Media);

        f.Provider.Paid = false;
        var r1 = await f.Service().ConfirmAsync(p.Id, f.UserId);
        Assert.Equal(PaymentStatus.Pending, r1!.Status);

        f.Provider.Paid = true;
        var r2 = await f.Service().ConfirmAsync(p.Id, f.UserId);
        Assert.Equal(PaymentStatus.Paid, r2!.Status);
    }

    [Fact]
    public async Task Nadie_confirma_el_pago_de_otro_usuario()
    {
        using var f = new Fixture();
        var p = await Cobrar(f, f.NewCard(), CardPlan.Media);
        Assert.Null(await f.Service().ConfirmAsync(p.Id, Guid.NewGuid()));
    }
}
