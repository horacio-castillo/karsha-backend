using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TarjetasPro.Domain.Entities;

namespace TarjetasPro.Infrastructure.Logging;

/// <summary>Guarda errores y entradas de bitácora. Nunca lanza: registrar no debe romper lo que se está registrando.</summary>
public partial class SystemLogger(IServiceScopeFactory scopes, ILogger<SystemLogger> logger)
{
    [GeneratedRegex(@"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}|\d+")]
    private static partial Regex Noise();

    private static string Cut(string? s, int max) => s is null ? "" : s.Length <= max ? s : s[..max];

    public async Task ErrorAsync(string source, string message, string? stack = null, string? path = null, string? method = null, Guid? userId = null, string? userAgent = null)
    {
        try
        {
            message = Cut(message, 1000); path = Cut(path, 300);
            var key = $"{source}|{Noise().Replace(message.Split('\n')[0], "#")}|{Noise().Replace(path, "#")}";
            var fp = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(key)));
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var now = DateTime.UtcNow;
            var row = await db.ErrorLogs.FirstOrDefaultAsync(e => e.Fingerprint == fp);
            if (row is null)
                db.ErrorLogs.Add(new ErrorLog
                {
                    Fingerprint = fp, Source = source, Message = message, Stack = Cut(stack, 8000), Path = path ?? "", Method = Cut(method, 10),
                    FirstAt = now, LastAt = now, LastUserId = userId, UserAgent = Cut(userAgent, 300),
                });
            else
            {
                row.Count++; row.LastAt = now; row.Resolved = false; row.LastUserId = userId ?? row.LastUserId;
                if (!string.IsNullOrEmpty(stack)) row.Stack = Cut(stack, 8000);
            }
            await db.SaveChangesAsync();
        }
        catch (Exception ex) { logger.LogError(ex, "No se pudo guardar el error «{Message}»", message); }
    }

    public Task ErrorAsync(string source, Exception ex, string? path = null, string? method = null, Guid? userId = null, string? userAgent = null)
        => ErrorAsync(source, $"{ex.GetType().Name}: {ex.Message}", ex.ToString(), path, method, userId, userAgent);

    public async Task AuditAsync(Guid? actorId, string actorName, string area, string action, string target, string detail, string ip)
    {
        try
        {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.AuditLogs.Add(new AuditLog { ActorId = actorId, ActorName = Cut(actorName, 150), Area = area, Action = Cut(action, 200), Target = Cut(target, 200), Detail = Cut(detail, 500), Ip = Cut(ip, 45) });
            await db.SaveChangesAsync();
        }
        catch (Exception ex) { logger.LogError(ex, "No se pudo guardar la bitácora «{Action}»", action); }
    }

    /// <summary>Errores de hace más de 30 días y bitácora de más de un año se borran solos.</summary>
    public async Task PurgeAsync(CancellationToken ct = default)
    {
        try
        {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var e = DateTime.UtcNow.AddDays(-30); var a = DateTime.UtcNow.AddYears(-1);
            await db.ErrorLogs.Where(x => x.LastAt < e).ExecuteDeleteAsync(ct);
            await db.AuditLogs.Where(x => x.At < a).ExecuteDeleteAsync(ct);
        }
        catch (Exception ex) { logger.LogError(ex, "No se pudo limpiar la bitácora"); }
    }
}
