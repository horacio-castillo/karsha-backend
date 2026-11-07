using TarjetasPro.Application.Templates;
using TarjetasPro.Infrastructure.Auth;

namespace TarjetasPro.Api;

/// <summary>
/// Busca las ilustraciones de una profesión en wwwroot/art/&lt;profesion&gt;/ (fondo, arriba, abajo, mascota).
/// Aplica a todos los diseños de esa profesión. Ver wwwroot/art/LEEME.txt.
/// </summary>
public static class ArtCatalog
{
    private static readonly string[] Exts = [".png", ".webp", ".svg", ".jpg", ".jpeg"];

    public static TemplateRenderer.ArtPack? Find(IWebHostEnvironment env, string templateCode, string apiBase)
    {
        var code = ProfessionThemes.BaseCode(templateCode);
        var pack = new TemplateRenderer.ArtPack(Pick(env, apiBase, code, "fondo"), Pick(env, apiBase, code, "arriba"),
            Pick(env, apiBase, code, "abajo"), Pick(env, apiBase, code, "mascota"));
        return pack.Any ? pack : null;
    }

    /// <summary>Arte de la plantilla, con las piezas que el cliente armó (arriba / abajo / mascota de cualquier pack, o "none").</summary>
    public static TemplateRenderer.ArtPack? Compose(IWebHostEnvironment env, string templateCode, CardTheme? theme, string apiBase)
    {
        var basePack = Find(env, templateCode, apiBase);
        if (theme is not { ArtTop: null, ArtBottom: null, ArtMascot: null } && theme is not null)
        {
            string? Piece(string? choice, string name, string? fallback) =>
                choice is null ? fallback : choice == "none" ? null : Pick(env, apiBase, choice, name);
            var pack = new TemplateRenderer.ArtPack(basePack?.Bg, Piece(theme.ArtTop, "arriba", basePack?.Top),
                Piece(theme.ArtBottom, "abajo", basePack?.Bottom), Piece(theme.ArtMascot, "mascota", basePack?.Mascot));
            return pack.Any ? pack : null;
        }
        return basePack;
    }

    /// <summary>Packs de arte disponibles para armar una plantilla (carpetas de wwwroot/art con alguna pieza).</summary>
    public static IEnumerable<object> Packs(IWebHostEnvironment env, string apiBase)
    {
        var root = Path.Combine(env.WebRootPath ?? "", "art");
        if (!Directory.Exists(root)) yield break;
        foreach (var dir in Directory.GetDirectories(root).OrderBy(d => d, StringComparer.Ordinal))
        {
            var code = Path.GetFileName(dir);
            var top = Pick(env, apiBase, code, "arriba");
            var bottom = Pick(env, apiBase, code, "abajo");
            var mascot = Pick(env, apiBase, code, "mascota");
            if (top is null && bottom is null && mascot is null) continue;
            var label = string.Join(' ', code.Split('-').Select(w => w.Length > 0 ? char.ToUpperInvariant(w[0]) + w[1..] : w));
            yield return new { code, label, top, bottom, mascot };
        }
    }

    private static string? Pick(IWebHostEnvironment env, string apiBase, string code, string name)
    {
        if (code.Length == 0 || code.Any(c => !(char.IsAsciiLetterOrDigit(c) || c == '-'))) return null;
        var dir = Path.Combine(env.WebRootPath ?? "", "art", code);
        if (!Directory.Exists(dir)) return null;
        foreach (var ext in Exts)
        {
            var file = Path.Combine(dir, name + ext);
            if (File.Exists(file))
                return $"{apiBase}/art/{code}/{name}{ext}?v={File.GetLastWriteTimeUtc(file).Ticks}";
        }
        return null;
    }
}
