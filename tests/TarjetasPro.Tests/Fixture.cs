using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TarjetasPro.Application.Auth;
using TarjetasPro.Infrastructure.Auth;
using TarjetasPro.Domain.Entities;
using TarjetasPro.Infrastructure;
using TarjetasPro.Infrastructure.Identity;
using TarjetasPro.Infrastructure.Payments;

namespace TarjetasPro.Tests;

/// <summary>Proveedor de pagos falso: no llama a Stripe; devuelve lo que la prueba le indique.</summary>
public class FakeProvider : IPaymentProvider
{
    public string Name => "Stripe";
    public bool Paid { get; set; }
    public PaymentDetails? Details { get; set; }
    public Task<CheckoutSession> CreateCheckoutAsync(CheckoutRequest r, CancellationToken ct = default) =>
        Task.FromResult(new CheckoutSession($"cs_{r.PaymentId:N}", "https://checkout.test/" + r.PaymentId));
    public Task<bool> IsPaidAsync(string sessionId, CancellationToken ct = default) => Task.FromResult(Paid);
    public Task<PaymentDetails?> GetDetailsAsync(string sessionId, CancellationToken ct = default) => Task.FromResult(Details);
}

/// <summary>Correo falso: las pruebas no mandan nada.</summary>
public class NoEmail : IEmailSender
{
    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default) => Task.CompletedTask;
}

/// <summary>Base SQLite en memoria con el modelo real, más un usuario, plantilla y planes ya sembrados.</summary>
public sealed class Fixture : IDisposable
{
    private readonly SqliteConnection _conn = new("DataSource=:memory:");
    public AppDbContext Db { get; }
    public FakeProvider Provider { get; } = new();
    public Guid UserId { get; } = Guid.NewGuid();
    public Guid TemplateId { get; } = Guid.NewGuid();

    public Fixture()
    {
        _conn.Open();
        Db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options);
        Db.Database.EnsureCreated();

        Db.Users.Add(new ApplicationUser { Id = UserId, UserName = "c@test.mx", NormalizedUserName = "C@TEST.MX", Email = "c@test.mx", FullName = "Cliente" });
        Db.Templates.Add(new Template { Id = TemplateId, Name = "Base", Code = "base" });
        foreach (var key in new[] { Products.Cards, Products.Catalog })
            Db.ProductSettings.Add(new ProductSetting { Key = key, Name = key, IsActive = true });
        var prices = new[] { (CardPlan.Sencilla, 8000), (CardPlan.Media, 10000), (CardPlan.Pro, 12000), (CardPlan.ProMax, 20000) };
        foreach (var prod in new[] { Products.Cards, Products.Catalog })
            foreach (var (plan, cents) in prices)
                Db.PlanSettings.Add(new PlanSetting { Product = prod, Code = plan.ToString().ToLowerInvariant(), Plan = plan, Name = plan.ToString(), PriceCents = cents, Months = 12, IsActive = true });
        Db.SaveChanges();
    }

    public PaymentService Service()
    {
        var config = new ConfigurationBuilder().Build();
        var scopes = new ServiceCollection().BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
        var syslog = new TarjetasPro.Infrastructure.Logging.SystemLogger(scopes, NullLogger<TarjetasPro.Infrastructure.Logging.SystemLogger>.Instance);
        var notify = new NotificationService(Db, new NoEmail(), config, NullLogger<NotificationService>.Instance, syslog);
        return new(Db, Provider, Options.Create(new PaymentOptions { Months = 12 }), config, notify);
    }

    public Card NewCard(string product = Products.Cards, CardStatus status = CardStatus.Draft, CardPlan plan = CardPlan.Sencilla, DateTime? expires = null)
    {
        var c = new Card { UserId = UserId, TemplateId = TemplateId, Product = product, Title = "Mi tarjeta", Slug = "s" + Guid.NewGuid().ToString("N")[..8], Status = status, Plan = plan, ExpiresAt = expires, PaidAt = expires is null ? null : DateTime.UtcNow };
        Db.Cards.Add(c);
        Db.SaveChanges();
        return c;
    }

    public void SetMonths(string product, CardPlan plan, int months)
    {
        Db.PlanSettings.Where(p => p.Product == product && p.Plan == plan).ExecuteUpdate(s => s.SetProperty(p => p.Months, months));
        Db.ChangeTracker.Clear();
    }

    public Card Reload(Guid id) { Db.ChangeTracker.Clear(); return Db.Cards.AsNoTracking().First(c => c.Id == id); }
    public Payment ReloadPayment(Guid id) { Db.ChangeTracker.Clear(); return Db.Payments.AsNoTracking().First(p => p.Id == id); }

    public void Dispose() { Db.Dispose(); _conn.Dispose(); }
}
