using System.Collections.Concurrent;

namespace TarjetasPro.Api.Presence;

/// <summary>
/// Quién está navegando ahora mismo. Vive solo en memoria (no se guarda nada en la base):
/// cada pestaña abierta avisa cada 30 s y, si deja de avisar 90 s, sale de la cuenta.
/// Se cuentan personas, no pestañas: las pestañas de un mismo navegador son una sola, y se recuerda de quién es
/// ese navegador aunque el acceso de una pestaña quieta venza (dura 15 minutos).
/// Al reiniciar la API empieza en cero. Sirve para saber si conviene esperar antes de publicar.
/// </summary>
public class PresenceTracker
{
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(90);
    /// <summary>Cuánto se recuerda de quién es un navegador: lo que dura una sesión sin volver a entrar.</summary>
    private static readonly TimeSpan Memory = TimeSpan.FromDays(7);
    private const int MaxTabs = 5000;

    private record Entry(DateTime Seen, string Path, string Browser, string? UserId, bool SignedIn);
    private record Owner(string UserId, DateTime Seen);
    private readonly ConcurrentDictionary<string, Entry> tabs = new();
    private readonly ConcurrentDictionary<string, Owner> owners = new();

    /// <param name="userId">Quién es, si el aviso llegó con un acceso vigente.</param>
    /// <param name="browserId">Identifica el navegador; sin él (versiones viejas de la web) cada pestaña va sola.</param>
    /// <param name="signedIn">Si ese navegador tiene sesión abierta. En <c>false</c> se olvida de quién era.</param>
    public void Ping(string tabId, string path, string? userId, string? browserId = null, bool? signedIn = null)
    {
        if (tabs.Count >= MaxTabs && !tabs.ContainsKey(tabId)) Sweep();
        if (tabs.Count >= MaxTabs && !tabs.ContainsKey(tabId)) return;
        var browser = string.IsNullOrEmpty(browserId) ? tabId : browserId;
        var now = DateTime.UtcNow;
        if (userId is not null)
        {
            if (owners.Count < MaxTabs || owners.ContainsKey(browser)) owners[browser] = new Owner(userId, now);
        }
        else if (signedIn == false) owners.TryRemove(browser, out _);
        tabs[tabId] = new Entry(now, path, browser, userId, userId is not null || signedIn == true);
    }

    public void Leave(string tabId) => tabs.TryRemove(tabId, out _);

    /// <summary>Resumen sin datos personales. <paramref name="exceptUserId"/> deja fuera al admin que consulta.</summary>
    public PresenceSnapshot Snapshot(string? exceptUserId)
    {
        Sweep();
        var live = new List<(string Person, bool SignedIn, string Path)>();
        foreach (var browser in tabs.Values.GroupBy(e => e.Browser))
        {
            var userId = browser.Where(e => e.UserId is not null).OrderByDescending(e => e.Seen).Select(e => e.UserId).FirstOrDefault()
                         ?? (owners.TryGetValue(browser.Key, out var owner) ? owner.UserId : null);
            if (userId is not null && userId == exceptUserId) continue;
            // La misma cuenta en el celular y la computadora es una sola persona; sin sesión, cada navegador es un visitante.
            var signedIn = userId is not null || browser.Any(e => e.SignedIn);
            var person = userId is not null ? "u:" + userId : "n:" + browser.Key;
            live.AddRange(browser.Select(e => (person, signedIn, e.Path)));
        }
        var users = live.Where(x => x.SignedIn).Select(x => x.Person).Distinct().Count();
        var guests = live.Where(x => !x.SignedIn).Select(x => x.Person).Distinct().Count();
        var pages = live.GroupBy(x => x.Path).Select(g => new PresencePage(g.Key, g.Select(x => x.Person).Distinct().Count()))
            .OrderByDescending(p => p.Count).ThenBy(p => p.Path).Take(10).ToList();
        return new PresenceSnapshot(users + guests, users, guests, pages);
    }

    private void Sweep()
    {
        var now = DateTime.UtcNow;
        foreach (var (k, v) in tabs) if (v.Seen < now - Ttl) tabs.TryRemove(k, out _);
        foreach (var (k, v) in owners) if (v.Seen < now - Memory) owners.TryRemove(k, out _);
    }
}

public record PresencePage(string Path, int Count);
public record PresenceSnapshot(int Total, int SignedIn, int Guests, List<PresencePage> Pages);
