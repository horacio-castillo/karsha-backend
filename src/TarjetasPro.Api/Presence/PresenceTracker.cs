using System.Collections.Concurrent;

namespace TarjetasPro.Api.Presence;

/// <summary>
/// Quién está navegando ahora mismo. Vive solo en memoria (no se guarda nada en la base):
/// cada pestaña abierta avisa cada 30 s y, si deja de avisar 90 s, sale de la cuenta.
/// Al reiniciar la API empieza en cero. Sirve para saber si conviene esperar antes de publicar.
/// </summary>
public class PresenceTracker
{
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(90);
    private const int MaxTabs = 5000;

    private record Entry(DateTime Seen, string Path, string? UserId);
    private readonly ConcurrentDictionary<string, Entry> tabs = new();

    public void Ping(string tabId, string path, string? userId)
    {
        if (tabs.Count >= MaxTabs && !tabs.ContainsKey(tabId)) Sweep();
        if (tabs.Count >= MaxTabs && !tabs.ContainsKey(tabId)) return;
        tabs[tabId] = new Entry(DateTime.UtcNow, path, userId);
    }

    public void Leave(string tabId) => tabs.TryRemove(tabId, out _);

    /// <summary>Resumen sin datos personales. <paramref name="exceptUserId"/> deja fuera al admin que consulta.</summary>
    public PresenceSnapshot Snapshot(string? exceptUserId)
    {
        Sweep();
        var live = tabs.Values.Where(e => exceptUserId is null || e.UserId != exceptUserId).ToList();
        // Una persona con sesión cuenta una vez aunque tenga varias pestañas.
        var users = live.Where(e => e.UserId is not null).Select(e => e.UserId).Distinct().Count();
        var guests = live.Count(e => e.UserId is null);
        var pages = live.GroupBy(e => e.Path).Select(g => new PresencePage(g.Key, g.Count()))
            .OrderByDescending(p => p.Count).ThenBy(p => p.Path).Take(10).ToList();
        return new PresenceSnapshot(users + guests, users, guests, pages);
    }

    private void Sweep()
    {
        var limit = DateTime.UtcNow - Ttl;
        foreach (var (k, v) in tabs) if (v.Seen < limit) tabs.TryRemove(k, out _);
    }
}

public record PresencePage(string Path, int Count);
public record PresenceSnapshot(int Total, int SignedIn, int Guests, List<PresencePage> Pages);
