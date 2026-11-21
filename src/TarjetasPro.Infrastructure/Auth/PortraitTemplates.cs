using Microsoft.EntityFrameworkCore;
using TarjetasPro.Application.Templates;
using TarjetasPro.Domain.Entities;

namespace TarjetasPro.Infrastructure.Auth;

/// <summary>
/// Plantillas «Retrato»: la persona (foto sin fondo) sobresale de un fondo de color con su nombre gigante detrás.
/// Pensadas para tarjetas personales: el cliente sube su foto, el editor le quita el fondo y el color lo elige la plantilla.
/// Se actualizan al arrancar si cambia su diseño.
/// </summary>
public static class PortraitTemplates
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

    private record Palette(string Name, string Profession, string From, string To, string Surface, string Text, string Muted, string Button, string ButtonText);

    private static readonly Palette[] Palettes =
    [
        new("Azul eléctrico", "Médico / Dentista", "#1747d1", "#19b8f5", "#ffffff", "#0b1220", "#566170", "#1747d1", "#ffffff"),
        new("Esmeralda", "Nutriólogo / Terapeuta", "#0f7a5c", "#34d399", "#ffffff", "#0b1a14", "#506258", "#0f7a5c", "#ffffff"),
        new("Violeta", "Coach / Creativo", "#5b21b6", "#a78bfa", "#ffffff", "#150b24", "#5d546b", "#5b21b6", "#ffffff"),
        new("Coral", "Estilista / Belleza", "#e11d48", "#fb923c", "#ffffff", "#240a12", "#6b545a", "#e11d48", "#ffffff"),
        new("Medianoche", "Abogado / Ejecutivo", "#0b1220", "#1e3a8a", "#ffffff", "#0b1220", "#566170", "#0b1220", "#ffffff"),
        new("Dorado", "Inmobiliario / Asesor", "#7c5a12", "#e6b422", "#fffdf7", "#231a07", "#6b5d3c", "#7c5a12", "#ffffff"),
    ];

    private const string Html = """
        <div class="page pt">
          <header class="hero">
            <p class="ghost" aria-hidden="true">{{nombre}}</p>
            {{#foto}}<img class="cut" src="{{foto}}" alt="{{nombre}}">{{/foto}}
          </header>
          <section class="who">
            <h1>{{nombre}}</h1>
            {{#puesto}}<p class="role">{{puesto}}</p>{{/puesto}}
            {{#empresa}}<p class="firm">{{empresa}}</p>{{/empresa}}
            {{#eslogan}}<p class="lede">{{eslogan}}</p>{{/eslogan}}
          </section>
          {{#logo}}<img class="logo" src="{{logo}}" alt="{{nombre}}">{{/logo}}
          {{#descripcion}}<p class="bio">{{descripcion}}</p>{{/descripcion}}
          {{#whatsapp}}<a class="cta" href="https://wa.me/{{whatsapp}}">Escribir por WhatsApp</a>{{/whatsapp}}
          <nav class="rows" aria-label="Contacto">
            {{#telefono}}<a class="row" href="tel:{{telefono}}"><b>Teléfono</b><span>{{telefono}}</span></a>{{/telefono}}
            {{#whatsapp2}}<a class="row" href="https://wa.me/{{whatsapp2}}"><b>WhatsApp</b><span>{{whatsapp2}}</span></a>{{/whatsapp2}}
            {{#email}}<a class="row" href="mailto:{{email}}"><b>Correo</b><span>{{email}}</span></a>{{/email}}
            {{#web}}<a class="row" href="{{web}}"><b>Sitio web</b><span>{{web}}</span></a>{{/web}}
            {{#instagram}}<a class="row" href="{{instagram}}"><b>Instagram</b><span>Ver perfil</span></a>{{/instagram}}
            {{#facebook}}<a class="row" href="{{facebook}}"><b>Facebook</b><span>Ver perfil</span></a>{{/facebook}}
            {{#tiktok}}<a class="row" href="{{tiktok}}"><b>TikTok</b><span>Ver perfil</span></a>{{/tiktok}}
            {{#linkedin}}<a class="row" href="{{linkedin}}"><b>LinkedIn</b><span>Ver perfil</span></a>{{/linkedin}}
          </nav>
          {{#has_servicios}}<section class="blk"><h2>Servicios</h2><ul>{{#servicios}}<li>{{.}}</li>{{/servicios}}</ul></section>{{/has_servicios}}
          {{#ubicacion}}<section class="blk"><h2>Ubicación</h2><p>{{ubicacion}}</p>{{#mapa}}<p><a class="lnk" href="{{mapa}}">Ver en el mapa</a></p>{{/mapa}}</section>{{/ubicacion}}
          {{#horarios}}<section class="blk"><h2>Horarios</h2><p>{{horarios}}</p></section>{{/horarios}}
        </div>
        """;

    private const string Css = """
        @import url('https://fonts.googleapis.com/css2?family=Poppins:wght@400;600;800&display=swap');
        *{box-sizing:border-box}
        body{margin:0;background:[[surface]];color:[[text]];font:400 16px/1.55 'Poppins',system-ui,sans-serif;-webkit-font-smoothing:antialiased}
        a{color:inherit;text-decoration:none}a:focus-visible{outline:2px solid [[from]];outline-offset:3px}
        h1,h2,p{margin:0}
        .pt{max-width:440px;margin:0 auto;min-height:100vh;background:[[surface]];padding-bottom:48px;overflow:hidden}
        .pt .hero{position:relative;height:230px;overflow:hidden;background:radial-gradient(260px 200px at 85% 8%,rgba(255,255,255,.28),transparent 70%),linear-gradient(160deg,[[from]],[[to]]);border-radius:0 0 36px 36px}
        .pt .ghost{position:absolute;inset:26px 14px auto;text-align:center;overflow-wrap:anywhere;font:800 4rem/.92 'Poppins',sans-serif;letter-spacing:-.04em;text-transform:uppercase;color:rgba(255,255,255,.2)}
        .pt .hero:has(.cut){height:420px}
        .pt .cut{position:absolute;left:50%;bottom:0;transform:translateX(-50%);height:96%;width:auto;max-width:100%;object-fit:contain;object-position:bottom;filter:drop-shadow(0 18px 24px rgba(0,0,0,.28))}
        .pt .who{padding:22px 26px 0}
        .pt h1{font-size:2.1rem;line-height:1.05;font-weight:800;letter-spacing:-.03em}
        .pt .role{margin-top:8px;font-weight:600;color:[[from]]}
        .pt .firm{color:[[muted]];font-size:.94rem}
        .pt .lede{margin-top:12px;font-size:1.05rem;line-height:1.4}
        .pt .logo{display:block;max-height:34px;max-width:150px;object-fit:contain;margin:20px 26px 0}
        .pt .bio{margin:14px 26px 0;color:[[muted]]}
        .pt .cta{display:flex;align-items:center;justify-content:center;margin:24px 26px 0;padding:16px 18px;border-radius:999px;background:[[button]];color:[[buttonText]];font-weight:600;box-shadow:0 12px 24px -12px [[button]];transition:transform .16s cubic-bezier(.23,1,.32,1)}
        .pt .cta:active,.pt .row:active{transform:scale(.985)}
        .pt .rows{margin:22px 26px 0;display:grid;gap:8px}
        .pt .row{display:flex;justify-content:space-between;align-items:baseline;gap:16px;padding:14px 18px;border-radius:16px;background:color-mix(in srgb,[[from]] 7%,transparent);transition:transform .16s cubic-bezier(.23,1,.32,1)}
        .pt .row b{flex:none;font-weight:400;color:[[muted]];font-size:.86rem}.pt .row span{font-weight:600;text-align:right;min-width:0;overflow-wrap:anywhere}
        .pt .blk{margin:30px 26px 0}.pt h2{font-size:1.15rem;font-weight:800;margin-bottom:8px}
        .pt .blk p{color:[[muted]]}.pt .lnk{color:[[from]];font-weight:600;text-decoration:underline;text-underline-offset:3px}
        .pt .blk ul{margin:0;padding:0;list-style:none}.pt .blk li{padding:10px 0;border-top:1px solid color-mix(in srgb,[[text]] 10%,transparent)}
        @media (prefers-reduced-motion:no-preference){.pt .cut{animation:up .7s cubic-bezier(.23,1,.32,1) both}@keyframes up{from{opacity:0;transform:translate(-50%,24px)}}}
        @media (hover:hover){.pt .row:hover{background:color-mix(in srgb,[[from]] 12%,transparent)}.pt .cta:hover{filter:brightness(1.06)}}
        """;

    public static IEnumerable<Template> All() => Palettes.Select(p =>
    {
        var css = Css.Replace("[[from]]", p.From).Replace("[[to]]", p.To).Replace("[[surface]]", p.Surface)
            .Replace("[[text]]", p.Text).Replace("[[muted]]", p.Muted).Replace("[[buttonText]]", p.ButtonText).Replace("[[button]]", p.Button);
        return new Template
        {
            Name = $"{p.Name} · Retrato",
            Code = $"retrato-{p.Name.ToLowerInvariant().Replace("á", "a").Replace("é", "e").Replace("ó", "o").Replace("í", "i").Replace(' ', '-')}",
            Description = "Retrato · Tu foto sin fondo sobre un color vivo, con tu nombre gigante detrás.",
            Category = "General",
            Profession = p.Profession,
            Html = Html,
            Css = css,
            FieldKeys = string.Join(',', TemplateRenderer.ExtractFields(Html)),
        };
    });
}
