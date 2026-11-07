using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TarjetasPro.Domain.Entities;

namespace TarjetasPro.Infrastructure.Payments;

/// <summary>Cada hora marca como Vencida toda tarjeta publicada cuyo periodo pagado terminó.</summary>
public class CardExpiryService(IServiceScopeFactory scopes) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var now = DateTime.UtcNow;
                await db.Cards.Where(c => c.Status == CardStatus.Published && c.ExpiresAt != null && c.ExpiresAt <= now)
                    .ExecuteUpdateAsync(s => s.SetProperty(c => c.Status, CardStatus.Expired)
                                              .SetProperty(c => c.UpdatedAt, now), stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch { /* se reintenta en la siguiente hora */ }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
