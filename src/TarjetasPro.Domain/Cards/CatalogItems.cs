using System.Text.Json;
using System.Text.Json.Serialization;

namespace TarjetasPro.Domain.Cards;

/// <summary>Un producto del catálogo. La primera foto es la principal; las demás y la descripción larga se ven en su ficha (plan Pro).</summary>
public record CatalogProduct(string Name, string Price = "", string Desc = "", string Cat = "", string[]? Photos = null, string More = "")
{
    public string MainPhoto => Photos is { Length: > 0 } p ? p[0] : "";
}

/// <summary>
/// Lista de productos guardada en el campo «catalogo». Hoy se guarda como JSON (<c>[{"name":…}]</c>);
/// los catálogos viejos son una línea por producto «Nombre | $precio | descripción | foto | categoría» y se siguen leyendo.
/// </summary>
public static class CatalogItems
{
    public const int MaxItems = 100;
    public const int MaxPhotos = 5;
    /// <summary>Tope del texto que manda el editor (100 productos con 5 fotos y descripción larga caben de sobra).</summary>
    public const int MaxRawLength = 250_000;

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        PropertyNameCaseInsensitive = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static bool IsJson(string? raw) => raw?.TrimStart().StartsWith('[') == true;

    /// <summary>Lee el JSON nuevo o el formato viejo de líneas; recorta cada campo y descarta productos sin nombre.</summary>
    public static List<CatalogProduct> Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return [];
        IEnumerable<CatalogProduct?> list;
        if (IsJson(raw))
        {
            try { list = JsonSerializer.Deserialize<CatalogProduct?[]>(raw, Json) ?? []; }
            catch (JsonException) { return []; }
        }
        else
        {
            list = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(FromLine);
        }
        return list.Select(Clean).OfType<CatalogProduct>().Take(MaxItems).ToList();
    }

    /// <summary>Texto que se guarda: siempre JSON, ya limpio.</summary>
    public static string Serialize(IEnumerable<CatalogProduct> items) =>
        JsonSerializer.Serialize(items.Select(i => i with { Photos = i.Photos is { Length: > 0 } ? i.Photos : null }), Json);

    private static CatalogProduct FromLine(string line)
    {
        var p = line.Split('|', StringSplitOptions.TrimEntries);
        string At(int i) => p.Length > i ? p[i] : "";
        return new CatalogProduct(At(0), At(1), At(2), At(4), At(3).Length > 0 ? [At(3)] : null);
    }

    private static CatalogProduct? Clean(CatalogProduct? i)
    {
        if (i is null) return null;
        var name = Cut(i.Name, 80);
        if (name.Length == 0) return null;
        var photos = (i.Photos ?? []).Select(u => (u ?? "").Trim()).Where(u => u.Length is > 0 and <= 500).Distinct().Take(MaxPhotos).ToArray();
        return new CatalogProduct(name, Cut(i.Price, 20), Cut(i.Desc, 160), Cut(i.Cat, 60), photos, Cut(i.More, 1000));
    }

    private static string Cut(string? s, int max)
    {
        s = (s ?? "").Replace("\r\n", "\n").Trim();
        return s.Length <= max ? s : s[..max].TrimEnd();
    }
}
