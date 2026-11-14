using Microsoft.EntityFrameworkCore;
using TarjetasPro.Application.Templates;
using TarjetasPro.Domain.Entities;

namespace TarjetasPro.Infrastructure.Auth;

/// <summary>Catálogo de plantillas del producto Menú digital. Se actualizan al arrancar si cambia su diseño.</summary>
public static class MenuTemplates
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (!await db.TemplateCategories.AnyAsync(c => c.Name == "Restaurantes y menús"))
            db.TemplateCategories.Add(new TemplateCategory { Name = "Restaurantes y menús", SortOrder = await db.TemplateCategories.MaxAsync(c => c.SortOrder) + 1 });

        var rows = await db.Templates.Where(t => t.Product == Products.Menu).ToListAsync();
        foreach (var (name, code, desc, html, css) in All())
        {
            var fields = string.Join(',', TemplateRenderer.ExtractFields(html));
            var row = rows.FirstOrDefault(t => t.Code == code);
            if (row is null)
            {
                db.Templates.Add(new Template
                {
                    Name = name, Code = code, Description = desc, Product = Products.Menu, Category = "Restaurantes y menús",
                    Profession = "Restaurante", Html = html, Css = css, FieldKeys = fields,
                });
                continue;
            }
            row.Name = name; row.Description = desc; row.Html = html; row.Css = css; row.FieldKeys = fields; row.UpdatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync();
    }

    private static IEnumerable<(string Name, string Code, string Desc, string Html, string Css)> All()
    {
        yield return ("Menú clásico", "menu-clasico", "Fondo crema, secciones claras y precios alineados. Pensado para leerse en el celular.", ClasicoHtml, ClasicoCss);
    }

    private const string ClasicoHtml = """
        <div class="menu">
          <header>
            {{#foto}}<img class="logo" src="{{foto}}" alt="{{nombre}}">{{/foto}}
            <h1>{{nombre}}</h1>
            {{#eslogan}}<p class="tag">{{eslogan}}</p>{{/eslogan}}
            {{#descripcion}}<p class="desc">{{descripcion}}</p>{{/descripcion}}
          </header>
          {{#has_menu_entradas}}
          <section><h2>Entradas</h2><ul>{{#menu_entradas}}<li>{{.}}</li>{{/menu_entradas}}</ul></section>
          {{/has_menu_entradas}}
          {{#has_menu_principales}}
          <section><h2>Platos fuertes</h2><ul>{{#menu_principales}}<li>{{.}}</li>{{/menu_principales}}</ul></section>
          {{/has_menu_principales}}
          {{#has_menu_bebidas}}
          <section><h2>Bebidas</h2><ul>{{#menu_bebidas}}<li>{{.}}</li>{{/menu_bebidas}}</ul></section>
          {{/has_menu_bebidas}}
          {{#has_menu_postres}}
          <section><h2>Postres</h2><ul>{{#menu_postres}}<li>{{.}}</li>{{/menu_postres}}</ul></section>
          {{/has_menu_postres}}
          <footer>
            {{#horarios}}<p class="info"><b>Horario</b><br>{{horarios}}</p>{{/horarios}}
            {{#ubicacion}}<p class="info"><b>Dónde estamos</b><br>{{ubicacion}}{{#mapa}} · <a href="{{mapa}}">Ver mapa</a>{{/mapa}}</p>{{/ubicacion}}
            <div class="actions">
              {{#whatsapp}}<a class="btn wa" href="https://wa.me/{{whatsapp}}">Pedir por WhatsApp</a>{{/whatsapp}}
              {{#telefono}}<a class="btn" href="tel:{{telefono}}">Llamar</a>{{/telefono}}
              {{#instagram}}<a class="btn" href="{{instagram}}">Instagram</a>{{/instagram}}
              {{#facebook}}<a class="btn" href="{{facebook}}">Facebook</a>{{/facebook}}
            </div>
          </footer>
        </div>
        """;

    private const string ClasicoCss = """
        @import url('https://fonts.googleapis.com/css2?family=Playfair+Display:wght@600;700&family=Lato:wght@400;700&display=swap');
        *{box-sizing:border-box}
        body{margin:0;background:#f6efe4;color:#3b2f25;font-family:'Lato',system-ui,sans-serif}
        .menu{max-width:460px;margin:0 auto;padding:36px 22px 110px;background:#fbf6ee}
        header{text-align:center;margin-bottom:26px}
        .logo{width:96px;height:96px;object-fit:cover;border-radius:50%;border:3px solid #c9a46a;margin-bottom:12px}
        h1{margin:0;font-family:'Playfair Display',Georgia,serif;font-size:2rem;letter-spacing:-.01em}
        .tag{margin:6px 0 0;color:#8a6d3f;font-style:italic}
        .desc{margin:12px 0 0;font-size:.95rem;line-height:1.5;color:#5b4a3a}
        section{margin:0 0 24px}
        h2{margin:0 0 10px;padding-bottom:6px;border-bottom:2px solid #c9a46a;font-family:'Playfair Display',Georgia,serif;font-size:1.2rem;color:#8a6d3f;text-transform:uppercase;letter-spacing:.08em}
        ul{list-style:none;margin:0;padding:0}
        li{padding:10px 0;border-bottom:1px dashed #e3d5bd;font-size:1rem;line-height:1.35}
        li:last-child{border-bottom:0}
        footer{margin-top:30px;text-align:center}
        .info{margin:0 0 14px;font-size:.92rem;line-height:1.5;color:#5b4a3a}
        .info a{color:#8a6d3f;font-weight:700}
        .actions{display:flex;flex-wrap:wrap;justify-content:center;gap:10px;margin-top:8px}
        .btn{display:inline-block;padding:12px 20px;border-radius:999px;background:#3b2f25;color:#fff;font-weight:700;text-decoration:none;font-size:.95rem}
        .btn.wa{background:#1e9e54}
        """;
}
