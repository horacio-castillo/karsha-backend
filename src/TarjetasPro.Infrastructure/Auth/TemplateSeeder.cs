using Microsoft.EntityFrameworkCore;
using TarjetasPro.Application.Templates;
using TarjetasPro.Domain.Entities;

namespace TarjetasPro.Infrastructure.Auth;

public static class TemplateSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (!await db.TemplateCategories.AnyAsync())
        {
            db.TemplateCategories.AddRange(TarjetasPro.Domain.Cards.TemplateCategories.All
                .Select((name, i) => new TarjetasPro.Domain.Entities.TemplateCategory { Name = name, SortOrder = i }));
            await db.SaveChangesAsync();
        }
        else
        {
            // Bases ya sembradas: solo agrega los giros nuevos del catálogo, sin tocar los que el admin editó.
            var names = (await db.TemplateCategories.Select(c => c.Name).ToListAsync()).ToHashSet();
            var next = await db.TemplateCategories.MaxAsync(c => c.SortOrder) + 1;
            foreach (var name in TarjetasPro.Domain.Cards.TemplateCategories.All.Where(n => !names.Contains(n)))
                db.TemplateCategories.Add(new TarjetasPro.Domain.Entities.TemplateCategory { Name = name, SortOrder = next++ });
            await db.SaveChangesAsync();
        }

        var existingRows = await db.Templates.ToListAsync();
        var existing = existingRows.Select(t => t.Code).ToHashSet();

        // Las plantillas por profesión son del catálogo: se actualizan al cambiar su diseño.
        foreach (var fresh in ProfessionThemes.All())
        {
            var row = existingRows.FirstOrDefault(t => t.Code == fresh.Code);
            if (row is null) continue;
            row.Name = fresh.Name; row.Description = fresh.Description; row.Category = fresh.Category;
            row.Profession = fresh.Profession; row.Html = fresh.Html; row.Css = fresh.Css;
            row.FieldKeys = fresh.FieldKeys; row.UpdatedAt = DateTime.UtcNow;
        }

        const string body = """
            <div class="card">
              {{#foto}}<img class="avatar" src="{{foto}}" alt="{{nombre}}">{{/foto}}
              <h1>{{nombre}}</h1>
              {{#puesto}}<p class="role">{{puesto}}</p>{{/puesto}}
              {{#empresa}}<p class="company">{{empresa}}</p>{{/empresa}}
              {{#descripcion}}<p class="bio">{{descripcion}}</p>{{/descripcion}}
              <div class="actions">
                {{#whatsapp}}<a class="btn wa" href="https://wa.me/{{whatsapp}}">WhatsApp</a>{{/whatsapp}}
                {{#telefono}}<a class="btn" href="tel:{{telefono}}">Llamar</a>{{/telefono}}
                {{#email}}<a class="btn" href="mailto:{{email}}">Correo</a>{{/email}}
                {{#web}}<a class="btn" href="{{web}}">Sitio web</a>{{/web}}
                {{#facebook}}<a class="btn" href="{{facebook}}">Facebook</a>{{/facebook}}
                {{#instagram}}<a class="btn" href="{{instagram}}">Instagram</a>{{/instagram}}
                {{#tiktok}}<a class="btn" href="{{tiktok}}">TikTok</a>{{/tiktok}}
                {{#linkedin}}<a class="btn" href="{{linkedin}}">LinkedIn</a>{{/linkedin}}
              </div>
              {{#ubicacion}}<p class="info">📍 {{ubicacion}}{{#mapa}} · <a href="{{mapa}}">Ver mapa</a>{{/mapa}}</p>{{/ubicacion}}
              {{#horarios}}<p class="info">🕒<br>{{horarios}}</p>{{/horarios}}
            </div>
            """;

        const string clasicaCss = """
            *{box-sizing:border-box}body{margin:0;min-height:100vh;display:flex;justify-content:center;align-items:flex-start;padding:24px 16px;background:#eef2f7;font-family:system-ui,sans-serif;color:#1f2937}
            .card{width:100%;max-width:400px;background:#fff;border-radius:20px;padding:28px 24px;text-align:center;box-shadow:0 10px 30px rgba(0,0,0,.08)}
            .avatar{width:110px;height:110px;border-radius:50%;object-fit:cover;border:4px solid #2563eb}
            h1{margin:12px 0 4px;font-size:1.6rem}.role{margin:0;color:#2563eb;font-weight:600}.company{margin:2px 0 0;color:#6b7280}
            .bio{margin:16px 0;line-height:1.5}.actions{display:grid;gap:10px;margin:18px 0}
            .btn{display:block;padding:12px;border-radius:12px;background:#2563eb;color:#fff;text-decoration:none;font-weight:600}
            .btn.wa{background:#16a34a}.info{color:#4b5563;font-size:.95rem;line-height:1.5}.info a{color:#2563eb}
            """;

        const string modernaCss = """
            *{box-sizing:border-box}body{margin:0;min-height:100vh;display:flex;justify-content:center;align-items:flex-start;padding:24px 16px;background:linear-gradient(160deg,#0f172a,#312e81);font-family:system-ui,sans-serif;color:#e2e8f0}
            .card{width:100%;max-width:400px;background:rgba(255,255,255,.07);border:1px solid rgba(255,255,255,.15);border-radius:24px;padding:32px 24px;text-align:center;backdrop-filter:blur(8px)}
            .avatar{width:120px;height:120px;border-radius:30px;object-fit:cover;box-shadow:0 0 0 3px #a78bfa}
            h1{margin:14px 0 4px;font-size:1.7rem;color:#fff}.role{margin:0;color:#c4b5fd;font-weight:600}.company{margin:2px 0 0;color:#94a3b8}
            .bio{margin:16px 0;line-height:1.5;color:#cbd5e1}.actions{display:flex;flex-wrap:wrap;gap:10px;justify-content:center;margin:18px 0}
            .btn{padding:10px 16px;border-radius:999px;background:#7c3aed;color:#fff;text-decoration:none;font-weight:600;font-size:.95rem}
            .btn.wa{background:#22c55e}.info{color:#94a3b8;font-size:.95rem;line-height:1.5}.info a{color:#c4b5fd}
            """;

        // «Custom»: la imagen que sube el cliente es el fondo y los datos que llene van encima.
        const string customHtml = """
            <div class="page">
              {{#imagen}}<img class="bgimg" src="{{imagen}}" alt="">{{/imagen}}
              <div class="shade"></div>
              <div class="content">
                {{#nombre}}<h1>{{nombre}}</h1>{{/nombre}}
                {{#puesto}}<p class="role">{{puesto}}</p>{{/puesto}}
                {{#empresa}}<p class="company">{{empresa}}</p>{{/empresa}}
                {{#eslogan}}<p class="company">{{eslogan}}</p>{{/eslogan}}
                {{#descripcion}}<p class="bio">{{descripcion}}</p>{{/descripcion}}
                <div class="actions">
                  {{#whatsapp}}<a class="btn wa" href="https://wa.me/{{whatsapp}}">WhatsApp</a>{{/whatsapp}}
                  {{#whatsapp2}}<a class="btn wa" href="https://wa.me/{{whatsapp2}}">WhatsApp 2</a>{{/whatsapp2}}
                  {{#telefono}}<a class="btn" href="tel:{{telefono}}">Llamar</a>{{/telefono}}
                  {{#email}}<a class="btn" href="mailto:{{email}}">Correo</a>{{/email}}
                  {{#web}}<a class="btn" href="{{web}}">Sitio web</a>{{/web}}
                  {{#facebook}}<a class="btn" href="{{facebook}}">Facebook</a>{{/facebook}}
                  {{#instagram}}<a class="btn" href="{{instagram}}">Instagram</a>{{/instagram}}
                  {{#tiktok}}<a class="btn" href="{{tiktok}}">TikTok</a>{{/tiktok}}
                  {{#linkedin}}<a class="btn" href="{{linkedin}}">LinkedIn</a>{{/linkedin}}
                </div>
                {{#has_servicios}}<ul class="svc">{{#servicios}}<li>{{.}}</li>{{/servicios}}</ul>{{/has_servicios}}
                {{#ubicacion}}<p class="info">📍 {{ubicacion}}{{#mapa}} · <a href="{{mapa}}">Ver mapa</a>{{/mapa}}</p>{{/ubicacion}}
                {{#horarios}}<p class="info">🕒<br>{{horarios}}</p>{{/horarios}}
              </div>
            </div>
            """;
        const string customCss = """
            *{box-sizing:border-box}body{margin:0;background:#111827;font-family:system-ui,sans-serif;color:#fff}
            .page{position:relative;width:100%;max-width:480px;min-height:100vh;margin:0 auto;overflow:hidden;background:#1f2937}
            .bgimg{position:absolute;inset:0;width:100%;height:100%;object-fit:cover;z-index:0}
            .shade{position:absolute;inset:0;z-index:0;background:linear-gradient(rgba(0,0,0,.35),rgba(0,0,0,.6))}
            .content{position:relative;z-index:1;min-height:100vh;display:flex;flex-direction:column;justify-content:center;padding:40px 24px;text-align:center;text-shadow:0 1px 6px rgba(0,0,0,.6)}
            h1{margin:0 0 6px;font-size:2rem}.role{margin:0;font-weight:600;color:#fde68a}.company{margin:2px 0 0;opacity:.9}
            .bio{margin:16px 0;line-height:1.5}.actions{display:grid;gap:10px;margin:18px 0}
            .btn{display:block;padding:12px;border-radius:12px;background:rgba(255,255,255,.2);border:1px solid rgba(255,255,255,.55);backdrop-filter:blur(4px);color:#fff;text-decoration:none;font-weight:600;text-shadow:none}
            .btn.wa{background:#16a34a;border-color:#16a34a}
            .svc{list-style:none;margin:12px 0;padding:0;text-align:left}.svc li{padding:6px 0;border-bottom:1px solid rgba(255,255,255,.3)}
            .info{font-size:.95rem;line-height:1.5}.info a{color:#fde68a}
            """;

        var all = new[]
        {
            Make("Custom (tu propia imagen)", "custom-foto", "Tu imagen de fondo con los datos que elijas encima.", "General", "", customHtml, customCss),
            Make("Clásica", "clasica", "Fondo claro, botones grandes y foto circular.", "General", "", body, clasicaCss),
            Make("Moderna", "moderna", "Estilo oscuro con degradado y botones redondeados.", "General", "", body, modernaCss),
            Make("Pediatra", "pediatra-pastel", "Colores pastel, botones redondeados y lista de servicios.", "Salud", "Pediatra", PediatraHtml, PediatraCss),
            Make("Técnico", "tecnico-oscuro", "Oscuro con amarillo, botón grande de cotización y servicios.", "Oficios y servicios", "Técnico / Plomero / Electricista", TecnicoHtml, TecnicoCss),
        };
        if (existingRows.FirstOrDefault(t => t.Code == "custom-foto") is { } custom)
        {
            custom.Html = all[0].Html; custom.Css = all[0].Css; custom.FieldKeys = all[0].FieldKeys; custom.UpdatedAt = DateTime.UtcNow;
        }
        db.Templates.AddRange(all.Concat(ProfessionThemes.All()).Where(t => !existing.Contains(t.Code)));
        await db.SaveChangesAsync();
    }

    private static Template Make(string name, string code, string desc, string category, string profession,
        string html, string css) => new()
    {
        Name = name,
        Code = code,
        Description = desc,
        Category = category,
        Profession = profession,
        Html = html,
        Css = css,
        FieldKeys = string.Join(',', TemplateRenderer.ExtractFields(html)),
    };

    private const string PediatraHtml = """
        <div class="page">
          <div class="blob b1"></div><div class="blob b2"></div>
          <header>
            {{#logo}}<img class="logo" src="{{logo}}" alt="{{nombre}}">{{/logo}}
            <h1>{{nombre}}</h1>
            {{#puesto}}<p class="spec">{{puesto}}</p>{{/puesto}}
            {{#eslogan}}<p class="slogan">{{eslogan}}</p>{{/eslogan}}
          </header>
          <nav class="links">
            {{#whatsapp}}<a class="link pink" href="https://wa.me/{{whatsapp}}"><i>💬</i><span>WhatsApp</span><b>›</b></a>{{/whatsapp}}
            {{#whatsapp2}}<a class="link teal" href="https://wa.me/{{whatsapp2}}"><i>💬</i><span>WhatsApp 2</span><b>›</b></a>{{/whatsapp2}}
            {{#telefono}}<a class="link pink" href="tel:{{telefono}}"><i>📞</i><span>Llamar</span><b>›</b></a>{{/telefono}}
            {{#email}}<a class="link teal" href="mailto:{{email}}"><i>✉️</i><span>Correo</span><b>›</b></a>{{/email}}
            {{#facebook}}<a class="link pink" href="{{facebook}}"><i>📘</i><span>Facebook</span><b>›</b></a>{{/facebook}}
            {{#instagram}}<a class="link teal" href="{{instagram}}"><i>📷</i><span>Instagram</span><b>›</b></a>{{/instagram}}
            {{#web}}<a class="link pink" href="{{web}}"><i>🌐</i><span>Sitio web</span><b>›</b></a>{{/web}}
          </nav>
          {{#ubicacion}}<section><h2>📍 Ubicación</h2><p>{{ubicacion}}</p>{{#mapa}}<p><a href="{{mapa}}">Ver en el mapa</a></p>{{/mapa}}</section>{{/ubicacion}}
          {{#horarios}}<section><h2>🕒 Horarios</h2><p>{{horarios}}</p></section>{{/horarios}}
          {{#has_servicios}}<section class="svc"><h2>Servicios</h2><ul>{{#servicios}}<li><span>✚</span>{{.}}</li>{{/servicios}}</ul></section>{{/has_servicios}}
        </div>
        """;

    private const string PediatraCss = """
        *{box-sizing:border-box}body{margin:0;background:#fdf2f4;font-family:'Trebuchet MS',system-ui,sans-serif;color:#5b3a4a}
        .page{position:relative;overflow:hidden;max-width:440px;margin:0 auto;min-height:100vh;padding:28px 20px 40px;background:linear-gradient(180deg,#fff7f3 0%,#fdeef2 60%,#e6f6f4 100%)}
        .blob{position:absolute;border-radius:50%;opacity:.55}.b1{width:220px;height:220px;right:-70px;top:-60px;background:#bfe8e2}.b2{width:200px;height:200px;left:-80px;bottom:120px;background:#ffd1dc}
        .page>*{position:relative;z-index:1}.page>.blob{position:absolute}
        header{text-align:center}.logo{max-width:180px;max-height:130px;object-fit:contain}
        h1{margin:10px 0 2px;font-size:2rem;color:#c2527a;font-weight:600}
        .spec{margin:0;letter-spacing:.12em;text-transform:uppercase;font-size:.8rem;font-weight:700;color:#3a9d97}.slogan{margin:8px 0 0;font-style:italic;color:#8a6a78}
        .links{display:flex;flex-direction:column;gap:12px;margin:22px 0}
        .link{display:flex;align-items:center;gap:12px;padding:12px 14px;border-radius:14px;text-decoration:none;color:#5b3a4a;font-weight:700;letter-spacing:.06em;text-transform:uppercase;font-size:.85rem;background:#fff;box-shadow:0 4px 10px rgba(194,82,122,.15)}
        .link i{font-style:normal;width:34px;height:34px;display:grid;place-items:center;border-radius:50%;font-size:1rem}.link span{flex:1;text-align:center}.link b{font-size:1.3rem;color:#c2527a}
        .pink i{background:#ffd9e3}.teal i{background:#cdeeea}.teal b{color:#3a9d97}
        section{margin:22px 0;text-align:center}h2{margin:0 0 8px;font-size:.95rem;letter-spacing:.14em;text-transform:uppercase;color:#c2527a}
        section p{margin:0 0 6px;line-height:1.5}section a{color:#3a9d97;font-weight:700}
        .svc ul{list-style:none;margin:0;padding:0;display:grid;grid-template-columns:1fr 1fr;gap:10px;text-align:left}
        .svc li{display:flex;gap:8px;align-items:flex-start;background:rgba(255,255,255,.75);border-radius:12px;padding:10px;font-size:.85rem;line-height:1.3}.svc li span{color:#3a9d97;font-weight:900}
        """;

    private const string TecnicoHtml = """
        <div class="page">
          <header>
            {{#foto}}<img class="avatar" src="{{foto}}" alt="{{nombre}}">{{/foto}}
            <h1>{{nombre}}</h1>
            {{#puesto}}<p class="spec">{{puesto}}</p>{{/puesto}}
            {{#eslogan}}<p class="slogan">{{eslogan}}</p>{{/eslogan}}
          </header>
          {{#whatsapp}}<a class="cta" href="https://wa.me/{{whatsapp}}">💬 Cotiza por WhatsApp</a>{{/whatsapp}}
          <div class="row">
            {{#telefono}}<a class="btn" href="tel:{{telefono}}">📞 Llamar</a>{{/telefono}}
            {{#whatsapp2}}<a class="btn" href="https://wa.me/{{whatsapp2}}">💬 WhatsApp 2</a>{{/whatsapp2}}
            {{#email}}<a class="btn" href="mailto:{{email}}">✉️ Correo</a>{{/email}}
            {{#facebook}}<a class="btn" href="{{facebook}}">📘 Facebook</a>{{/facebook}}
            {{#instagram}}<a class="btn" href="{{instagram}}">📷 Instagram</a>{{/instagram}}
            {{#web}}<a class="btn" href="{{web}}">🌐 Web</a>{{/web}}
          </div>
          {{#has_servicios}}<section><h2>Servicios</h2><ul>{{#servicios}}<li>{{.}}</li>{{/servicios}}</ul></section>{{/has_servicios}}
          {{#descripcion}}<section><p>{{descripcion}}</p></section>{{/descripcion}}
          {{#ubicacion}}<section><h2>Ubicación</h2><p>📍 {{ubicacion}}</p>{{#mapa}}<p><a href="{{mapa}}">Ver en el mapa</a></p>{{/mapa}}</section>{{/ubicacion}}
          {{#horarios}}<section><h2>Horarios</h2><p>{{horarios}}</p></section>{{/horarios}}
        </div>
        """;

    private const string TecnicoCss = """
        *{box-sizing:border-box}body{margin:0;background:#16181d;font-family:system-ui,sans-serif;color:#e5e7eb}
        .page{max-width:440px;margin:0 auto;min-height:100vh;padding:28px 20px 40px}
        header{text-align:center}.avatar{width:120px;height:120px;border-radius:50%;object-fit:cover;border:4px solid #facc15}
        h1{margin:12px 0 2px;font-size:1.9rem;color:#fff}.spec{margin:0;color:#facc15;font-weight:700;text-transform:uppercase;letter-spacing:.08em;font-size:.85rem}.slogan{margin:8px 0 0;color:#9ca3af}
        .cta{display:block;margin:22px 0 12px;padding:16px;border-radius:14px;background:#facc15;color:#16181d;text-align:center;text-decoration:none;font-weight:800;font-size:1.1rem}
        .row{display:grid;grid-template-columns:1fr 1fr;gap:10px}.btn{padding:12px;border-radius:12px;background:#262a33;color:#fff;text-align:center;text-decoration:none;font-weight:600;font-size:.9rem}
        section{margin:24px 0}h2{margin:0 0 10px;color:#facc15;font-size:.95rem;letter-spacing:.1em;text-transform:uppercase}
        section p{margin:0 0 6px;line-height:1.5;color:#cbd5e1}section a{color:#facc15}
        ul{margin:0;padding:0;list-style:none;display:grid;gap:8px}li{padding:10px 12px;background:#262a33;border-left:4px solid #facc15;border-radius:8px}
        """;
}
