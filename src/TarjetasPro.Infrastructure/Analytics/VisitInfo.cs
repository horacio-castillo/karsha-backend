using System.Net;
using System.Net.Sockets;

namespace TarjetasPro.Infrastructure.Analytics;

/// <summary>Deduce el origen y el dispositivo de una visita a partir del referrer, el parámetro ?src y el user-agent.</summary>
public static class VisitInfo
{
    public static string Source(string? src, string? referrer, string? userAgent)
    {
        var s = (src ?? "").Trim().ToLowerInvariant();
        if (s is "qr") return "Código QR";
        if (s is "in") return "Navegó en KARSHA";
        if (s is "wa" or "whatsapp") return "WhatsApp";
        if (s is "fb" or "facebook") return "Facebook";
        if (s is "ig" or "instagram") return "Instagram";

        var ua = userAgent ?? "";
        // Los navegadores internos de las apps se reconocen por su user-agent aunque no manden referrer.
        if (ua.Contains("Instagram", StringComparison.OrdinalIgnoreCase)) return "Instagram";
        if (ua.Contains("FBAN", StringComparison.Ordinal) || ua.Contains("FBAV", StringComparison.Ordinal) || ua.Contains("FB_IAB", StringComparison.Ordinal)) return "Facebook";
        if (ua.Contains("WhatsApp", StringComparison.OrdinalIgnoreCase)) return "WhatsApp";
        if (ua.Contains("TikTok", StringComparison.OrdinalIgnoreCase) || ua.Contains("musical_ly", StringComparison.OrdinalIgnoreCase)) return "TikTok";
        if (ua.Contains("LinkedInApp", StringComparison.OrdinalIgnoreCase)) return "LinkedIn";

        if (!Uri.TryCreate(referrer, UriKind.Absolute, out var uri)) return "Directo";
        var h = uri.Host.ToLowerInvariant();
        if (h.StartsWith("www.")) h = h[4..];
        if (h.Contains("whatsapp") || h == "wa.me") return "WhatsApp";
        if (h.Contains("facebook") || h == "fb.com" || h == "m.me" || h.EndsWith("messenger.com")) return "Facebook";
        if (h.Contains("instagram")) return "Instagram";
        if (h.Contains("tiktok")) return "TikTok";
        if (h.Contains("linkedin") || h == "lnkd.in") return "LinkedIn";
        if (h == "t.co" || h == "x.com" || h.Contains("twitter")) return "X (Twitter)";
        if (h.Contains("google")) return "Google";
        if (h.Contains("bing") || h.Contains("yahoo") || h.Contains("duckduckgo")) return "Buscador";
        if (h.Contains("mail") || h.Contains("outlook")) return "Correo";
        if (h == "karsha.mx" || h.EndsWith(".karsha.mx")) return "Directo";
        return h.Length > 30 ? h[..30] : h;
    }

    public static string Device(string? userAgent)
    {
        var ua = userAgent ?? "";
        if (ua.Length == 0) return "Desconocido";
        string os =
            ua.Contains("iPhone") ? "iPhone" :
            ua.Contains("iPad") ? "iPad" :
            ua.Contains("Android") ? "Android" :
            ua.Contains("Windows") ? "Windows" :
            ua.Contains("Mac OS X") || ua.Contains("Macintosh") ? "Mac" :
            ua.Contains("CrOS") ? "Chromebook" :
            ua.Contains("Linux") ? "Linux" : "Otro";
        var kind =
            os is "iPad" || (os == "Android" && !ua.Contains("Mobile")) ? "Tablet" :
            os is "iPhone" or "Android" || ua.Contains("Mobile") ? "Móvil" : "Computadora";
        return $"{kind} · {os}";
    }

    /// <summary>Robots y vistas previas que ejecutan JavaScript (Googlebot, Lighthouse, navegadores sin pantalla).</summary>
    public static bool IsBot(string? userAgent)
    {
        var ua = userAgent ?? "";
        if (ua.Length == 0) return true;
        foreach (var m in new[] { "bot", "crawl", "spider", "slurp", "headless", "lighthouse", "pagespeed", "preview", "facebookexternalhit", "python", "curl", "wget" })
            if (ua.Contains(m, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    /// <summary>Deja solo la ruta (sin parámetros ni #), en minúsculas y con tope de largo.</summary>
    public static string CleanPath(string? path)
    {
        var p = (path ?? "/").Trim();
        var cut = p.IndexOfAny(['?', '#']);
        if (cut >= 0) p = p[..cut];
        if (!p.StartsWith('/')) p = "/" + p;
        p = p.ToLowerInvariant();
        if (p.Length > 1) p = p.TrimEnd('/');
        return p.Length > 120 ? p[..120] : p;
    }

    /// <summary>IP limpia y pública; las privadas o locales no se pueden ubicar.</summary>
    public static string? CleanIp(IPAddress? ip)
    {
        if (ip is null) return null;
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
        return ip.ToString();
    }

    public static bool IsPublic(string? ip)
    {
        if (!IPAddress.TryParse(ip, out var a)) return false;
        if (IPAddress.IsLoopback(a)) return false;
        if (a.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = a.GetAddressBytes();
            return !(b[0] == 10 || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) || (b[0] == 192 && b[1] == 168) || (b[0] == 169 && b[1] == 254) || b[0] == 0 || b[0] == 100 && b[1] >= 64 && b[1] <= 127);
        }
        return !(a.IsIPv6LinkLocal || a.IsIPv6SiteLocal || a.IsIPv6UniqueLocal);
    }
}
