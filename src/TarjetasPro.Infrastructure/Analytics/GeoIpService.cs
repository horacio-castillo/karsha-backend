using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TarjetasPro.Infrastructure.Analytics;

/// <summary>
/// Ubica (país, estado, ciudad) la IP de cada visita en segundo plano, para no frenar la página pública.
/// Usa un servicio de geolocalización por IP (Geo:Url, por defecto ipwho.is) y guarda en memoria lo ya consultado.
/// </summary>
public class GeoIpService(IServiceScopeFactory scopes, IHttpClientFactory http, IConfiguration config, ILogger<GeoIpService> log) : BackgroundService
{
    private readonly Channel<(long VisitId, string Ip, bool Site)> queue = Channel.CreateBounded<(long, string, bool)>(new BoundedChannelOptions(5000) { FullMode = BoundedChannelFullMode.DropOldest });
    private readonly ConcurrentDictionary<string, Geo?> cache = new();
    private string UrlFor(string ip) => (config["Geo:Url"] ?? "https://ipwho.is/{ip}?fields=success,country,region,city&lang=es").Replace("{ip}", Uri.EscapeDataString(ip));

    public record Geo(string? Country, string? Region, string? City);
    private record WhoIs([property: JsonPropertyName("success")] bool Success, [property: JsonPropertyName("country")] string? Country,
        [property: JsonPropertyName("region")] string? Region, [property: JsonPropertyName("city")] string? City);

    /// <summary>Encola la visita para ubicarla; no bloquea.</summary>
    public void Enqueue(long visitId, string? ip, bool site = false)
    {
        if (string.Equals(config["Geo:Enabled"], "false", StringComparison.OrdinalIgnoreCase)) return;
        if (VisitInfo.IsPublic(ip)) queue.Writer.TryWrite((visitId, ip!, site));
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await foreach (var (id, ip, site) in queue.Reader.ReadAllAsync(ct))
        {
            try
            {
                var geo = await LookupAsync(ip, ct);
                if (geo is null) continue;
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                string? country = Cut(geo.Country, 60), region = Cut(geo.Region, 80), city = Cut(geo.City, 80);
                if (site)
                    await db.SiteVisits.Where(v => v.Id == id).ExecuteUpdateAsync(s => s
                        .SetProperty(v => v.Country, country).SetProperty(v => v.Region, region).SetProperty(v => v.City, city), ct);
                else
                    await db.CardVisits.Where(v => v.Id == id).ExecuteUpdateAsync(s => s
                        .SetProperty(v => v.Country, country).SetProperty(v => v.Region, region).SetProperty(v => v.City, city), ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { log.LogWarning(ex, "No se pudo ubicar la IP de la visita {Id}", id); }
        }
    }

    private async Task<Geo?> LookupAsync(string ip, CancellationToken ct)
    {
        if (cache.TryGetValue(ip, out var hit)) return hit;
        var client = http.CreateClient(nameof(GeoIpService));
        var r = await client.GetFromJsonAsync<WhoIs>(UrlFor(ip), ct);
        var geo = r is { Success: true } ? new Geo(r.Country, r.Region, r.City) : null;
        if (cache.Count > 20000) cache.Clear();
        cache[ip] = geo;
        return geo;
    }

    private static string? Cut(string? s, int n) => string.IsNullOrWhiteSpace(s) ? null : s.Length > n ? s[..n] : s;
}
