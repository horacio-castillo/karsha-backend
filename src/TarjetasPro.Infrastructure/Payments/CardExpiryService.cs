using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TarjetasPro.Domain.Entities;

namespace TarjetasPro.Infrastructure.Payments;

/// <summary>Cada hora marca como Vencida toda tarjeta publicada cuyo periodo pagado terminó y manda los avisos de «por vencer» y «venció».</summary>
public class CardExpiryService(IServiceScopeFactory scopes, TarjetasPro.Infrastructure.Logging.SystemLogger syslog) : BackgroundService
{
    /// <summary>Última vuelta del proceso (para «Salud del sistema»).</summary>
    public static DateTime? LastRun { get; private set; }
    public static string? LastError { get; private set; }

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
                await scope.ServiceProvider.GetRequiredService<TarjetasPro.Infrastructure.Auth.NotificationService>().ExpiryNoticesAsync(stoppingToken);
                await syslog.PurgeAsync(stoppingToken);
                LastRun = DateTime.UtcNow; LastError = null;
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { LastRun = DateTime.UtcNow; LastError = ex.Message; await syslog.ErrorAsync("job", ex, "CardExpiryService"); /* se reintenta en la siguiente hora */ }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
