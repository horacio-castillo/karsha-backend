using System.Globalization;
using System.Text.RegularExpressions;

namespace TarjetasPro.Application.Templates;

/// <summary>Personalización del plan Media: el cliente nunca escribe HTML/CSS, solo elige valores validados.</summary>
public record CardTheme(string? Accent = null, string? Accent2 = null, string? Text = null, string? Bg = null,
    string? Font = null, string? BgImage = null, string[]? Hidden = null,
    string? Decor = null, string? Buttons = null, string? Photo = null,
    string[]? Order = null, string? ButtonsLayout = null, string? GalleryLayout = null, int? DecorCount = null,
    Sticker[]? Stickers = null, bool? HideIcons = null, string? TextAlign = null, int? ArtHue = null,
    string? ArtTop = null, string? ArtBottom = null, string? ArtMascot = null, int? BgLight = null);

/// <summary>Icono colocado por el cliente: X en % del ancho de la tarjeta, Y en px desde arriba, tamaño en px, giro en grados.</summary>
/// <remarks>Img: logo propio subido por el cliente (/uploads/…); si existe se dibuja en vez del emoji y S es su ancho en px.</remarks>
public record Sticker(string E, double X, double Y, int S = 44, int R = 0, string St = "sombra", string? Img = null);

/// <summary>Colores base de una plantilla que el cliente puede cambiar (detectados de su CSS).</summary>
public record TemplateKnobs(string? Accent, string? Accent2, string? Text);

public static partial class ThemeRules
{
    /// <summary>Tipografías permitidas (clave → pila CSS). Las del sistema y las web de WebFontQuery.</summary>
    public static readonly IReadOnlyDictionary<string, string> Fonts = new Dictionary<string, string>
    {
        ["sistema"] = "system-ui,-apple-system,'Segoe UI',Roboto,sans-serif",
        ["elegante"] = "Georgia,'Times New Roman',serif",
        ["moderna"] = "'Trebuchet MS','Segoe UI',Verdana,sans-serif",
        ["redonda"] = "'Arial Rounded MT Bold','Helvetica Rounded',Arial,sans-serif",
        ["maquina"] = "'Courier New',ui-monospace,monospace",
        ["merriweather"] = "'Merriweather',Georgia,serif",
        ["playfair"] = "'Playfair Display',Georgia,serif",
        ["lora"] = "'Lora',Georgia,serif",
        ["poppins"] = "'Poppins',system-ui,sans-serif",
        ["montserrat"] = "'Montserrat',system-ui,sans-serif",
        ["nunito"] = "'Nunito',system-ui,sans-serif",
        ["oswald"] = "'Oswald',Impact,sans-serif",
        ["fredoka"] = "'Fredoka',system-ui,sans-serif",
        ["baloo"] = "'Baloo 2',system-ui,sans-serif",
        ["pacifico"] = "'Pacifico',cursive",
        ["rajdhani"] = "'Rajdhani',system-ui,sans-serif",
    };

    /// <summary>Fuentes web (Google Fonts): la CSP ya permite hojas de estilo y fuentes por https.</summary>
    public static readonly IReadOnlyDictionary<string, string> WebFontQuery = new Dictionary<string, string>
    {
        ["merriweather"] = "Merriweather:wght@400;700", ["playfair"] = "Playfair+Display:wght@400;700",
        ["lora"] = "Lora:wght@400;600", ["poppins"] = "Poppins:wght@400;600;700",
        ["montserrat"] = "Montserrat:wght@400;600;800", ["nunito"] = "Nunito:wght@400;700;800",
        ["oswald"] = "Oswald:wght@500;700", ["fredoka"] = "Fredoka:wght@400;600;700",
        ["baloo"] = "Baloo+2:wght@500;700;800", ["pacifico"] = "Pacifico", ["rajdhani"] = "Rajdhani:wght@500;600;700",
    };

    public static readonly string[] ButtonStyles = ["redondo", "suave", "cuadrado", "borde", "sombra"];
    public const int MaxDecor = 8;

    /// <summary>Cada "capa" repite la decoración espejada o más chica; más capas = más adornos en la tarjeta.</summary>
    public static readonly string[] DecorLayers =
    [
        "", "translate(400 0) scale(-1 1)", "translate(0 800) scale(1 -1)", "translate(400 800) scale(-1 -1)",
        "translate(0 230) scale(.5)", "translate(400 230) scale(-.5 .5)",
        "translate(0 570) scale(.5 -.5)", "translate(400 570) scale(-.5 -.5)",
    ];

    public const int MaxStickers = 12;
    /// <summary>Alineación del texto: clave del cliente → valor CSS.</summary>
    public static readonly IReadOnlyDictionary<string, string> TextAligns = new Dictionary<string, string>
    {
        ["izquierda"] = "left", ["centro"] = "center", ["derecha"] = "right", ["justificado"] = "justify",
    };
    public static readonly string[] StickerStyles = ["sombra", "borde", "ninguno"];

    /// <summary>Un emoji (símbolos y secuencias ZWJ): nunca letras, dígitos ni signos ASCII.</summary>
    private static bool IsEmoji(string e) =>
        e is { Length: > 0 and <= 16 } && e.EnumerateRunes().All(r => !r.IsAscii && r.Value > 0x2000 &&
            System.Text.Rune.GetUnicodeCategory(r) is UnicodeCategory.OtherSymbol or UnicodeCategory.ModifierSymbol
                or UnicodeCategory.NonSpacingMark or UnicodeCategory.Format or UnicodeCategory.MathSymbol or UnicodeCategory.OtherNotAssigned);

    /// <summary>Pieza de arte elegida: "none" (sin pieza) o el código de un pack (solo letras, números y guiones).</summary>
    private static string? CleanArt(string? a) =>
        a is { Length: > 0 and <= 40 } && a.All(c => char.IsAsciiLetterOrDigit(c) || c == '-') ? a : null;

    private static bool IsUpload(string? u) =>
        u is { Length: > 0 and <= 500 } && u.StartsWith("/uploads/", StringComparison.Ordinal) && !u.Contains("..");

    private static Sticker[]? CleanStickers(Sticker[]? list) =>
        list?.Where(s => s is not null && (IsEmoji(s.E) || IsUpload(s.Img))).Take(MaxStickers)
            .Select(s => new Sticker(IsEmoji(s.E) ? s.E : "🖼", Math.Round(Math.Clamp(double.IsFinite(s.X) ? s.X : 50, 3, 97), 1),
                Math.Round(Math.Clamp(double.IsFinite(s.Y) ? s.Y : 0, 0, 2400)),
                Math.Clamp(s.S, 20, IsUpload(s.Img) ? 240 : 120), Math.Clamp(s.R, -180, 180),
                StickerStyles.Contains(s.St) ? s.St : "sombra", IsUpload(s.Img) ? s.Img : null)).ToArray();

    public static readonly string[] ButtonLayouts = ["lista", "cuadricula", "compacta"];
    public static readonly string[] GalleryLayouts = ["cuadricula", "tres", "tira", "grande"];
    public static readonly string[] PhotoStyles = ["circulo", "suave", "cuadrado", "marco", "ancho"];

    /// <summary>Decoraciones vectoriales (clave → SVG). Usan currentColor y --c2; las dibuja el sistema, el cliente solo elige.</summary>
    public static readonly IReadOnlyDictionary<string, string> Decors = new Dictionary<string, string>
    {
        ["burbujas"] = "<circle cx='40' cy='60' r='30'/><circle cx='92' cy='122' r='14' style='fill:var(--c2)'/><circle cx='352' cy='90' r='22' style='fill:var(--c2)'/><circle cx='372' cy='168' r='10'/>" +
            "<circle cx='30' cy='704' r='26' style='fill:var(--c2)'/><circle cx='82' cy='764' r='12'/><circle cx='340' cy='722' r='34'/><circle cx='298' cy='780' r='14' style='fill:var(--c2)'/>",
        ["ondas"] = "<path d='M0 0H400V46Q300 92 200 46T0 46Z'/><path d='M0 764Q100 716 200 764T400 764V800H0Z' style='fill:var(--c2)'/><path d='M0 784Q100 744 200 784T400 784V800H0Z'/>",
        ["lineas"] = "<g fill='none' stroke='currentColor' stroke-width='3' stroke-linecap='round'><path d='M0 70L70 0M0 110L110 0M0 150L150 0'/><path d='M400 730L330 800M400 690L290 800M400 650L250 800' style='stroke:var(--c2)'/></g>",
        ["circulos"] = "<g fill='none' stroke-width='4'><g stroke='currentColor'><circle cx='400' cy='0' r='60'/><circle cx='400' cy='0' r='100'/><circle cx='400' cy='0' r='140'/></g>" +
            "<g style='stroke:var(--c2)'><circle cx='0' cy='800' r='60'/><circle cx='0' cy='800' r='100'/><circle cx='0' cy='800' r='140'/></g></g>",
        ["confeti"] = "<rect x='40' y='50' width='16' height='8' transform='rotate(25 48 54)'/><circle cx='120' cy='30' r='6' style='fill:var(--c2)'/><rect x='340' y='60' width='14' height='8' transform='rotate(-30 347 64)' style='fill:var(--c2)'/>" +
            "<circle cx='370' cy='140' r='7'/><polygon points='30,150 44,150 37,138'/><rect x='24' y='690' width='14' height='8' transform='rotate(-20 31 694)' style='fill:var(--c2)'/>" +
            "<circle cx='90' cy='770' r='7'/><polygon points='350,700 364,700 357,688' style='fill:var(--c2)'/><rect x='320' y='770' width='16' height='8' transform='rotate(35 328 774)'/><circle cx='380' cy='740' r='5'/>",
        ["formas"] = "<polygon points='0,0 110,0 0,110'/><rect x='320' y='40' width='44' height='44' transform='rotate(20 342 62)' style='fill:var(--c2)'/>" +
            "<polygon points='400,800 290,800 400,690' style='fill:var(--c2)'/><rect x='30' y='700' width='38' height='38' transform='rotate(-18 49 719)'/>",
    };

    [GeneratedRegex("^#[0-9a-fA-F]{6}$")]
    private static partial Regex HexRx();

    [GeneratedRegex(@"#[0-9a-fA-F]{6}\b")]
    private static partial Regex AnyHexRx();

    [GeneratedRegex(@"body\{[^}]*?(?:^|[;{\s])color:\s*(#[0-9a-fA-F]{6})", RegexOptions.IgnoreCase)]
    private static partial Regex BodyColorRx();

    public static bool IsHex(string? s) => s is not null && HexRx().IsMatch(s);

    public static TemplateKnobs Detect(string css)
    {
        var counts = AnyHexRx().Matches(css).Select(m => m.Value.ToLowerInvariant())
            .Where(Colorful).GroupBy(h => h).OrderByDescending(g => g.Count()).Select(g => g.Key).ToList();
        var text = BodyColorRx().Match(css) is { Success: true } m ? m.Groups[1].Value.ToLowerInvariant() : null;
        return new TemplateKnobs(counts.ElementAtOrDefault(0), counts.ElementAtOrDefault(1), text);
    }

    private static bool Colorful(string hex)
    {
        var r = int.Parse(hex[1..3], NumberStyles.HexNumber) / 255.0;
        var g = int.Parse(hex[3..5], NumberStyles.HexNumber) / 255.0;
        var b = int.Parse(hex[5..7], NumberStyles.HexNumber) / 255.0;
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var l = (max + min) / 2;
        var s = max == min ? 0 : (max - min) / (1 - Math.Abs(2 * l - 1));
        return s >= 0.3 && l is >= 0.15 and <= 0.88;
    }

    /// <summary>Descarta cualquier valor inválido y los campos ocultos que la plantilla no usa.</summary>
    public static CardTheme Sanitize(CardTheme? t, IEnumerable<string> templateFields, bool pro = false)
    {
        if (t is null) return new CardTheme();
        var fields = templateFields.ToHashSet();
        string? Hex(string? v) => IsHex(v) ? v!.ToLowerInvariant() : null;
        var bgImage = t.BgImage is { Length: > 0 and <= 500 } u
            && (u.StartsWith("/uploads/", StringComparison.Ordinal) && !u.Contains("..") || u.StartsWith("https://", StringComparison.Ordinal))
            ? u : null;
        return new CardTheme(Hex(t.Accent), Hex(t.Accent2), Hex(t.Text), Hex(t.Bg),
            t.Font is not null && Fonts.ContainsKey(t.Font) ? t.Font : null,
            bgImage,
            t.Hidden?.Where(fields.Contains).Distinct().ToArray(),
            t.Decor is not null && Decors.ContainsKey(t.Decor) ? t.Decor : null,
            t.Buttons is not null && ButtonStyles.Contains(t.Buttons) ? t.Buttons : null,
            t.Photo is not null && PhotoStyles.Contains(t.Photo) ? t.Photo : null,
            // Ordenar y elegir disposición es del plan Pro.
            pro ? t.Order?.Where(k => k is { Length: > 0 and <= 30 } && k.All(c => char.IsAsciiLetterOrDigit(c) || c == '_')).Distinct().Take(20).ToArray() : null,
            pro && t.ButtonsLayout is not null && ButtonLayouts.Contains(t.ButtonsLayout) ? t.ButtonsLayout : null,
            pro && t.GalleryLayout is not null && GalleryLayouts.Contains(t.GalleryLayout) ? t.GalleryLayout : null,
            t.Decor is not null && Decors.ContainsKey(t.Decor) ? Math.Clamp(t.DecorCount ?? 1, 1, MaxDecor) : null,
            CleanStickers(t.Stickers) is { Length: > 0 } st ? st : null,
            t.HideIcons == true ? true : null,
            t.TextAlign is not null && TextAligns.ContainsKey(t.TextAlign) ? t.TextAlign : null,
            t.ArtHue is { } hue && hue != 0 ? Math.Clamp(hue, 0, 360) : null,
            CleanArt(t.ArtTop), CleanArt(t.ArtBottom), CleanArt(t.ArtMascot),
            bgImage is not null && t.BgLight is { } bl && bl > 0 ? Math.Clamp(bl, 1, 85) : null);
    }
}
