using System.Security.Cryptography;
using System.Text;
using Stripe;
using TarjetasPro.Infrastructure.Payments;

namespace TarjetasPro.Tests;

public class WebhookTests
{
    private const string Secret = "whsec_prueba";

    private static (string Json, string Header) Firmar(string type, string paymentStatus, string secret = Secret)
    {
        var json = "{\"id\":\"evt_1\",\"object\":\"event\",\"api_version\":\"2020-01-01\",\"created\":1,\"livemode\":false,\"type\":\"" + type +
                   "\",\"data\":{\"object\":{\"id\":\"cs_1\",\"object\":\"checkout.session\",\"payment_status\":\"" + paymentStatus + "\"}}}";
        var t = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var sig = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes($"{t}.{json}"))).ToLowerInvariant();
        return (json, $"t={t},v1={sig}");
    }

    [Theory]
    [InlineData("checkout.session.completed", "paid", WebhookOutcome.Paid)]
    [InlineData("checkout.session.completed", "unpaid", WebhookOutcome.Awaiting)]
    [InlineData("checkout.session.async_payment_succeeded", "paid", WebhookOutcome.Paid)]
    [InlineData("checkout.session.async_payment_failed", "unpaid", WebhookOutcome.Failed)]
    [InlineData("checkout.session.expired", "unpaid", WebhookOutcome.Expired)]
    public void Cada_evento_se_traduce_al_resultado_correcto(string type, string status, WebhookOutcome esperado)
    {
        var (json, header) = Firmar(type, status);
        var r = StripeWebhook.Parse(json, header, Secret);
        Assert.Equal(esperado, r.Outcome);
        Assert.Equal("cs_1", r.SessionId);
    }

    [Fact]
    public void Firma_invalida_se_rechaza()
    {
        var (json, header) = Firmar("checkout.session.completed", "paid", "otro_secreto");
        Assert.Throws<StripeException>(() => StripeWebhook.Parse(json, header, Secret));
    }

    [Fact]
    public void Cuerpo_alterado_se_rechaza()
    {
        var (json, header) = Firmar("checkout.session.completed", "unpaid");
        var alterado = json.Replace("unpaid", "paid");
        Assert.Throws<StripeException>(() => StripeWebhook.Parse(alterado, header, Secret));
    }

    [Fact]
    public void Un_evento_que_no_nos_interesa_se_ignora()
    {
        var (json, header) = Firmar("checkout.session.async_payment_succeeded", "unpaid");   // "succeeded" sin estar pagada no cuenta
        Assert.Equal(WebhookOutcome.Ignored, StripeWebhook.Parse(json, header, Secret).Outcome);
    }
}
