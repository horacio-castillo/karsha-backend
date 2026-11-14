using Microsoft.EntityFrameworkCore;
using TarjetasPro.Application.Templates;
using TarjetasPro.Domain.Entities;

namespace TarjetasPro.Infrastructure.Auth;

/// <summary>
/// Plantillas ejecutivas: 5 composiciones distintas (no el mismo diseño con otro color) × 4 paletas = 20.
/// Pensadas para profesionistas que necesitan verse serios: sin emojis, sin botones de colores, mucho aire.
/// Se actualizan al arrancar si cambia su diseño.
/// </summary>
public static class ExecutiveTemplates
{
    public static async Task SeedAsync(AppDbContext db)
    {
        var rows = await db.Templates.ToListAsync();
        foreach (var fresh in All())
        {
            var row = rows.FirstOrDefault(t => t.Code == fresh.Code);
            if (row is null) { db.Templates.Add(fresh); continue; }
            row.Name = fresh.Name; row.Description = fresh.Description; row.Category = fresh.Category;
            row.Profession = fresh.Profession; row.Html = fresh.Html; row.Css = fresh.Css;
            row.FieldKeys = fresh.FieldKeys; row.UpdatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync();
    }

    private record Family(string Key, string Label, string Html, string Css, string Fonts, string Head, string Body, string Blurb);

    private record Palette(string Family, string Name, string Category, string Profession,
        string Bg, string Surface, string Text, string Muted, string Primary, string Accent);

    private static readonly Family[] Families =
    [
        new("editorial", "Editorial", EditorialHtml, Common + EditorialCss,
            "Newsreader:wght@500;600&family=Hanken+Grotesk:wght@400;600", "Newsreader", "Hanken Grotesk",
            "Retrato a ancho completo, nombre grande y contactos como una lista tipográfica."),
        new("membrete", "Membrete", MembreteHtml, Common + MembreteCss,
            "EB+Garamond:ital,wght@0,500;1,400&family=Source+Sans+3:wght@400;600", "EB Garamond", "Source Sans 3",
            "Hoja de papel membretado: centrada, con doble filete y versalitas."),
        new("consejo", "Consejo", ConsejoHtml, Common + ConsejoCss,
            "Instrument+Serif&family=Inter+Tight:wght@400;600", "Instrument Serif", "Inter Tight",
            "Banda de color con el nombre y la foto montada sobre el corte."),
        new("atelier", "Atelier", AtelierHtml, Common + AtelierCss,
            "Hanken+Grotesk:wght@300;400;600", "Hanken Grotesk", "Hanken Grotesk",
            "Minimalista: nombre en peso ligero, mucho espacio y una sola acción."),
        new("dossier", "Dossier", DossierHtml, Common + DossierCss,
            "IBM+Plex+Serif:wght@500;600&family=IBM+Plex+Sans:wght@400;600", "IBM Plex Serif", "IBM Plex Sans",
            "Ficha profesional: columna de rótulos a la izquierda, como un expediente."),
    ];

    private static readonly Palette[] Palettes =
    [
        new("editorial", "Marfil", "General", "Abogado / Notario", "#f4efe6", "#fbf8f2", "#1f1b16", "#675f53", "#7a5a1e", "#a67c2e"),
        new("editorial", "Grafito", "General", "Arquitecto / Ingeniero", "#eceef0", "#f8f9fa", "#14181d", "#58616b", "#243447", "#8c6d3f"),
        new("editorial", "Bosque", "General", "Contador / Asesor fiscal", "#eef0ea", "#f9faf6", "#17231c", "#56635a", "#1f4d3a", "#9a7b3c"),
        new("editorial", "Vino", "General", "Médico especialista", "#f5eeee", "#fcf8f8", "#24141a", "#6a565c", "#6b1f35", "#a8864a"),

        new("membrete", "Notarial", "General", "Notario / Abogado", "#e4e0d6", "#fdfbf6", "#1c2230", "#5d6371", "#1b2a4a", "#a88a4b"),
        new("membrete", "Corporativo", "General", "Consultor / Directivo", "#e6e9ee", "#ffffff", "#1a2230", "#5a6575", "#2b4a70", "#7a8aa3"),
        new("membrete", "Esmeralda", "General", "Médico / Clínica", "#e2e9e5", "#fbfdfb", "#14261f", "#566a61", "#14513e", "#a38a4a"),
        new("membrete", "Borgoña", "General", "Despacho jurídico", "#eae1e1", "#fffbfb", "#26151a", "#6b5860", "#5e1a2c", "#a8864a"),

        new("consejo", "Marino", "General", "Directivo / Consultor", "#e8ecf1", "#ffffff", "#16202e", "#566275", "#12294a", "#c9a45c"),
        new("consejo", "Carbón", "General", "Ingeniero / Constructor", "#e9e9e9", "#ffffff", "#181818", "#5c5c5c", "#222427", "#c08a4a"),
        new("consejo", "Zafiro", "General", "Médico / Especialista", "#e5ecf4", "#ffffff", "#14202e", "#526176", "#1c4a7d", "#cfe0f2"),
        new("consejo", "Oliva", "General", "Asesor / Inmobiliario", "#eaebe3", "#fffffd", "#1d2216", "#5d6453", "#3b4a2a", "#d7c48a"),

        new("atelier", "Blanco", "General", "Consultor / Creativo", "#ffffff", "#ffffff", "#101114", "#5f6368", "#101114", "#8a6d3b"),
        new("atelier", "Negro", "General", "Ejecutivo / Finanzas", "#0f1012", "#17181b", "#f1eee8", "#a39f97", "#d9b97a", "#d9b97a"),
        new("atelier", "Arena", "General", "Arquitecto / Interiorismo", "#efe8dc", "#f7f2e9", "#22190f", "#6a5d4b", "#22190f", "#9c6b2f"),
        new("atelier", "Pizarra", "General", "Asesor financiero / Abogado", "#e9ecef", "#f4f6f8", "#151a20", "#565f6a", "#2e3a46", "#3b6e8f"),

        new("dossier", "Acero", "General", "Abogado / Contador", "#f1f3f5", "#ffffff", "#161b22", "#5a6472", "#2a4365", "#8795a8"),
        new("dossier", "Tinta", "General", "Consultor / Académico", "#f3f1ec", "#fffefb", "#1a1a2e", "#5f5c6e", "#2d2a6e", "#8d8aa8"),
        new("dossier", "Cobre", "General", "Arquitecto / Perito", "#f5efe8", "#fffcf8", "#261a12", "#6b5a4c", "#8a4b24", "#c08a5a"),
        new("dossier", "Pino", "General", "Médico / Nutriólogo", "#edf1ee", "#fbfdfb", "#112019", "#52635a", "#1d5b45", "#7fa190"),
    ];

    public static IEnumerable<Template> All() => Palettes.Select(p =>
    {
        var f = Families.First(x => x.Key == p.Family);
        var html = f.Html.Replace("[[cta]]", Cta).Replace("[[rows]]", Rows).Replace("[[tail]]", Tail);
        var css = f.Css
            .Replace("[[bg]]", p.Bg).Replace("[[surface]]", p.Surface).Replace("[[text]]", p.Text).Replace("[[muted]]", p.Muted)
            .Replace("[[primary]]", p.Primary).Replace("[[accent]]", p.Accent)
            .Replace("[[head]]", $"'{f.Head}',Georgia,serif").Replace("[[body]]", $"'{f.Body}',system-ui,sans-serif");
        if (f.Key == "atelier") css = css.Replace($"'{f.Head}',Georgia,serif", $"'{f.Head}',system-ui,sans-serif");
        css = $"@import url('https://fonts.googleapis.com/css2?family={f.Fonts}&display=swap');\n{css}";
        return new Template
        {
            Name = $"{p.Name} · {f.Label}",
            Code = $"{f.Key}-{p.Name.ToLowerInvariant().Replace("ó", "o").Replace("ñ", "n")}",
            Description = $"{f.Label} · {f.Blurb}",
            Category = p.Category,
            Profession = p.Profession,
            Html = html,
            Css = css,
            FieldKeys = string.Join(',', TemplateRenderer.ExtractFields(html)),
        };
    });

    // ── Piezas compartidas ────────────────────────────────────────────────────────────────────────

    private const string Cta =
        """{{#whatsapp}}<a class="cta" href="https://wa.me/{{whatsapp}}"><span>Escribir por WhatsApp</span></a>{{/whatsapp}}""";

    private const string Rows = """
        <nav class="rows" aria-label="Contacto">
            {{#telefono}}<a class="row" href="tel:{{telefono}}"><b>Teléfono</b><span>{{telefono}}</span></a>{{/telefono}}
            {{#whatsapp2}}<a class="row" href="https://wa.me/{{whatsapp2}}"><b>WhatsApp</b><span>{{whatsapp2}}</span></a>{{/whatsapp2}}
            {{#email}}<a class="row" href="mailto:{{email}}"><b>Correo</b><span>{{email}}</span></a>{{/email}}
            {{#web}}<a class="row" href="{{web}}"><b>Sitio web</b><span>{{web}}</span></a>{{/web}}
            {{#linkedin}}<a class="row" href="{{linkedin}}"><b>LinkedIn</b><span>Ver perfil</span></a>{{/linkedin}}
            {{#instagram}}<a class="row" href="{{instagram}}"><b>Instagram</b><span>Ver perfil</span></a>{{/instagram}}
            {{#facebook}}<a class="row" href="{{facebook}}"><b>Facebook</b><span>Ver perfil</span></a>{{/facebook}}
            {{#tiktok}}<a class="row" href="{{tiktok}}"><b>TikTok</b><span>Ver perfil</span></a>{{/tiktok}}
          </nav>
        """;

    private const string Tail = """
        {{#has_servicios}}<section class="svc"><h2>Servicios</h2><ul>{{#servicios}}<li>{{.}}</li>{{/servicios}}</ul></section>{{/has_servicios}}
          {{#ubicacion}}<section class="loc"><h2>Ubicación</h2><p>{{ubicacion}}</p>{{#mapa}}<p><a class="lnk" href="{{mapa}}">Ver en el mapa</a></p>{{/mapa}}</section>{{/ubicacion}}
          {{#horarios}}<section class="hrs"><h2>Horarios</h2><p>{{horarios}}</p></section>{{/horarios}}
        """;

    /// <summary>Base común: reinicio, selección, foco visible, una entrada suave y feedback al presionar.</summary>
    private const string Common = """
        *{box-sizing:border-box}
        body{margin:0;background:[[bg]];color:[[text]];font:400 16px/1.55 [[body]];-webkit-font-smoothing:antialiased;text-rendering:optimizeLegibility}
        ::selection{background:[[primary]];color:[[surface]]}
        a{color:inherit;text-decoration:none}a:focus-visible{outline:2px solid [[accent]];outline-offset:3px}
        h1,h2{font-family:[[head]];margin:0;text-wrap:balance}p{margin:0}
        .row span{overflow-wrap:anywhere}.lnk{text-decoration:underline;text-underline-offset:3px;text-decoration-thickness:1px}
        .cta{display:flex;align-items:center;justify-content:space-between;gap:12px}
        .cta::after{content:"";flex:none;width:8px;height:8px;border-top:1.5px solid currentColor;border-right:1.5px solid currentColor;transform:rotate(45deg);margin-right:3px}
        .cta,.row{transition:transform .16s cubic-bezier(.23,1,.32,1),background-color .16s,color .16s}
        .cta:active,.row:active{transform:scale(.985)}
        @media (prefers-reduced-motion:no-preference){
          .page>*{animation:rise .55s cubic-bezier(.23,1,.32,1) both}
          .page>:nth-child(2){animation-delay:50ms}.page>:nth-child(3){animation-delay:100ms}
          .page>:nth-child(4){animation-delay:150ms}.page>:nth-child(n+5){animation-delay:200ms}
          @keyframes rise{from{opacity:0;transform:translateY(10px)}}
        }
        """;

    // ── 1 · Editorial ────────────────────────────────────────────────────────────────────────────

    private const string EditorialHtml = """
        <div class="page ed">
          {{#foto}}<img class="portrait" src="{{foto}}" alt="{{nombre}}">{{/foto}}
          <header>
            {{#logo}}<img class="logo" src="{{logo}}" alt="{{nombre}}">{{/logo}}
            <h1>{{nombre}}</h1>
            {{#puesto}}<p class="role">{{puesto}}</p>{{/puesto}}
            {{#empresa}}<p class="firm">{{empresa}}</p>{{/empresa}}
          </header>
          {{#eslogan}}<p class="lede">{{eslogan}}</p>{{/eslogan}}
          {{#descripcion}}<p class="bio">{{descripcion}}</p>{{/descripcion}}
          [[cta]]
          [[rows]]
          [[tail]]
        </div>
        """;

    private const string EditorialCss = """
        .ed{max-width:440px;margin:0 auto;min-height:100vh;background:[[surface]];padding-bottom:48px}
        .ed .portrait{display:block;width:100%;aspect-ratio:4/3.4;object-fit:cover;object-position:center 25%}
        .ed header{padding:30px 26px 0}
        .ed header::before{content:"";display:block;width:36px;height:2px;background:[[accent]];margin-bottom:20px}
        .ed .logo{display:block;max-height:38px;max-width:160px;object-fit:contain;margin:0 0 16px}
        .ed h1{font-size:2.6rem;line-height:1.02;letter-spacing:-.03em;font-weight:500}
        .ed .role{margin-top:14px;font-weight:600;color:[[primary]]}
        .ed .firm{margin-top:2px;color:[[muted]];font-size:.95rem}
        .ed .lede{margin:24px 26px 0;font:500 1.25rem/1.35 [[head]];letter-spacing:-.01em}
        .ed .bio{margin:14px 26px 0;color:[[muted]];max-width:62ch}
        .ed .cta{margin:28px 26px 0;padding:16px 18px;background:[[primary]];color:[[surface]];font-weight:600;border-radius:2px}
        .ed .rows{margin:24px 26px 0;border-top:1px solid color-mix(in srgb,[[text]] 20%,transparent)}
        .ed .row{display:grid;grid-template-columns:88px 1fr;gap:12px;padding:14px 0;border-bottom:1px solid color-mix(in srgb,[[text]] 12%,transparent)}
        .ed .row b{font-weight:400;color:[[muted]];font-size:.86rem;padding-top:2px}.ed .row span{font-weight:600}
        .ed section{margin:32px 26px 0}.ed h2{font-size:1.2rem;font-weight:600;margin-bottom:8px}
        .ed section p{color:[[muted]]}.ed .lnk{color:[[primary]];font-weight:600}
        .ed .svc ul{margin:0;padding:0;list-style:none}
        .ed .svc li{padding:11px 0;border-top:1px solid color-mix(in srgb,[[text]] 12%,transparent)}
        .ed .svc li:last-child{border-bottom:1px solid color-mix(in srgb,[[text]] 12%,transparent)}
        @media (hover:hover){.ed .row:hover{background:color-mix(in srgb,[[primary]] 6%,transparent)}.ed .cta:hover{background:color-mix(in srgb,[[primary]] 88%,#000)}}
        """;

    // ── 2 · Membrete ─────────────────────────────────────────────────────────────────────────────

    private const string MembreteHtml = """
        <div class="page mb">
          <header>
            {{#logo}}<img class="logo" src="{{logo}}" alt="{{nombre}}">{{/logo}}
            {{#foto}}<img class="avatar" src="{{foto}}" alt="{{nombre}}">{{/foto}}
            <h1>{{nombre}}</h1>
            {{#puesto}}<p class="role">{{puesto}}</p>{{/puesto}}
            {{#empresa}}<p class="firm">{{empresa}}</p>{{/empresa}}
          </header>
          {{#eslogan}}<p class="slogan">{{eslogan}}</p>{{/eslogan}}
          {{#descripcion}}<p class="bio">{{descripcion}}</p>{{/descripcion}}
          [[cta]]
          [[rows]]
          [[tail]]
        </div>
        """;

    private const string MembreteCss = """
        .mb{max-width:420px;margin:20px auto;padding:44px 28px 38px;background:[[surface]];text-align:center;
          box-shadow:0 1px 2px rgba(0,0,0,.06),0 18px 36px -14px rgba(0,0,0,.22);
          outline:1px solid color-mix(in srgb,[[primary]] 30%,transparent);outline-offset:-10px}
        .mb .logo{display:block;max-width:150px;max-height:80px;margin:0 auto 14px;object-fit:contain}
        .mb .avatar{display:block;width:96px;height:96px;margin:0 auto;border-radius:50%;object-fit:cover;border:1px solid [[accent]];padding:4px;background:[[surface]]}
        .mb h1{margin-top:18px;font-size:2rem;line-height:1.12;font-weight:500;font-variant-caps:small-caps;letter-spacing:.03em}
        .mb header::after{content:"";display:block;width:64px;height:4px;margin:18px auto 0;border-top:1px solid [[accent]];border-bottom:1px solid [[accent]]}
        .mb .role{margin-top:8px;font:italic 400 1.15rem/1.3 [[head]];color:[[primary]]}
        .mb .firm{margin-top:8px;font-size:.78rem;letter-spacing:.16em;text-transform:uppercase;color:[[muted]]}
        .mb .slogan{margin-top:20px;font:italic 400 1.08rem/1.5 [[head]];color:[[muted]]}
        .mb .bio{margin-top:14px;font-size:.96rem}
        .mb .cta{margin:26px 4px 0;padding:13px 16px;border:1px solid [[primary]];outline:1px solid [[primary]];outline-offset:3px;color:[[primary]];font-weight:600;font-size:.78rem;letter-spacing:.1em;text-transform:uppercase}
        .mb .rows{margin-top:28px;border-top:1px solid color-mix(in srgb,[[accent]] 70%,transparent)}
        .mb .row{display:block;padding:12px 0;border-bottom:1px dotted color-mix(in srgb,[[text]] 28%,transparent)}
        .mb .row b{display:block;font-weight:600;font-size:.68rem;letter-spacing:.18em;text-transform:uppercase;color:[[muted]]}
        .mb .row span{display:block;margin-top:2px;font-weight:600}
        .mb section{margin-top:30px}
        .mb h2{font-size:.78rem;font-family:[[body]];font-weight:600;letter-spacing:.18em;text-transform:uppercase;color:[[primary]];margin-bottom:10px}
        .mb section p{color:[[muted]]}.mb .lnk{color:[[primary]];font-weight:600}
        .mb .svc ul{margin:0;padding:0;list-style:none}.mb .svc li{padding:9px 0;border-bottom:1px dotted color-mix(in srgb,[[text]] 28%,transparent)}
        @media (hover:hover){.mb .cta:hover{background:[[primary]];color:[[surface]]}}
        @media (max-width:460px){.mb{margin:0;min-height:100vh;box-shadow:none}}
        """;

    // ── 3 · Consejo ──────────────────────────────────────────────────────────────────────────────

    private const string ConsejoHtml = """
        <div class="page cn">
          <header>
            <h1>{{nombre}}</h1>
            {{#puesto}}<p class="role">{{puesto}}</p>{{/puesto}}
            {{#empresa}}<p class="firm">{{empresa}}</p>{{/empresa}}
            {{#foto}}<img class="avatar" src="{{foto}}" alt="{{nombre}}">{{/foto}}
          </header>
          {{#logo}}<img class="logo" src="{{logo}}" alt="{{nombre}}">{{/logo}}
          {{#eslogan}}<p class="lede">{{eslogan}}</p>{{/eslogan}}
          {{#descripcion}}<p class="bio">{{descripcion}}</p>{{/descripcion}}
          [[cta]]
          [[rows]]
          [[tail]]
        </div>
        """;

    private const string ConsejoCss = """
        .cn{max-width:440px;margin:0 auto;min-height:100vh;background:[[surface]];padding-bottom:48px}
        .cn header{position:relative;background:[[primary]];color:#fff;padding:44px 26px 72px}
        .cn h1{font-size:2.7rem;line-height:1;font-weight:400;letter-spacing:-.025em;padding-right:4px}
        .cn .role{margin-top:14px;font-weight:600}
        .cn .firm{margin-top:2px;color:[[accent]];font-size:.94rem}
        .cn .avatar{position:absolute;right:24px;bottom:-46px;width:96px;height:96px;object-fit:cover;border-radius:18px;border:4px solid [[surface]];box-shadow:0 10px 22px -10px rgba(0,0,0,.4)}
        .cn .logo{display:block;max-height:34px;max-width:150px;object-fit:contain;margin:30px 26px 0}
        .cn .lede{margin:52px 26px 0;max-width:calc(100% - 52px);font:400 1.4rem/1.28 [[head]];letter-spacing:-.01em}
        .cn .logo + .lede{margin-top:22px}
        .cn .bio{margin:12px 26px 0;color:[[muted]]}
        .cn .cta{margin:26px 26px 0;padding:16px 18px;background:[[accent]];color:[[text]];font-weight:600;border-radius:2px}
        .cn .rows{margin-top:26px;border-top:1px solid color-mix(in srgb,[[text]] 14%,transparent)}
        .cn .row{display:flex;justify-content:space-between;align-items:baseline;gap:18px;padding:15px 26px;border-bottom:1px solid color-mix(in srgb,[[text]] 10%,transparent)}
        .cn .row b{flex:none;font-weight:400;color:[[muted]];font-size:.86rem}.cn .row span{font-weight:600;text-align:right;min-width:0}
        .cn section{margin:32px 26px 0}.cn h2{font-size:1.35rem;font-weight:400;margin-bottom:8px}
        .cn section p{color:[[muted]]}.cn .lnk{color:[[primary]];font-weight:600}
        .cn .svc ul{margin:0;padding:0;list-style:none;display:grid;gap:0}
        .cn .svc li{padding:11px 0;border-top:1px solid color-mix(in srgb,[[text]] 12%,transparent)}
        @media (hover:hover){.cn .row:hover{background:color-mix(in srgb,[[primary]] 6%,transparent)}.cn .cta:hover{filter:brightness(.96)}}
        """;

    // ── 4 · Atelier ──────────────────────────────────────────────────────────────────────────────

    private const string AtelierHtml = """
        <div class="page at">
          <header>
            {{#foto}}<img class="avatar" src="{{foto}}" alt="{{nombre}}">{{/foto}}
            {{#logo}}<img class="logo" src="{{logo}}" alt="{{nombre}}">{{/logo}}
            <h1>{{nombre}}</h1>
            {{#puesto}}<p class="role">{{puesto}}</p>{{/puesto}}
            {{#empresa}}<p class="firm">{{empresa}}</p>{{/empresa}}
          </header>
          {{#eslogan}}<p class="slogan">{{eslogan}}</p>{{/eslogan}}
          {{#descripcion}}<p class="bio">{{descripcion}}</p>{{/descripcion}}
          [[cta]]
          [[rows]]
          [[tail]]
        </div>
        """;

    private const string AtelierCss = """
        .at{max-width:440px;margin:0 auto;min-height:100vh;padding:52px 28px 60px}
        .at .avatar{display:block;width:104px;height:104px;object-fit:cover;border-radius:3px}
        .at .logo{display:block;max-height:38px;max-width:150px;object-fit:contain}
        .at .avatar + h1,.at .logo + h1{margin-top:34px}
        .at h1{font-size:2.8rem;line-height:1;font-weight:300;letter-spacing:-.035em}
        .at .role{margin-top:16px;font-size:.78rem;font-weight:600;letter-spacing:.16em;text-transform:uppercase}
        .at .firm{margin-top:4px;color:[[muted]];font-size:.95rem}
        .at .slogan{margin-top:36px;font-size:1.2rem;line-height:1.42;font-weight:300}
        .at .bio{margin-top:14px;color:[[muted]];font-size:.95rem}
        .at .cta{margin-top:36px;padding:17px 20px;background:[[text]];color:[[bg]];font-weight:600;font-size:.95rem;border-radius:2px}
        .at .rows{margin-top:32px;border-top:1px solid color-mix(in srgb,[[text]] 22%,transparent)}
        .at .row{display:flex;flex-direction:row-reverse;justify-content:space-between;align-items:baseline;gap:16px;padding:17px 0;border-bottom:1px solid color-mix(in srgb,[[text]] 14%,transparent)}
        .at .row b{flex:none;font-weight:600;color:[[muted]];font-size:.68rem;letter-spacing:.16em;text-transform:uppercase}
        .at .row span{font-size:1.05rem;font-weight:300;min-width:0}
        .at section{margin-top:40px}
        .at h2{font-family:[[body]];font-size:.68rem;font-weight:600;letter-spacing:.16em;text-transform:uppercase;color:[[muted]];margin-bottom:12px}
        .at section p{font-weight:300;font-size:1.02rem}.at .lnk{color:[[accent]];font-weight:600}
        .at .svc ul{margin:0;padding:0;list-style:none;columns:2;column-gap:20px}
        .at .svc li{break-inside:avoid;padding:5px 0;font-weight:300}
        @media (hover:hover){.at .row:hover span{color:[[accent]]}.at .cta:hover{opacity:.9}}
        """;

    // ── 5 · Dossier ──────────────────────────────────────────────────────────────────────────────

    private const string DossierHtml = """
        <div class="page ds">
          <header>
            {{#logo}}<img class="logo" src="{{logo}}" alt="{{nombre}}">{{/logo}}
            <h1>{{nombre}}</h1>
            {{#puesto}}<p class="role">{{puesto}}</p>{{/puesto}}
            {{#empresa}}<p class="firm">{{empresa}}</p>{{/empresa}}
            {{#eslogan}}<p class="slogan">{{eslogan}}</p>{{/eslogan}}
            {{#foto}}<img class="avatar" src="{{foto}}" alt="{{nombre}}">{{/foto}}
          </header>
          {{#descripcion}}<section class="blk"><h2>Perfil</h2><p class="bio">{{descripcion}}</p></section>{{/descripcion}}
          <section class="blk"><h2>Contacto</h2><div>
            [[cta]]
            [[rows]]
          </div></section>
          {{#has_servicios}}<section class="blk"><h2>Servicios</h2><ul>{{#servicios}}<li>{{.}}</li>{{/servicios}}</ul></section>{{/has_servicios}}
          {{#ubicacion}}<section class="blk"><h2>Ubicación</h2><div><p>{{ubicacion}}</p>{{#mapa}}<p><a class="lnk" href="{{mapa}}">Ver en el mapa</a></p>{{/mapa}}</div></section>{{/ubicacion}}
          {{#horarios}}<section class="blk"><h2>Horarios</h2><p>{{horarios}}</p></section>{{/horarios}}
        </div>
        """;

    private const string DossierCss = """
        .ds{max-width:440px;margin:0 auto;min-height:100vh;background:[[surface]];padding:32px 22px 48px}
        .ds header{display:grid;grid-template-columns:minmax(0,1fr) auto;column-gap:16px;align-items:start;padding-bottom:22px;border-bottom:2px solid [[text]]}
        .ds header>*{grid-column:1}
        .ds .avatar{grid-column:2;grid-row:1/span 5;width:88px;height:112px;object-fit:cover;border-radius:2px}
        .ds .logo{max-height:32px;max-width:140px;object-fit:contain;margin-bottom:14px}
        .ds h1{font-size:2.05rem;line-height:1.08;letter-spacing:-.02em;font-weight:600}
        .ds .role{margin-top:8px;color:[[primary]];font-weight:600}
        .ds .firm{color:[[muted]];font-size:.92rem}
        .ds .slogan{margin-top:10px;color:[[muted]];font-size:.92rem;font-style:italic}
        .ds .blk{display:grid;grid-template-columns:86px minmax(0,1fr);gap:14px;padding:20px 0;border-bottom:1px solid color-mix(in srgb,[[text]] 14%,transparent)}
        .ds .blk h2{font:600 .7rem/1.9 [[body]];letter-spacing:.12em;text-transform:uppercase;color:[[muted]]}
        .ds .bio,.ds .blk p{font-size:.96rem}.ds .blk p+p{margin-top:6px}
        .ds .lnk{color:[[primary]];font-weight:600}
        .ds .cta{width:fit-content;max-width:100%;padding:12px 16px;margin-bottom:10px;background:[[primary]];color:#fff;font-weight:600;font-size:.92rem;border-radius:2px}
        .ds .row{display:block;padding:9px 0}.ds .row+.row{border-top:1px solid color-mix(in srgb,[[text]] 10%,transparent)}
        .ds .row b{display:block;font-weight:400;font-size:.74rem;color:[[muted]]}.ds .row span{font-weight:600}
        .ds ul{margin:0;padding:0;list-style:none}.ds li{padding:5px 0;font-size:.96rem}.ds li::before{content:"–";margin-right:8px;color:[[accent]]}
        @media (hover:hover){.ds .row:hover span{color:[[primary]]}.ds .cta:hover{filter:brightness(1.12)}}
        """;
}
