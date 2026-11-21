using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using TarjetasPro.Application.Templates;

namespace TarjetasPro.Application.Canvas;

/// <summary>Un elemento del lienzo (plan Pro Max). Medidas en puntos sobre un lienzo de 390 de ancho.</summary>
public record CanvasEl(
    string Id, string Type, double X, double Y, double W, double H, double Rot = 0,
    string? Text = null, string? Src = null, string? Kind = null, string? Value = null,
    string? Color = null, string? Fill = null, double Size = 16, int Weight = 400, string Align = "left",
    string? Font = null, double Radius = 0, double Opacity = 1, bool Italic = false, string? Shape = null,
    string? Icon = null, double Spacing = 0, double Line = 0, string? Shadow = null, bool Locked = false, bool Hidden = false, string? Mask = null, string? Fx = null, string? Anim = null, string? Sticker = null, double StickerW = 0,
    double ImgZoom = 1, double ImgX = 50, double ImgY = 50, string? ImgFit = null);

public record CanvasDoc(double Height, string Bg, string? BgImage, List<CanvasEl> Els, string? Bg2 = null, double Angle = 180);

/// <summary>Código de la plantilla interna que usan todas las tarjetas de lienzo.</summary>
public static class CanvasTemplate
{
    public const string Code = "lienzo-libre";
}

public static partial class CanvasRules
{
    public const double Width = 390;
    public const int MaxElements = 250;
    public static readonly string[] Types = ["text", "image", "button", "shape", "icon"];
    public static readonly string[] Shadows = ["soft", "strong", "glow"];
    /// <summary>Efectos de texto y animaciones de entrada permitidos.</summary>
    public static readonly string[] TextFx = ["outline", "hard"];
    public static readonly string[] Anims = ["up", "fade", "zoom", "slide", "type"];
    /// <summary>Íconos del editor: nombre → trazos SVG (24x24, línea). Solo estos se aceptan; el usuario nunca manda SVG.</summary>
    public static readonly IReadOnlyDictionary<string, string> Icons = new Dictionary<string, string>
    {
        ["telefono"] = "M22 16.9v3a2 2 0 0 1-2.2 2 19.8 19.8 0 0 1-8.6-3.1 19.5 19.5 0 0 1-6-6A19.8 19.8 0 0 1 2.1 4.2 2 2 0 0 1 4.1 2h3a2 2 0 0 1 2 1.7c.1 1 .4 1.9.7 2.8a2 2 0 0 1-.5 2.1L8.1 9.9a16 16 0 0 0 6 6l1.3-1.3a2 2 0 0 1 2.1-.4c.9.3 1.8.6 2.8.7a2 2 0 0 1 1.7 2z",
        ["whatsapp"] = "M21 11.5a8.4 8.4 0 0 1-12.4 7.4L3 20.5l1.6-5.4A8.4 8.4 0 1 1 21 11.5z",
        ["correo"] = "M4 4h16a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2zM22 6l-10 7L2 6",
        ["mapa"] = "M12 22s8-6.2 8-12a8 8 0 1 0-16 0c0 5.8 8 12 8 12zM12 7a3 3 0 1 1 0 6 3 3 0 0 1 0-6z",
        ["web"] = "M12 2a10 10 0 1 0 0 20 10 10 0 0 0 0-20zM2 12h20M12 2a15 15 0 0 1 0 20M12 2a15 15 0 0 0 0 20",
        ["instagram"] = "M7 2h10a5 5 0 0 1 5 5v10a5 5 0 0 1-5 5H7a5 5 0 0 1-5-5V7a5 5 0 0 1 5-5zM12 8a4 4 0 1 0 0 8 4 4 0 0 0 0-8zM17.5 6.5h.01",
        ["facebook"] = "M18 2h-3a5 5 0 0 0-5 5v3H7v4h3v8h4v-8h3l1-4h-4V7a1 1 0 0 1 1-1h3z",
        ["linkedin"] = "M16 8a6 6 0 0 1 6 6v7h-4v-7a2 2 0 0 0-4 0v7h-4v-7a6 6 0 0 1 6-6zM2 9h4v12H2zM4 2a2 2 0 1 1 0 4 2 2 0 0 1 0-4z",
        ["youtube"] = "M22.5 7.2a3 3 0 0 0-2.1-2.1C18.5 4.6 12 4.6 12 4.6s-6.5 0-8.4.5A3 3 0 0 0 1.5 7.2 31 31 0 0 0 1 12a31 31 0 0 0 .5 4.8 3 3 0 0 0 2.1 2.1c1.9.5 8.4.5 8.4.5s6.5 0 8.4-.5a3 3 0 0 0 2.1-2.1A31 31 0 0 0 23 12a31 31 0 0 0-.5-4.8zM10 15V9l5.2 3z",
        ["tiktok"] = "M16 3a5 5 0 0 0 5 5v4a9 9 0 0 1-5-1.5V16a6 6 0 1 1-6-6v4a2 2 0 1 0 2 2V3z",
        ["estrella"] = "M12 2l3.1 6.3 6.9 1-5 4.9 1.2 6.8L12 17.8 5.8 21l1.2-6.8-5-4.9 6.9-1z",
        ["corazon"] = "M20.8 4.6a5.5 5.5 0 0 0-7.8 0L12 5.7l-1-1.1a5.5 5.5 0 0 0-7.8 7.8l1 1.1L12 21l7.8-7.5 1-1.1a5.5 5.5 0 0 0 0-7.8z",
        ["reloj"] = "M12 2a10 10 0 1 0 0 20 10 10 0 0 0 0-20zM12 6v6l4 2",
        ["calendario"] = "M5 4h14a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2zM16 2v4M8 2v4M3 10h18",
        ["check"] = "M20 6L9 17l-5-5",
        ["flecha"] = "M5 12h14M12 5l7 7-7 7",
        ["usuario"] = "M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2M12 3a4 4 0 1 1 0 8 4 4 0 0 1 0-8z",
        ["camara"] = "M23 19a2 2 0 0 1-2 2H3a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h4l2-3h6l2 3h4a2 2 0 0 1 2 2zM12 9a4 4 0 1 0 0 8 4 4 0 0 0 0-8z",
        ["maletin"] = "M4 7h16a2 2 0 0 1 2 2v10a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V9a2 2 0 0 1 2-2zM16 21V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v16",
        ["casa"] = "M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2zM9 22V12h6v10",
        ["carrito"] = "M9 22a1 1 0 1 0 0-2 1 1 0 0 0 0 2zM20 22a1 1 0 1 0 0-2 1 1 0 0 0 0 2zM1 1h4l2.7 13.4a2 2 0 0 0 2 1.6h9.7a2 2 0 0 0 2-1.6L23 6H6",
        ["rayo"] = "M13 2L3 14h9l-1 8 10-12h-9z",
        ["escudo"] = "M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z",
        ["tijeras"] = "M6 3a3 3 0 1 0 0 6 3 3 0 0 0 0-6zM6 15a3 3 0 1 0 0 6 3 3 0 0 0 0-6zM20 4L8.1 15.9M14.5 14.5L20 20M8.1 8.1L12 12",
        ["llave"] = "M21 2l-2 2m-7.6 7.6a5.5 5.5 0 1 1-7.8 7.8 5.5 5.5 0 0 1 7.8-7.8zm0 0L15.5 7.5m0 0l3 3L22 7l-3-3m-3.5 3.5L19 4",
    };
    public static readonly string[] Kinds = ["whatsapp", "telefono", "email", "web", "mapa", "facebook", "instagram", "tiktok", "linkedin"];
    /// <summary>Tipografías de Google Fonts disponibles en el editor: clave → familia.</summary>
    public static readonly IReadOnlyDictionary<string, string> Fonts = new Dictionary<string, string>
    {
        ["Inter"] = "Inter:wght@400;600;800", ["Poppins"] = "Poppins:wght@400;600;800", ["Montserrat"] = "Montserrat:wght@400;600;800",
        ["Playfair Display"] = "Playfair+Display:wght@400;600;800", ["Lora"] = "Lora:wght@400;600", ["Bebas Neue"] = "Bebas+Neue",
        ["Pacifico"] = "Pacifico", ["Roboto Mono"] = "Roboto+Mono:wght@400;600", ["Oswald"] = "Oswald:wght@400;600", ["DM Serif Display"] = "DM+Serif+Display",
        ["Dancing Script"] = "Dancing+Script:wght@400;700", ["Great Vibes"] = "Great+Vibes", ["Allura"] = "Allura", ["Parisienne"] = "Parisienne",
        ["Sacramento"] = "Sacramento", ["Satisfy"] = "Satisfy", ["Caveat"] = "Caveat:wght@400;700", ["Cinzel"] = "Cinzel:wght@400;700",
        ["Cormorant Garamond"] = "Cormorant+Garamond:wght@400;600", ["Abril Fatface"] = "Abril+Fatface", ["Josefin Sans"] = "Josefin+Sans:wght@400;600",
        ["Raleway"] = "Raleway:wght@400;600;800", ["Lobster"] = "Lobster", ["Permanent Marker"] = "Permanent+Marker", ["Fredoka"] = "Fredoka:wght@400;600",
    };

    /// <summary>Recortes de foto (CSS clip-path): nombre → forma. «polaroid» es un marco, no un recorte.</summary>
    public static readonly IReadOnlyDictionary<string, string> Masks = new Dictionary<string, string>
    {
        ["oval"] = "ellipse(50% 50% at 50% 50%)",
        ["arco"] = "inset(0 round 999px 999px 0 0)",
        ["pildora"] = "inset(0 round 999px)",
        ["suave"] = "inset(0 round 12%)",
        ["corazon"] = "polygon(50.0% 23.9%, 50.1% 22.7%, 50.9% 19.4%, 52.8% 14.6%, 56.2% 9.2%, 61.3% 4.4%, 67.7% 1.1%, 75.0% 0.0%, 82.5% 1.5%, 89.4% 5.4%, 95.1% 11.5%, 98.7% 19.0%, 100.0% 27.4%, 98.7% 36.0%, 95.1% 44.5%, 89.4% 52.6%, 82.5% 60.2%, 75.0% 67.5%, 67.7% 74.4%, 61.3% 81.0%, 56.2% 87.1%, 52.8% 92.3%, 50.9% 96.4%, 50.1% 99.1%, 50.0% 100.0%, 49.9% 99.1%, 49.1% 96.4%, 47.2% 92.3%, 43.8% 87.1%, 38.7% 81.0%, 32.3% 74.4%, 25.0% 67.5%, 17.5% 60.2%, 10.6% 52.6%, 4.9% 44.5%, 1.3% 36.0%, 0.0% 27.4%, 1.3% 19.0%, 4.9% 11.5%, 10.6% 5.4%, 17.5% 1.5%, 25.0% 0.0%, 32.3% 1.1%, 38.7% 4.4%, 43.7% 9.2%, 47.2% 14.6%, 49.1% 19.4%, 49.9% 22.7%)",
        ["estrella"] = "polygon(50.0% 0.0%, 62.3% 33.0%, 97.6% 34.5%, 70.0% 56.5%, 79.4% 90.5%, 50.0% 71.0%, 20.6% 90.5%, 30.0% 56.5%, 2.4% 34.5%, 37.7% 33.0%)",
        ["hexagono"] = "polygon(100.0% 50.0%, 75.0% 93.3%, 25.0% 93.3%, 0.0% 50.0%, 25.0% 6.7%, 75.0% 6.7%)",
        ["rombo"] = "polygon(50% 0,100% 50%,50% 100%,0 50%)",
    };
    public static readonly string[] MaskNames = [.. Masks.Keys, "polaroid"];

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [GeneratedRegex(@"^#[0-9a-fA-F]{3,8}$")]
    private static partial Regex ColorRx();

    private static double Clamp(double v, double min, double max) => double.IsNaN(v) || double.IsInfinity(v) ? min : Math.Min(max, Math.Max(min, v));
    private static string? Color(string? c) => c is not null && (c == "transparent" || ColorRx().IsMatch(c)) ? c : null;
    private static string? Cut(string? s, int n) => s is null ? null : s.Length <= n ? s : s[..n];

    private static string? Img(string? s) =>
        s is { Length: > 0 and <= 500 } && ((s.StartsWith("/uploads/", StringComparison.Ordinal) || s.StartsWith("/gfx/", StringComparison.Ordinal)) && !s.Contains("..") || s.StartsWith("https://", StringComparison.Ordinal)) ? s : null;

    /// <summary>Deja el lienzo dentro de límites seguros: nada de HTML, colores libres ni enlaces raros.</summary>
    public static CanvasDoc Sanitize(CanvasDoc? d)
    {
        if (d is null) return new CanvasDoc(780, "#ffffff", null, [], null, 180);
        var els = new List<CanvasEl>();
        foreach (var e in (d.Els ?? []).Take(MaxElements))
        {
            if (!Types.Contains(e.Type)) continue;
            if (e.Type == "icon" && !Icons.ContainsKey(e.Icon ?? "")) continue;
            var id = new string((e.Id ?? "").Where(char.IsAsciiLetterOrDigit).Take(24).ToArray());
            els.Add(new CanvasEl(
                id.Length > 0 ? id : Guid.NewGuid().ToString("N")[..8], e.Type,
                Clamp(e.X, -400, 800), Clamp(e.Y, -400, 4000), Clamp(e.W, 4, 1200), Clamp(e.H, 4, 2400), Clamp(e.Rot, -360, 360),
                Cut(e.Text, 600), Img(e.Src), Kinds.Contains(e.Kind ?? "") ? e.Kind : null, Cut(e.Value, 200),
                Color(e.Color), Color(e.Fill), Clamp(e.Size, 6, 200), (int)Clamp(Math.Round(e.Weight / 100.0) * 100, 100, 900),
                e.Align is "center" or "right" ? e.Align : "left",
                e.Font is not null && Fonts.ContainsKey(e.Font) ? e.Font : null,
                Clamp(e.Radius, 0, 600), Clamp(e.Opacity, 0, 1), e.Italic, e.Shape == "circle" ? "circle" : "rect",
                e.Type == "icon" && e.Icon is not null && Icons.ContainsKey(e.Icon) ? e.Icon : null,
                Clamp(e.Spacing, -0.2, 1), Clamp(e.Line, 0, 3), Shadows.Contains(e.Shadow ?? "") ? e.Shadow : null, e.Locked, e.Hidden,
                e.Type == "image" && e.Mask is not null && MaskNames.Contains(e.Mask) ? e.Mask : null,
                e.Type == "text" && TextFx.Contains(e.Fx ?? "") ? e.Fx : null,
                Anims.Contains(e.Anim ?? "") && (e.Anim != "type" || e.Type == "text") ? e.Anim : null,
                e.Type == "image" ? Color(e.Sticker) : null, e.Type == "image" ? Clamp(e.StickerW, 0, 12) : 0,
                e.Type == "image" ? Clamp(e.ImgZoom, 1, 4) : 1, e.Type == "image" ? Clamp(e.ImgX, 0, 100) : 50, e.Type == "image" ? Clamp(e.ImgY, 0, 100) : 50,
                e.Type == "image" && e.ImgFit == "contain" ? "contain" : null));
        }
        var bg2 = Color(d.Bg2);
        return new CanvasDoc(Clamp(d.Height, 300, 2400), Color(d.Bg) ?? "#ffffff", Img(d.BgImage), els, bg2 == "transparent" ? null : bg2, Clamp(d.Angle, 0, 360));
    }

    private static string N(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);
    /// <summary>Escapa el texto del usuario y neutraliza las llaves para que el motor de plantillas no las interprete.</summary>
    private static string Enc(string s) => WebUtility.HtmlEncode(s).Replace("{", "&#123;").Replace("}", "&#125;");

    /// <summary>Arma el HTML y CSS del lienzo y lo pasa por el renderer de siempre (CSP, marca de agua e íconos incluidos).</summary>
    /// <summary>Destino real de un botón del lienzo (lo que se abre al tocarlo), o null si no hace nada.</summary>
    public static (string Kind, string Target)? ButtonTarget(CanvasDoc doc, string elId)
    {
        var e = Sanitize(doc).Els.FirstOrDefault(x => x.Id == elId && x.Type == "button");
        if (e?.Kind is not { } k || string.IsNullOrWhiteSpace(e.Value)) return null;
        var t = TemplateRenderer.LinkTarget(k, new Dictionary<string, string> { [k] = e.Value });
        return t is null ? null : (k, t);
    }

    public static string Render(CanvasDoc doc, string assetBase, bool watermark, string? trackBase = null)
    {
        doc = Sanitize(doc);
        string Asset(string src) => src.StartsWith("/", StringComparison.Ordinal) ? assetBase.TrimEnd('/') + src : src;
        var h = doc.Height;
        var fonts = doc.Els.Where(e => e.Font is not null).Select(e => Fonts[e.Font!]).Distinct().ToList();
        var css = new StringBuilder();
        if (fonts.Count > 0) css.Append($"@import url('https://fonts.googleapis.com/css2?{string.Join('&', fonts.Select(f => "family=" + f))}&display=swap');");
        css.Append($"*{{box-sizing:border-box}}html,body{{margin:0}}body{{background:{doc.Bg};display:flex;justify-content:center;font-family:system-ui,sans-serif}}");
        css.Append(".cv{position:relative;width:100%;max-width:440px;overflow:hidden;container-type:inline-size;");
        var bgBase = doc.Bg2 is { } b2 ? $"linear-gradient({N(doc.Angle)}deg,{doc.Bg},{b2})" : doc.Bg;
        css.Append($"aspect-ratio:{N(Width)}/{N(h)};background:{bgBase}");
        if (doc.BgImage is { } bg) css.Append($",url('{Asset(bg)}') center/cover no-repeat");
        css.Append("}.cv .e{position:absolute;margin:0;line-height:1.2;overflow-wrap:anywhere}.cv img.e{object-fit:cover}");
        // En pantallas grandes (computadora) la columna crece: todo escala con el ancho, así que se ve igual pero más grande.
        css.Append("@media(min-width:900px){.cv{max-width:720px}}");
        css.Append(".cv a.e{display:flex;align-items:center;justify-content:center;text-decoration:none;text-align:center}");
        css.Append(".cv svg.e{pointer-events:none}");
        css.Append("@keyframes k-up{from{opacity:0;translate:0 22px}}@keyframes k-fade{from{opacity:0}}@keyframes k-zoom{from{opacity:0;scale:.6}}@keyframes k-slide{from{opacity:0;translate:-40px 0}}@keyframes k-type{from{clip-path:inset(0 100% 0 0)}}");
        css.Append("@media(prefers-reduced-motion:reduce){.cv .e{animation:none!important}}");

        var html = new StringBuilder("<div class=\"cv\">");
        var ai = 0;
        foreach (var e in doc.Els)
        {
            if (e.Hidden) continue;
            var st = new StringBuilder();
            st.Append($"left:{N(e.X / Width * 100)}%;top:{N(e.Y / h * 100)}%;width:{N(e.W / Width * 100)}%;height:{N(e.H / h * 100)}%;");
            if (e.Rot != 0) st.Append($"transform:rotate({N(e.Rot)}deg);");
            if (e.Opacity < 1) st.Append($"opacity:{N(e.Opacity)};");
            if (e.Type == "image" && e.Mask is { } mk)
            {
                if (mk == "polaroid") st.Append($"border:calc(8 * 100cqw / {N(Width)}) solid #fff;border-bottom-width:calc(34 * 100cqw / {N(Width)});box-shadow:0 6px 18px rgba(0,0,0,.25);");
                else st.Append($"clip-path:{Masks[mk]};");
            }
            else if (e.Shape == "circle" && e.Type is "shape" or "image") st.Append("border-radius:50%;");
            else if (e.Radius > 0) st.Append($"border-radius:calc({N(e.Radius)} * 100cqw / {N(Width)});");
            var font = $"font-size:calc({N(e.Size)} * 100cqw / {N(Width)});font-weight:{e.Weight};text-align:{e.Align};" +
                       (e.Italic ? "font-style:italic;" : "") + (e.Font is null ? "" : $"font-family:'{e.Font}',system-ui,sans-serif;") +
                       (e.Color is null ? "" : $"color:{e.Color};");
            if (e.Spacing != 0) font += $"letter-spacing:{N(e.Spacing)}em;";
            if (e.Line > 0) font += $"line-height:{N(e.Line)};";
            if (e.Anim is { } an)
                st.Append($"animation:k-{an} .7s {N(Math.Min(ai++ * 0.12, 1.2))}s cubic-bezier(.23,1,.32,1) both;");
            if (e.Type == "text" && e.Fx == "outline") font += $"color:transparent;-webkit-text-stroke:calc(1.6 * 100cqw / {N(Width)}) {e.Color ?? "#111111"};";
            else if (e.Type == "text" && e.Fx == "hard") st.Append($"text-shadow:calc(3 * 100cqw / {N(Width)}) calc(3 * 100cqw / {N(Width)}) 0 rgba(0,0,0,.35);");
            var sticker = e.Type == "image" && e.Sticker is { } sc && sc != "transparent" && e.StickerW > 0 && e.Mask is null;
            if (sticker)
            {
                var n = $"calc({N(e.StickerW)} * 100cqw / {N(Width)})";
                var f = string.Join(' ', new[] { "1 0", "-1 0", "0 1", "0 -1", ".7 .7", "-.7 .7", ".7 -.7", "-.7 -.7" }.Select(d => { var p = d.Split(' '); return $"drop-shadow(calc({p[0]} * {n}) calc({p[1]} * {n}) 0 {e.Sticker})"; }));
                if (e.Shadow is not null) f += $" drop-shadow(0 calc(5 * 100cqw / {N(Width)}) calc(8 * 100cqw / {N(Width)}) rgba(0,0,0,.35))";
                st.Append($"filter:{f};");
            }
            if (e.Shadow is { } sh && !sticker)
            {
                var glow = e.Type is "text" ? (e.Color ?? "#111111") : (e.Fill ?? e.Color ?? "#0ea5e9");
                var val = sh switch { "soft" => "0 4px 14px rgba(0,0,0,.28)", "strong" => "0 8px 24px rgba(0,0,0,.5)", _ => $"0 0 22px {glow}" };
                st.Append(e.Type == "text" ? $"text-shadow:{val};" : $"box-shadow:{val};");
            }
            switch (e.Type)
            {
                case "icon":
                    html.Append($"<svg class=\"e\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"{e.Color ?? "#111111"}\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\" style=\"{st}\"><path d=\"{Icons[e.Icon!]}\"/></svg>");
                    break;
                case "text":
                    if (e.Fill is { } tf && tf != "transparent") st.Append($"background:{tf};");
                    html.Append($"<div class=\"e\" style=\"{st}{font}\">{Enc(e.Text ?? "").Replace("\r\n", "\n").Replace("\n", "<br>")}</div>");
                    break;
                case "image":
                    if (e.Src is { } src)
                    {
                        var adjusted = e.ImgZoom > 1 || e.ImgX != 50 || e.ImgY != 50 || e.ImgFit is not null;
                        var gfx = src.StartsWith("/gfx/", StringComparison.Ordinal);
                        if (!adjusted) html.Append($"<img class=\"e\" src=\"{Enc(Asset(src))}\" alt=\"\" loading=\"lazy\" style=\"{st}{(gfx ? "object-fit:contain;" : "")}\">");
                        else
                        {
                            var fit = gfx || e.ImgFit == "contain" ? "contain" : "cover";
                            html.Append($"<div class=\"e\" style=\"{st}overflow:hidden;\"><img src=\"{Enc(Asset(src))}\" alt=\"\" loading=\"lazy\" style=\"display:block;width:100%;height:100%;object-fit:{fit};object-position:{N(e.ImgX)}% {N(e.ImgY)}%;transform:scale({N(e.ImgZoom)});transform-origin:{N(e.ImgX)}% {N(e.ImgY)}%\"></div>");
                        }
                    }
                    break;
                case "shape":
                    html.Append($"<div class=\"e\" style=\"{st}background:{e.Fill ?? "#e2e8f0"};\"></div>");
                    break;
                case "button":
                    var href = e.Kind is { } k && !string.IsNullOrWhiteSpace(e.Value)
                        ? TemplateRenderer.LinkTarget(k, new Dictionary<string, string> { [k] = e.Value }) : null;
                    if (href is not null && trackBase is not null) href = $"{trackBase}/{e.Id}";
                    var look = $"background:{e.Fill ?? "#0b0f14"};" + (e.Color is null ? "color:#fff;" : "");
                    html.Append(href is null
                        ? $"<div class=\"e\" style=\"{st}{look}{font}display:flex;align-items:center;justify-content:center\">{Enc(e.Text ?? "")}</div>"
                        : $"<a class=\"e\" href=\"{Enc(href)}\" style=\"{st}{look}{font}\">{Enc(e.Text ?? "")}</a>");
                    break;
            }
        }
        html.Append("</div>");
        return TemplateRenderer.Render(html.ToString(), css.ToString(), null, assetBase, watermark);
    }
}
