using System.Text.RegularExpressions;
using TarjetasPro.Domain.Cards;

namespace TarjetasPro.Application.Templates;

/// <summary>
/// Render de plantillas tipo Mustache: {{campo}}, {{#campo}}...{{/campo}} (si tiene valor)
/// y {{^campo}}...{{/campo}} (si está vacío). En campos de tipo lista, {{#campo}}...{{/campo}}
/// se repite por cada elemento y {{.}} es el elemento. Todo valor del usuario se sanea y escapa.
/// </summary>
public static partial class TemplateRenderer
{
    private const string Csp =
        "default-src 'none'; img-src https: http: data:; style-src 'unsafe-inline' https:; font-src https: data:";

    private const string FrameSrc = "; frame-src https://www.youtube-nocookie.com";

    private const string Watermark =
        "<div style=\"position:fixed;left:0;right:0;bottom:0;z-index:99999;padding:8px;text-align:center;" +
        "background:rgba(220,38,38,.92);color:#fff;font:600 13px system-ui,sans-serif\">" +
        "VISTA PREVIA · Publica tu tarjeta para activar tu enlace</div>";

    [GeneratedRegex(@"\{\{\s*([#^])\s*(\w+)\s*\}\}((?:(?!\{\{\s*[#^]\s*\2\s*\}\}).)*?)\{\{\s*/\s*\2\s*\}\}", RegexOptions.Singleline)]
    private static partial Regex SectionRx();

    [GeneratedRegex(@"\{\{\s*(\w+)\s*\}\}")]
    private static partial Regex VarRx();

    [GeneratedRegex(@"\{\{\s*\.\s*\}\}")]
    private static partial Regex ItemRx();

    [GeneratedRegex(@"\{\{\s*[#^]?\s*(\w+)\s*\}\}")]
    private static partial Regex UsedRx();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRx();

    /// <summary>{{#has_servicios}} equivale a "si hay servicios", sin repetir el bloque por elemento.</summary>
    private const string HasPrefix = "has_";
    private static string BaseKey(string key) => key.StartsWith(HasPrefix, StringComparison.Ordinal) ? key[HasPrefix.Length..] : key;

    public static string[] ExtractFields(string html) =>
        UsedRx().Matches(ExpandAddresses(html)).Select(m => BaseKey(m.Groups[1].Value))
            .Where(CardFields.ByKey.ContainsKey).Distinct().ToArray();

    [GeneratedRegex(@"\{\{#ubicacion\}\}((?:(?!\{\{#ubicacion\}\}).)*?)\{\{/ubicacion\}\}", RegexOptions.Singleline)]
    private static partial Regex AddressBlockRx();

    [GeneratedRegex(@"<h2[^>]*>.*?</h2>", RegexOptions.Singleline)]
    private static partial Regex HeadingRx();

    /// <summary>
    /// Las plantillas escriben el bloque de dirección una sola vez ({{#ubicacion}}…{{/ubicacion}}); aquí se repite
    /// para las direcciones 2 y 3 (sin repetir el título de la sección). Las invitaciones no se expanden: tienen un solo lugar.
    /// </summary>
    public static string ExpandAddresses(string html)
    {
        if (html.Contains("{{evento}}") || html.Contains("{{#evento}}") || html.Contains("{{invitado}}")) return html;
        return AddressBlockRx().Replace(html, m =>
        {
            var inner = m.Groups[1].Value;
            var extra = "";
            foreach (var n in new[] { "2", "3" })
            {
                var copy = inner.Replace("{{ubicacion}}", "{{ubicacion" + n + "}}").Replace("{{#mapa}}", "{{#mapa" + n + "}}")
                    .Replace("{{/mapa}}", "{{/mapa" + n + "}}").Replace("{{mapa}}", "{{mapa" + n + "}}");
                copy = HeadingRx().Replace(copy, "", 1);
                extra += "{{#ubicacion" + n + "}}" + copy + "{{/ubicacion" + n + "}}";
            }
            return m.Value + extra;
        });
    }

    [GeneratedRegex(@"\{\{\s*#\s*(\w+)\s*\}\}\s*<section")]
    private static partial Regex SectionKeyRx();

    /// <summary>Secciones reordenables de la plantilla, en el orden en que aparecen.</summary>
    public static string[] SectionKeys(string html) =>
        SectionKeyRx().Matches(html).Select(m => BaseKey(m.Groups[1].Value)).Distinct().ToArray();

    public static string[] UnknownFields(string html) =>
        UsedRx().Matches(html).Select(m => BaseKey(m.Groups[1].Value))
            .Where(k => !CardFields.ByKey.ContainsKey(k)).Distinct().ToArray();

    /// <summary>Funciones por plan: tema (Media), galería/video y rastreo de clics (Pro).</summary>
    public record Extras(CardTheme? Theme = null, string[]? Gallery = null, string? VideoUrl = null, string? TrackBase = null, ArtPack? Art = null, bool Embed = false);

    /// <summary>Ilustraciones de la profesión (URLs ya resueltas por el servidor): fondo, esquina superior, inferior y mascota.</summary>
    public record ArtPack(string? Bg = null, string? Top = null, string? Bottom = null, string? Mascot = null)
    {
        public bool Any => Bg is not null || Top is not null || Bottom is not null || Mascot is not null;
    }

    public static string Render(string html, string css, IReadOnlyDictionary<string, string>? data,
        string assetBase = "", bool watermark = false, Extras? extras = null)
    {
        assetBase = assetBase.TrimEnd('/');
        if (extras?.Theme?.Hidden is { Length: > 0 } hidden && data is not null)
            data = data.Where(kv => !hidden.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);
        var (clean, lists) = Sanitize(data, assetBase);

        var body = ExpandAddresses(html);
        for (var i = 0; i < 10; i++)
        {
            var next = SectionRx().Replace(body, m =>
            {
                var key = m.Groups[2].Value;
                var show = m.Groups[1].Value == "#";
                var isHas = key.StartsWith(HasPrefix, StringComparison.Ordinal);
                if (isHas) key = BaseKey(key);
                if (!isHas && lists.TryGetValue(key, out var items))
                    return show ? string.Concat(items.Select(it => ItemRx().Replace(m.Groups[3].Value, _ => it))) : "";

                var has = clean.TryGetValue(key, out var v) && v.Length > 0;
                if (show != has) return "";
                var inner = m.Groups[3].Value;
                // Las <section> de la plantilla se marcan con su campo para poder reordenarlas (plan Pro).
                return show && inner.TrimStart().StartsWith("<section", StringComparison.Ordinal) && !inner.Contains("data-sec=")
                    ? SectionOpenRx().Replace(inner, $"<section data-sec=\"{key}\"", 1)
                    : inner;
            });
            if (next == body) break;
            body = next;
        }

        body = VarRx().Replace(body, m =>
            clean.TryGetValue(m.Groups[1].Value, out var v) ? v : "");
        body = ItemRx().Replace(body, "");

        if (extras?.TrackBase is { } track) body = TrackLinks(body, clean, track);
        var (galInner, vidInner) = extras is null ? ("", "") : BuildGallery(extras, assetBase);
        var hasExtras = galInner.Length + vidInner.Length > 0;
        var placed = false;
        if (hasExtras || extras?.Theme?.Order is { Length: > 0 })
            (body, placed) = Arrange(body, galInner, vidInner, extras?.Theme?.Order);
        var gallery = hasExtras && !placed ? $"<div class=\"tp-extra\">{galInner}{vidInner}</div>" : "";
        var decor = extras?.Theme is { Decor: { } d } th ? BuildDecor(d, th, css) : "";
        var stickers = extras?.Theme?.Stickers is { Length: > 0 } stk ? BuildStickers(stk, assetBase) : "";
        var art = extras?.Art is { Any: true } ap ? BuildArt(ap) : "";
        if (art.Length > 0) css += ArtCss(extras!.Art!);
        // Las plantillas del catálogo (.page) pintan su propio fondo: el arte va después para quedar encima y bajo el contenido (z-index 1).
        var artOver = body.Contains("class=\"page", StringComparison.Ordinal);
        var (artBefore, artAfter) = artOver ? ("", art) : (art, "");
        css = extras?.Theme is { } theme ? ApplyTheme(css, theme, assetBase) : css;
        css += SocialIconsCss;
        if (hasExtras) css += (placed ? "" : WrapCss) + GalleryCss + GalleryLayoutCss(extras?.Theme?.GalleryLayout);

        return $"""
            <!doctype html>
            <html lang="es"><head><meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <meta http-equiv="Content-Security-Policy" content="{Csp}{(extras?.Embed == true ? FrameSrc : "")}">
            <base target="_blank">
            <style>{css}</style></head>
            <body>{artBefore}{decor}{stickers}{body}{artAfter}{gallery}{(watermark ? Watermark : "")}</body></html>
            """;
    }

    /// <summary>Iconos de marca (Simple Icons, CC0) en los botones de WhatsApp, Instagram y Facebook; toman el color del texto.</summary>
    private const string SocialIconsCss =
        """
        :is(a.btn,a.link,a.cta):is([href*="wa.me"],[href*="instagram.com"],[href*="facebook.com"])::before,a.pill:is([href*="wa.me"],[href*="instagram.com"],[href*="facebook.com"]) .pi::before{content:"";display:inline-block;width:1.15em;height:1.15em;margin-right:.5em;vertical-align:-.2em;background:currentColor;-webkit-mask-repeat:no-repeat;mask-repeat:no-repeat;-webkit-mask-position:center;mask-position:center;-webkit-mask-size:contain;mask-size:contain}
        :is(a.btn,a.link,a.cta)[href*="wa.me"]::before,a.pill[href*="wa.me"] .pi::before{-webkit-mask-image:url("data:image/svg+xml,%3Csvg xmlns=%22http://www.w3.org/2000/svg%22 viewBox=%220 0 24 24%22%3E%3Cpath d=%22M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347m-5.421 7.403h-.004a9.87 9.87 0 01-5.031-1.378l-.361-.214-3.741.982.998-3.648-.235-.374a9.86 9.86 0 01-1.51-5.26c.001-5.45 4.436-9.884 9.888-9.884 2.64 0 5.122 1.03 6.988 2.898a9.825 9.825 0 012.893 6.994c-.003 5.45-4.437 9.884-9.885 9.884m8.413-18.297A11.815 11.815 0 0012.05 0C5.495 0 .16 5.335.157 11.892c0 2.096.547 4.142 1.588 5.945L.057 24l6.305-1.654a11.882 11.882 0 005.683 1.448h.005c6.554 0 11.89-5.335 11.893-11.893a11.821 11.821 0 00-3.48-8.413Z%22/%3E%3C/svg%3E");mask-image:url("data:image/svg+xml,%3Csvg xmlns=%22http://www.w3.org/2000/svg%22 viewBox=%220 0 24 24%22%3E%3Cpath d=%22M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347m-5.421 7.403h-.004a9.87 9.87 0 01-5.031-1.378l-.361-.214-3.741.982.998-3.648-.235-.374a9.86 9.86 0 01-1.51-5.26c.001-5.45 4.436-9.884 9.888-9.884 2.64 0 5.122 1.03 6.988 2.898a9.825 9.825 0 012.893 6.994c-.003 5.45-4.437 9.884-9.885 9.884m8.413-18.297A11.815 11.815 0 0012.05 0C5.495 0 .16 5.335.157 11.892c0 2.096.547 4.142 1.588 5.945L.057 24l6.305-1.654a11.882 11.882 0 005.683 1.448h.005c6.554 0 11.89-5.335 11.893-11.893a11.821 11.821 0 00-3.48-8.413Z%22/%3E%3C/svg%3E")}
        :is(a.btn,a.link,a.cta)[href*="instagram.com"]::before,a.pill[href*="instagram.com"] .pi::before{-webkit-mask-image:url("data:image/svg+xml,%3Csvg xmlns=%22http://www.w3.org/2000/svg%22 viewBox=%220 0 24 24%22%3E%3Cpath d=%22M7.0301.084c-1.2768.0602-2.1487.264-2.911.5634-.7888.3075-1.4575.72-2.1228 1.3877-.6652.6677-1.075 1.3368-1.3802 2.127-.2954.7638-.4956 1.6365-.552 2.914-.0564 1.2775-.0689 1.6882-.0626 4.947.0062 3.2586.0206 3.6671.0825 4.9473.061 1.2765.264 2.1482.5635 2.9107.308.7889.72 1.4573 1.388 2.1228.6679.6655 1.3365 1.0743 2.1285 1.38.7632.295 1.6361.4961 2.9134.552 1.2773.056 1.6884.069 4.9462.0627 3.2578-.0062 3.668-.0207 4.9478-.0814 1.28-.0607 2.147-.2652 2.9098-.5633.7889-.3086 1.4578-.72 2.1228-1.3881.665-.6682 1.0745-1.3378 1.3795-2.1284.2957-.7632.4966-1.636.552-2.9124.056-1.2809.0692-1.6898.063-4.948-.0063-3.2583-.021-3.6668-.0817-4.9465-.0607-1.2797-.264-2.1487-.5633-2.9117-.3084-.7889-.72-1.4568-1.3876-2.1228C21.2982 1.33 20.628.9208 19.8378.6165 19.074.321 18.2017.1197 16.9244.0645 15.6471.0093 15.236-.005 11.977.0014 8.718.0076 8.31.0215 7.0301.0839m.1402 21.6932c-1.17-.0509-1.8053-.2453-2.2287-.408-.5606-.216-.96-.4771-1.3819-.895-.422-.4178-.6811-.8186-.9-1.378-.1644-.4234-.3624-1.058-.4171-2.228-.0595-1.2645-.072-1.6442-.079-4.848-.007-3.2037.0053-3.583.0607-4.848.05-1.169.2456-1.805.408-2.2282.216-.5613.4762-.96.895-1.3816.4188-.4217.8184-.6814 1.3783-.9003.423-.1651 1.0575-.3614 2.227-.4171 1.2655-.06 1.6447-.072 4.848-.079 3.2033-.007 3.5835.005 4.8495.0608 1.169.0508 1.8053.2445 2.228.408.5608.216.96.4754 1.3816.895.4217.4194.6816.8176.9005 1.3787.1653.4217.3617 1.056.4169 2.2263.0602 1.2655.0739 1.645.0796 4.848.0058 3.203-.0055 3.5834-.061 4.848-.051 1.17-.245 1.8055-.408 2.2294-.216.5604-.4763.96-.8954 1.3814-.419.4215-.8181.6811-1.3783.9-.4224.1649-1.0577.3617-2.2262.4174-1.2656.0595-1.6448.072-4.8493.079-3.2045.007-3.5825-.006-4.848-.0608M16.953 5.5864A1.44 1.44 0 1 0 18.39 4.144a1.44 1.44 0 0 0-1.437 1.4424M5.8385 12.012c.0067 3.4032 2.7706 6.1557 6.173 6.1493 3.4026-.0065 6.157-2.7701 6.1506-6.1733-.0065-3.4032-2.771-6.1565-6.174-6.1498-3.403.0067-6.156 2.771-6.1496 6.1738M8 12.0077a4 4 0 1 1 4.008 3.9921A3.9996 3.9996 0 0 1 8 12.0077%22/%3E%3C/svg%3E");mask-image:url("data:image/svg+xml,%3Csvg xmlns=%22http://www.w3.org/2000/svg%22 viewBox=%220 0 24 24%22%3E%3Cpath d=%22M7.0301.084c-1.2768.0602-2.1487.264-2.911.5634-.7888.3075-1.4575.72-2.1228 1.3877-.6652.6677-1.075 1.3368-1.3802 2.127-.2954.7638-.4956 1.6365-.552 2.914-.0564 1.2775-.0689 1.6882-.0626 4.947.0062 3.2586.0206 3.6671.0825 4.9473.061 1.2765.264 2.1482.5635 2.9107.308.7889.72 1.4573 1.388 2.1228.6679.6655 1.3365 1.0743 2.1285 1.38.7632.295 1.6361.4961 2.9134.552 1.2773.056 1.6884.069 4.9462.0627 3.2578-.0062 3.668-.0207 4.9478-.0814 1.28-.0607 2.147-.2652 2.9098-.5633.7889-.3086 1.4578-.72 2.1228-1.3881.665-.6682 1.0745-1.3378 1.3795-2.1284.2957-.7632.4966-1.636.552-2.9124.056-1.2809.0692-1.6898.063-4.948-.0063-3.2583-.021-3.6668-.0817-4.9465-.0607-1.2797-.264-2.1487-.5633-2.9117-.3084-.7889-.72-1.4568-1.3876-2.1228C21.2982 1.33 20.628.9208 19.8378.6165 19.074.321 18.2017.1197 16.9244.0645 15.6471.0093 15.236-.005 11.977.0014 8.718.0076 8.31.0215 7.0301.0839m.1402 21.6932c-1.17-.0509-1.8053-.2453-2.2287-.408-.5606-.216-.96-.4771-1.3819-.895-.422-.4178-.6811-.8186-.9-1.378-.1644-.4234-.3624-1.058-.4171-2.228-.0595-1.2645-.072-1.6442-.079-4.848-.007-3.2037.0053-3.583.0607-4.848.05-1.169.2456-1.805.408-2.2282.216-.5613.4762-.96.895-1.3816.4188-.4217.8184-.6814 1.3783-.9003.423-.1651 1.0575-.3614 2.227-.4171 1.2655-.06 1.6447-.072 4.848-.079 3.2033-.007 3.5835.005 4.8495.0608 1.169.0508 1.8053.2445 2.228.408.5608.216.96.4754 1.3816.895.4217.4194.6816.8176.9005 1.3787.1653.4217.3617 1.056.4169 2.2263.0602 1.2655.0739 1.645.0796 4.848.0058 3.203-.0055 3.5834-.061 4.848-.051 1.17-.245 1.8055-.408 2.2294-.216.5604-.4763.96-.8954 1.3814-.419.4215-.8181.6811-1.3783.9-.4224.1649-1.0577.3617-2.2262.4174-1.2656.0595-1.6448.072-4.8493.079-3.2045.007-3.5825-.006-4.848-.0608M16.953 5.5864A1.44 1.44 0 1 0 18.39 4.144a1.44 1.44 0 0 0-1.437 1.4424M5.8385 12.012c.0067 3.4032 2.7706 6.1557 6.173 6.1493 3.4026-.0065 6.157-2.7701 6.1506-6.1733-.0065-3.4032-2.771-6.1565-6.174-6.1498-3.403.0067-6.156 2.771-6.1496 6.1738M8 12.0077a4 4 0 1 1 4.008 3.9921A3.9996 3.9996 0 0 1 8 12.0077%22/%3E%3C/svg%3E")}
        :is(a.btn,a.link,a.cta)[href*="facebook.com"]::before,a.pill[href*="facebook.com"] .pi::before{-webkit-mask-image:url("data:image/svg+xml,%3Csvg xmlns=%22http://www.w3.org/2000/svg%22 viewBox=%220 0 24 24%22%3E%3Cpath d=%22M9.101 23.691v-7.98H6.627v-3.667h2.474v-1.58c0-4.085 1.848-5.978 5.858-5.978.401 0 .955.042 1.468.103a8.68 8.68 0 0 1 1.141.195v3.325a8.623 8.623 0 0 0-.653-.036 26.805 26.805 0 0 0-.733-.009c-.707 0-1.259.096-1.675.309a1.686 1.686 0 0 0-.679.622c-.258.42-.374.995-.374 1.752v1.297h3.919l-.386 2.103-.287 1.564h-3.246v8.245C19.396 23.238 24 18.179 24 12.044c0-6.627-5.373-12-12-12s-12 5.373-12 12c0 5.628 3.874 10.35 9.101 11.647Z%22/%3E%3C/svg%3E");mask-image:url("data:image/svg+xml,%3Csvg xmlns=%22http://www.w3.org/2000/svg%22 viewBox=%220 0 24 24%22%3E%3Cpath d=%22M9.101 23.691v-7.98H6.627v-3.667h2.474v-1.58c0-4.085 1.848-5.978 5.858-5.978.401 0 .955.042 1.468.103a8.68 8.68 0 0 1 1.141.195v3.325a8.623 8.623 0 0 0-.653-.036 26.805 26.805 0 0 0-.733-.009c-.707 0-1.259.096-1.675.309a1.686 1.686 0 0 0-.679.622c-.258.42-.374.995-.374 1.752v1.297h3.919l-.386 2.103-.287 1.564h-3.246v8.245C19.396 23.238 24 18.179 24 12.044c0-6.627-5.373-12-12-12s-12 5.373-12 12c0 5.628 3.874 10.35 9.101 11.647Z%22/%3E%3C/svg%3E")}
        """;

    private static string BuildArt(ArtPack a) =>
        "<div class=\"tp-art\" aria-hidden=\"true\">" +
        (a.Top is null ? "" : $"<img class=\"a-top\" src=\"{Enc(a.Top)}\" alt=\"\">") +
        (a.Bottom is null ? "" : $"<img class=\"a-bot\" src=\"{Enc(a.Bottom)}\" alt=\"\">") +
        (a.Mascot is null ? "" : $"<img class=\"a-mas\" src=\"{Enc(a.Mascot)}\" alt=\"\">") + "</div>";

    private static string ArtCss(ArtPack a) =>
        "body{position:relative}.pil::before,.bbl::before{display:none}" +
        (a.Bottom is null && a.Mascot is null ? "" : ".page{padding-bottom:180px!important}") +
        ".tp-art{position:absolute;top:0;bottom:0;left:50%;width:min(100%,440px);transform:translateX(-50%);overflow:hidden;pointer-events:none;z-index:0" +
        (a.Bg is null ? "" : $";background:url('{Enc(a.Bg)}') center top/cover no-repeat") + "}" +
        ".tp-art img{position:absolute;display:block;height:auto}.tp-art .a-top{top:0;left:0;width:100%}" +
        ".tp-art .a-bot{bottom:0;left:0;width:100%}.tp-art .a-mas{bottom:3%;left:2%;width:32%}";

    private static string BuildStickers(Sticker[] list, string? assetBase) =>
        "<div class=\"tp-stk\" aria-hidden=\"true\">" + string.Concat(list.Select(s =>
            s.Img is not null && SafeUrl(s.Img, assetBase) is { Length: > 0 } src
                ? FormattableString.Invariant($"<img class=\"tp-s {s.St}\" src=\"{src}\" alt=\"\" style=\"left:{s.X}%;top:{s.Y}px;width:{s.S}px;height:auto;transform:translate(-50%,-50%) rotate({s.R}deg)\">")
                : FormattableString.Invariant($"<i class=\"tp-s {s.St}\" style=\"left:{s.X}%;top:{s.Y}px;font-size:{s.S}px;transform:translate(-50%,-50%) rotate({s.R}deg)\">{Enc(s.E)}</i>"))) + "</div>";

    private const string WrapCss =
        "body{flex-wrap:wrap;align-content:flex-start}.tp-extra{flex:0 0 100%;width:100%;max-width:440px;margin:0 auto;padding:8px 16px 40px;box-sizing:border-box;text-align:center}";

    private const string GalleryCss =
        ".tp-extra h2{font:700 .95rem system-ui,sans-serif;letter-spacing:.12em;text-transform:uppercase;margin:18px 0 10px;color:inherit;opacity:.8}" +
        ".tp-gal{display:grid;grid-template-columns:1fr 1fr;gap:8px}.tp-gal img{width:100%;aspect-ratio:1/1;object-fit:cover;border-radius:12px;display:block}" +
        ".tp-vid{display:block;position:relative;border-radius:14px;overflow:hidden}.tp-vid img{width:100%;display:block}" +
        ".tp-vid iframe{width:100%;aspect-ratio:16/9;border:0;display:block}.tp-vid span{position:absolute;inset:0;display:grid;place-items:center;font-size:3rem;color:#fff;background:rgba(0,0,0,.28)}" +
        ".tp-vbtn{display:inline-block;padding:12px 20px;border-radius:999px;background:rgba(127,127,127,.25);color:inherit;text-decoration:none;font:600 1rem system-ui,sans-serif}";

    [GeneratedRegex(@"(?:youtu\.be/|youtube\.com/(?:watch\?(?:.*&)?v=|embed/|shorts/))([\w-]{11})")]
    private static partial Regex YoutubeRx();

    private static (string Gallery, string Video) BuildGallery(Extras x, string assetBase)
    {
        var imgs = (x.Gallery ?? []).Take(8).Select(g => SafeUrl(g, assetBase)).Where(u => u.Length > 0).ToArray();
        var video = "";
        if (x.VideoUrl is { Length: > 0 and <= 300 } v && Uri.TryCreate(v, UriKind.Absolute, out var vu) && vu.Scheme == Uri.UriSchemeHttps)
        {
            var yt = YoutubeRx().Match(vu.AbsoluteUri);
            video = yt.Success
                ? x.Embed
                    ? $"<h2>Video</h2><div class=\"tp-vid\"><iframe src=\"https://www.youtube-nocookie.com/embed/{yt.Groups[1].Value}\" title=\"Video\" loading=\"lazy\" allow=\"encrypted-media; picture-in-picture; fullscreen\" allowfullscreen referrerpolicy=\"strict-origin-when-cross-origin\"></iframe></div>"
                    : $"<h2>Video</h2><a class=\"tp-vid\" href=\"https://www.youtube.com/watch?v={yt.Groups[1].Value}\"><img src=\"https://img.youtube.com/vi/{yt.Groups[1].Value}/hqdefault.jpg\" alt=\"Video\"><span>▶</span></a>"
                : $"<h2>Video</h2><a class=\"tp-vbtn\" href=\"{Enc(vu.AbsoluteUri)}\">▶ Ver video</a>";
        }
        var gal = imgs.Length == 0 ? "" : "<h2>Galería</h2><div class=\"tp-gal\">" + string.Concat(imgs.Select(i => $"<img src=\"{i}\" alt=\"\" loading=\"lazy\">")) + "</div>";
        return (gal, video);
    }

    [GeneratedRegex("<section")]
    private static partial Regex SectionOpenRx();

    [GeneratedRegex("<section data-sec=\"(\\w+)\"[^>]*>.*?</section>", RegexOptions.Singleline)]
    private static partial Regex SectionBlockRx();

    /// <summary>
    /// Reordena las secciones marcadas según el orden del cliente y mete galería y video como secciones más.
    /// Cada hueco conserva su lugar en el HTML; solo cambia qué sección ocupa cada hueco.
    /// Si la plantilla no tiene secciones, devuelve placed=false y la galería va al final.
    /// </summary>
    private static (string Body, bool Placed) Arrange(string body, string gal, string vid, string[]? order)
    {
        var matches = SectionBlockRx().Matches(body);
        if (matches.Count == 0) return (body, false);

        var blocks = matches.Select(m => (Key: m.Groups[1].Value, Html: m.Value)).ToList();
        if (gal.Length > 0) blocks.Add(("galeria", $"<section data-sec=\"galeria\">{gal}</section>"));
        if (vid.Length > 0) blocks.Add(("video", $"<section data-sec=\"video\">{vid}</section>"));

        var rank = order ?? [];
        int Rank(string k) { var i = Array.IndexOf(rank, k); return i < 0 ? int.MaxValue : i; }
        var sorted = blocks.OrderBy(b => Rank(b.Key)).Select(b => b.Html).ToList();

        var n = 0;
        var result = SectionBlockRx().Replace(body, _ => sorted[n++]);
        // Las secciones que no tenían hueco (galería/video) van justo después de la última.
        if (n < sorted.Count)
        {
            var last = matches[^1];
            var end = last.Index + last.Length + (result.Length - body.Length);
            result = result.Insert(end, string.Concat(sorted.Skip(n)));
        }
        return (result, true);
    }

    private static string GalleryLayoutCss(string? layout) => layout switch
    {
        "tres" => ".tp-gal{grid-template-columns:repeat(3,1fr)!important}",
        "grande" => ".tp-gal{grid-template-columns:1fr!important}.tp-gal img{aspect-ratio:4/3!important}",
        "tira" => ".tp-gal{display:flex!important;overflow-x:auto;scroll-snap-type:x mandatory;gap:8px}.tp-gal img{flex:0 0 72%;width:72%!important;scroll-snap-align:center}",
        _ => "",
    };

    /// <summary>Cambia los href por una ruta de la API que cuenta el clic y redirige; el destino se vuelve a leer de la tarjeta.</summary>
    private static string TrackLinks(string body, Dictionary<string, string> clean, string trackBase)
    {
        foreach (var key in TrackedKeys)
        {
            if (!clean.TryGetValue(key, out var v)) continue;
            body = body.Replace($"href=\"{Href(key, v)}\"", $"href=\"{trackBase}/{key}\"");
        }
        return body;
    }

    public static readonly string[] TrackedKeys =
        ["whatsapp", "whatsapp2", "telefono", "email", "facebook", "instagram", "tiktok", "linkedin", "web", "mapa", "mapa2", "mapa3"];

    private static string Href(string key, string value) => key switch
    {
        "whatsapp" or "whatsapp2" => $"https://wa.me/{value}",
        "telefono" => $"tel:{value}",
        "email" => $"mailto:{value}",
        _ => value,
    };

    /// <summary>URL real (sin escapar para HTML) a la que debe llevar un clic rastreado, o null si no aplica.</summary>
    public static string? LinkTarget(string key, IReadOnlyDictionary<string, string> data)
    {
        if (!TrackedKeys.Contains(key) || !CardFields.ByKey.TryGetValue(key, out var field)) return null;
        if (!data.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw)) return null;
        var v = System.Net.WebUtility.HtmlDecode(Clean(field.Type, Expand(field.Key, raw.Trim()), ""));
        return v.Length == 0 ? null : Href(key, v);
    }

    private static string BuildDecor(string key, CardTheme t, string css)
    {
        if (!ThemeRules.Decors.TryGetValue(key, out var svg)) return "";
        var knobs = ThemeRules.Detect(css);
        var c1 = t.Accent ?? knobs.Accent ?? "#7c8cf8";
        var c2 = t.Accent2 ?? knobs.Accent2 ?? c1;
        var count = Math.Clamp(t.DecorCount ?? 1, 1, ThemeRules.MaxDecor);
        var layers = string.Concat(ThemeRules.DecorLayers.Take(count)
            .Select(tr => tr.Length == 0 ? svg : $"<g transform=\"{tr}\">{svg}</g>"));
        return $"<div class=\"tp-decor\" aria-hidden=\"true\" style=\"color:{c1};--c2:{c2}\"><svg viewBox=\"0 0 400 800\" preserveAspectRatio=\"xMidYMid slice\" fill=\"currentColor\">{layers}</svg></div>";
    }

    private static string ApplyTheme(string css, CardTheme t, string assetBase)
    {
        var knobs = ThemeRules.Detect(css);
        string Swap(string c, string? from, string? to) =>
            from is null || to is null ? c : Regex.Replace(c, Regex.Escape(from), to, RegexOptions.IgnoreCase);
        css = Swap(css, knobs.Accent, t.Accent);
        css = Swap(css, knobs.Accent2, t.Accent2);
        css = Swap(css, knobs.Text, t.Text);

        var extra = new System.Text.StringBuilder();
        var import = t.Font is not null && ThemeRules.WebFontQuery.TryGetValue(t.Font, out var q)
            ? $"@import url('https://fonts.googleapis.com/css2?family={q}&display=swap');\n" : "";
        if (t.Font is not null && ThemeRules.Fonts.TryGetValue(t.Font, out var stack))
            extra.Append($"body,h1,h2,h3,p,a,li,span,button{{font-family:{stack}!important}}");
        if (t.BgImage is not null && SafeUrl(t.BgImage, assetBase) is { Length: > 0 } img)
        {
            // Velo blanco opcional para aclarar fotos de fondo oscuras.
            var a = (t.BgLight ?? 0) / 100.0;
            var veil = a > 0 ? FormattableString.Invariant($"linear-gradient(rgba(255,255,255,{a:0.##}),rgba(255,255,255,{a:0.##})),") : "";
            extra.Append($"body{{background-image:{veil}url('{img}')!important;background-size:cover!important;background-position:center!important;background-attachment:fixed!important}}.page{{background:transparent!important}}");
        }
        else if (t.Bg is not null)
            extra.Append($"body{{background:{t.Bg}!important}}.page{{background:transparent!important}}");
        extra.Append(t.ButtonsLayout switch
        {
            "lista" => ".actions,.links,.row{display:grid!important;grid-template-columns:1fr!important;gap:10px!important}",
            "cuadricula" => ".actions,.links,.row{display:grid!important;grid-template-columns:1fr 1fr!important;gap:10px!important}",
            "compacta" => ".actions,.links,.row{display:flex!important;flex-wrap:wrap!important;justify-content:center!important;gap:8px!important}.actions>*,.links>*,.row>*{flex:0 1 auto}",
            _ => "",
        });
        var accent = t.Accent ?? knobs.Accent ?? "currentColor";
        if (t.TextAlign is not null && ThemeRules.TextAligns.TryGetValue(t.TextAlign, out var ta))
            extra.Append($"body,.card,.page,h1,h2,p,li,.role,.company,.bio,.info{{text-align:{ta}!important}}");
        if (t.ArtHue is { } ah) extra.Append($".tp-art .a-top,.tp-art .a-bot{{filter:hue-rotate({ah}deg)}}");
        if (t.HideIcons == true) extra.Append(".deco,.ibar{display:none!important}");
        if (t.Stickers is { Length: > 0 })
        {
            var edge = t.Accent ?? knobs.Accent ?? "#ffffff";
            extra.Append("html,body{overflow-x:hidden}body{position:relative}" +
                ".tp-stk{position:absolute;top:0;left:50%;width:min(100%,440px);height:0;transform:translateX(-50%);pointer-events:none;z-index:6}" +
                ".tp-s{position:absolute;font-style:normal;line-height:1;white-space:nowrap}" +
                ".tp-s.sombra{filter:drop-shadow(0 4px 5px rgba(0,0,0,.35))}" +
                $".tp-s.borde{{filter:drop-shadow(2px 0 0 {edge}) drop-shadow(-2px 0 0 {edge}) drop-shadow(0 2px 0 {edge}) drop-shadow(0 -2px 0 {edge})}}");
        }
        if (t.Decor is not null)
            extra.Append(".tp-decor{position:fixed;top:0;bottom:0;left:50%;width:min(100%,440px);transform:translateX(-50%);pointer-events:none;z-index:5;opacity:.38}.tp-decor svg{width:100%;height:100%;display:block}");
        extra.Append(t.Buttons switch
        {
            "redondo" => ".btn,.link,.cta{border-radius:999px!important}",
            "suave" => ".btn,.link,.cta{border-radius:14px!important}",
            "cuadrado" => ".btn,.link,.cta{border-radius:0!important}",
            "borde" => $".btn,.link,.cta{{background:transparent!important;border:2px solid {accent}!important;color:{accent}!important;box-shadow:none!important}}",
            "sombra" => ".btn,.link,.cta{box-shadow:0 5px 0 rgba(0,0,0,.28)!important}",
            _ => "",
        });
        extra.Append(t.Photo switch
        {
            "circulo" => ".avatar,.logo{border-radius:50%!important;object-fit:cover}",
            "suave" => ".avatar,.logo{border-radius:18px!important;object-fit:cover}",
            "cuadrado" => ".avatar,.logo{border-radius:0!important;object-fit:cover}",
            "marco" => $".avatar,.logo{{border:4px solid {accent}!important;padding:3px;background:#fff;border-radius:12px!important;object-fit:cover}}",
            // Foto a todo el ancho de la pantalla (sin márgenes) y en vertical, como una pantalla de celular.
            "ancho" => ".avatar{display:block!important;width:100%!important;max-width:none!important;height:auto!important;aspect-ratio:4/5;margin:0 0 14px!important;border-radius:0 0 28px 28px!important;object-fit:cover;object-position:center top}" +
                ".page .avatar{width:calc(100% + 40px)!important;margin:-28px -20px 14px!important}",
            _ => "",
        });
        return import + css + extra;
    }

    /// <summary>Devuelve valores ya listos para insertarse en HTML (escapados) por campo conocido.</summary>
    private static (Dictionary<string, string> Values, Dictionary<string, string[]> Lists) Sanitize(
        IReadOnlyDictionary<string, string>? data, string assetBase)
    {
        var values = new Dictionary<string, string>();
        var lists = new Dictionary<string, string[]>();
        if (data is null) return (values, lists);

        foreach (var field in CardFields.All)
        {
            if (!data.TryGetValue(field.Key, out var raw) || string.IsNullOrWhiteSpace(raw)) continue;

            if (field.Type == FieldType.List)
            {
                var items = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Take(MaxListItems).Select(i => Enc(Cut(i, 100))).ToArray();
                if (items.Length == 0) continue;
                lists[field.Key] = items;
                values[field.Key] = string.Join("<br>", items);
                continue;
            }

            var value = Clean(field.Type, Expand(field.Key, raw.Trim()), assetBase);
            if (value.Length > 0) values[field.Key] = value;
        }
        // Con solo el link de mapa (sin dirección escrita) la sección "Ubicación" igual debe verse.
        foreach (var n in new[] { "", "2", "3" })
            if (values.ContainsKey("mapa" + n) && !values.ContainsKey("ubicacion" + n)) values["ubicacion" + n] = "Cómo llegar";
        return (values, lists);
    }

    private static readonly Dictionary<string, string> SocialBase = new()
    {
        ["facebook"] = "https://facebook.com/", ["instagram"] = "https://instagram.com/",
        ["tiktok"] = "https://tiktok.com/@", ["linkedin"] = "https://linkedin.com/in/",
    };

    /// <summary>Si en una red social escribieron solo el usuario (@usuario o usuario), arma el enlace completo.</summary>
    private static string Expand(string key, string raw)
    {
        if (!SocialBase.TryGetValue(key, out var baseUrl) || raw.Length == 0 || raw.Contains("://") || raw.Contains('.') || raw.Contains('/')) return raw;
        var user = raw.TrimStart('@');
        return user.Length > 0 && user.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '-') ? baseUrl + user : raw;
    }

    private const int MaxListItems = 20;

    private static string Clean(FieldType type, string raw, string assetBase) => type switch
    {
        FieldType.Text => Enc(Cut(raw, 200)),
        FieldType.TextArea => Enc(Cut(raw, 1000)).Replace("\r\n", "\n").Replace("\n", "<br>"),
        FieldType.Whatsapp => WhatsappDigits(raw),
        FieldType.Phone => Enc(Cut(new string(raw.Where(c => char.IsAsciiDigit(c) || "+()- ".Contains(c)).ToArray()), 30)),
        FieldType.Email => EmailRx().IsMatch(raw) && raw.Length <= 200 ? Enc(raw) : "",
        FieldType.Url => SafeUrl(raw, null),
        FieldType.Image => SafeUrl(raw, assetBase),
        _ => "",
    };

    private static string SafeUrl(string raw, string? assetBase)
    {
        if (raw.Length > 500) return "";
        if (assetBase is not null && raw.StartsWith("/uploads/", StringComparison.Ordinal) && !raw.Contains(".."))
            return Enc(assetBase + raw);
        if (!raw.Contains("://")) raw = "https://" + raw;
        return Uri.TryCreate(raw, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" && uri.Host.Contains('.')
            ? Enc(uri.AbsoluteUri)
            : "";
    }

    /// <summary>Solo dígitos; con 10 dígitos (número de México sin lada) antepone 52 para que wa.me funcione.</summary>
    private static string WhatsappDigits(string raw)
    {
        var d = new string(raw.Where(char.IsAsciiDigit).ToArray());
        return Cut(d.Length == 10 ? "52" + d : d, 20);
    }

    private static string Cut(string s, int max) => s.Length <= max ? s : s[..max];
    private static string Enc(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&#39;")
         .Replace("{", "&#123;");
}
