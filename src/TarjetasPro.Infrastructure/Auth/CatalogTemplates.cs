using Microsoft.EntityFrameworkCore;
using TarjetasPro.Application.Templates;
using TarjetasPro.Domain.Entities;

namespace TarjetasPro.Infrastructure.Auth;

/// <summary>Catálogo de plantillas del producto Catálogo en línea. Los productos se agregan como ítems (campo «catalogo»).</summary>
public static class CatalogTemplates
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (!await db.TemplateCategories.AnyAsync(c => c.Name == "Catálogos"))
            db.TemplateCategories.Add(new TemplateCategory { Name = "Catálogos", SortOrder = await db.TemplateCategories.MaxAsync(c => c.SortOrder) + 1 });

        var codes = All().Select(x => x.Code).ToList();
        var rows = await db.Templates.Where(t => t.Product == Products.Catalog || codes.Contains(t.Code)).ToListAsync();
        foreach (var (name, code, desc, css) in All())
        {
            var fields = string.Join(',', TemplateRenderer.ExtractFields(Html));
            var row = rows.FirstOrDefault(t => t.Code == code);
            if (row is null)
            {
                db.Templates.Add(new Template
                {
                    Name = name, Code = code, Description = desc, Product = Products.Catalog, Category = "Catálogos",
                    Profession = "Comercio", Html = Html, Css = css, FieldKeys = fields,
                });
                continue;
            }
            row.Product = Products.Catalog; row.Name = name; row.Description = desc; row.Html = Html; row.Css = css; row.FieldKeys = fields; row.UpdatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync();
    }

    private static IEnumerable<(string Name, string Code, string Desc, string Css)> All()
    {
        yield return ("Catálogo natural", "catalogo-natural", "Fondo crema y tarjetas de producto en dos columnas. Ideal para velas, repostería y artesanías.", Base("#f6f1e9", "#fffdf9", "#3c4a3e", "#5f7a61", "#1e9e54"));
        yield return ("Catálogo oscuro", "catalogo-oscuro", "Fondo verde profundo con tarjetas claras. Se ve elegante y moderno.", Base("#12342b", "#f3ede2", "#f3ede2", "#a9b99a", "#1e9e54", dark: true));
        yield return ("Catálogo rosa", "catalogo-rosa", "Tonos rosa suave y tarjetas blancas. Perfecto para accesorios, pastelería y belleza.", Base("#fbe9ee", "#ffffff", "#5a2a3a", "#c2185b", "#1e9e54"));
        yield return ("Catálogo azul noche", "catalogo-azul", "Azul marino con tarjetas claras. Serio y confiable para servicios y tecnología.", Base("#14213d", "#f4f6fb", "#f4f6fb", "#9db4e0", "#1e9e54", dark: true));
        yield return ("Catálogo terracota", "catalogo-terracota", "Tierra cálida y crema. Va con comida, cerámica y productos artesanales.", Base("#f3e3d6", "#fffaf5", "#4a2c1d", "#b5532a", "#1e9e54"));
        yield return ("Catálogo minimal", "catalogo-minimal", "Blanco limpio y tarjetas sutiles. Deja que tus fotos sean las protagonistas.", Base("#ffffff", "#f6f6f6", "#1a1a1a", "#555555", "#1e9e54"));
        yield return ("Catálogo negro y dorado", "catalogo-dorado", "Fondo negro con detalles dorados. Para joyería, barbería y productos premium.", Base("#121212", "#f5efe0", "#f5efe0", "#d4af37", "#1e9e54", dark: true));
        yield return ("Catálogo menta", "catalogo-menta", "Verde menta fresco y ligero. Ideal para plantas, salud y productos naturales.", Base("#e4f4ec", "#ffffff", "#1f4a39", "#2e8b6a", "#1e9e54"));
        yield return ("Repostería", "catalogo-reposteria", "Rosa pastel, letra manuscrita y fotos redondas con precio en etiqueta.", Base("#fff4f1", "#ffffff", "#5b2a3a", "#e0577f", "#1e9e54") + Repo);
        yield return ("Ferretería", "catalogo-ferreteria", "Negro y amarillo, bloques oscuros con precio destacado.", Base("#efefeb", "#1c1c1c", "#111111", "#fcc419", "#1e9e54") + Ferre);
        yield return ("Vitrina", "catalogo-vitrina", "Foto completa con el nombre y el precio encima. Ideal para ropa y calzado.", Base("#0f0f10", "#000000", "#f5f5f4", "#ff6b35", "#1e9e54", dark: true) + Vitrina);
        yield return ("Pop", "catalogo-pop", "Amarillo, sombras duras y etiquetas de color. Alegre y llamativo.", Base("#ffe14d", "#ffffff", "#111111", "#ff4fa3", "#1e9e54") + Pop);
        yield return ("Editorial", "catalogo-editorial", "Sin cajas, letra serif y filetes finos, como revista.", Base("#faf7f2", "#faf7f2", "#1b1b1b", "#8a6d3b", "#1e9e54") + Editorial);
        yield return ("Lista de productos", "catalogo-lista", "Una fila por producto con foto pequeña. Para menús largos y refacciones.", Base("#ffffff", "#f7f7f5", "#1f2937", "#0ea5a4", "#1e9e54") + Lista);
        yield return ("Florería", "catalogo-floreria", "Verde salvia y durazno con fotos redondas.", Base("#fdf1ea", "#ffffff", "#2f4a36", "#e07a5f", "#1e9e54") + Floreria);
        yield return ("Joyería", "catalogo-joyeria", "Negro con filetes dorados y letra elegante.", Base("#141414", "#1d1d1d", "#f3ece0", "#c9a24b", "#1e9e54", dark: true) + Joyeria);
        yield return ("Cafetería", "catalogo-cafeteria", "Café y crema con encabezado oscuro y letra serif cálida.", Base("#f5e9da", "#fffaf2", "#4b2e1e", "#a8643a", "#1e9e54") + Cafe);
        yield return ("Ropa", "catalogo-ropa", "Título gigante, sin cajas y fotos verticales. Moderno.", Base("#f4f4f0", "#f4f4f0", "#111111", "#111111", "#1e9e54") + Ropa);
        yield return ("Tecnología", "catalogo-tecnologia", "Azul oscuro con detalles neón y tarjetas sutiles.", Base("#0b1020", "#141c34", "#e6edf7", "#22d3ee", "#1e9e54", dark: true) + Tecno);
        yield return ("Garnachas", "catalogo-garnachas", "Papel picado, naranja y rojo. Para antojitos y comida mexicana.", Base("#fff3df", "#ffffff", "#3a1a08", "#e11d48", "#1e9e54") + Garnachas);
    }

    private const string Repo = """
        header{background:#f9c6d3;margin:-32px -16px 22px;padding:30px 16px 26px;border-radius:0 0 36px 36px}
        h1{font-family:'Pacifico',cursive;font-size:2.2rem;color:#b4375f;font-weight:400}.tag{color:#7a2e45}
        .prod{border-radius:26px;text-align:center}.prod img{width:78%;margin:12px auto 0;border-radius:50%}
        .pb{align-items:center}.pr{background:#e0577f;color:#fff;padding:3px 14px;border-radius:999px;font-size:.9rem;margin-top:auto}
        .qty{justify-content:center}.pb h3{color:#4a2431}
        """;
    private const string Ferre = """
        header{background:#111;margin:-32px -16px 22px;padding:28px 16px 22px;text-align:left;border-bottom:10px solid #fcc419}
        .logo{border-radius:6px;float:left;margin:0 14px 0 0;width:68px;height:68px}
        h1{color:#fcc419;font-size:2.8rem}.tag{color:#fff}.desc{color:#ddd;clear:both}
        .prod{background:#1c1c1c;color:#fff;border-radius:6px}.pb p{color:#b5b5b5}
        .pr{align-self:flex-end;background:#fcc419;color:#111;padding:3px 10px;border-radius:4px}
        .catt{color:#111;border-color:#111}.btn{background:#111;color:#fcc419}
        """;
    private const string Vitrina = """
        .prod{position:relative;background:#000;border-radius:10px;min-height:150px}
        .prod img{aspect-ratio:3/4}
        .pb{position:absolute;left:0;right:0;bottom:0;background:linear-gradient(transparent,rgba(0,0,0,.88) 45%);padding:34px 10px 10px;color:#fff}
        .pb p{display:none}.pr{color:#fde68a}.catt{color:#ff6b35}
        """;
    private const string Pop = """
        h1{font-size:3.4rem;text-shadow:3px 3px 0 #ff4fa3}
        .prod{border:3px solid #111;box-shadow:5px 5px 0 #111;border-radius:16px}
        .pr{background:#ff4fa3;color:#fff;border:2px solid #111;padding:2px 10px;border-radius:999px;align-self:flex-start}
        .btn{background:#111;color:#ffe14d}.catt{color:#111;border-color:#111}
        """;
    private const string Editorial = """
        header{text-align:left;border-bottom:3px double #1b1b1b;padding-bottom:16px}
        h1{font-family:'Playfair Display',serif;font-size:2.6rem;font-weight:700}.logo{border-radius:0;width:66px;height:66px}
        .prod{background:transparent;box-shadow:none;border-radius:0;color:#1b1b1b}.prod img{aspect-ratio:3/4;border-radius:2px}
        .pb{padding:8px 0}.pb h3{font-family:'Playfair Display',serif;font-size:.95rem}.pb p{color:#7d7468}.pr{color:#8a6d3b}
        .catt{font-family:'Playfair Display',serif;border-color:#1b1b1b;color:#1b1b1b}.btn{background:#1b1b1b;color:#faf7f2}
        """;
    private const string Lista = """
        .grid,.grid.c2,.grid.c3{grid-template-columns:1fr}
        .prod{flex-direction:row;align-items:center;border:1px solid #e5e7eb;box-shadow:none}
        .grid .prod img,.grid.c1 .prod img{width:96px;height:96px;aspect-ratio:1/1;margin:8px;border-radius:10px;flex:none}
        .grid.c3 .pb p{display:block}.grid.c3 .pb h3{font-size:.88rem}
        .pr{color:#0ea5a4}
        """;
    private const string Floreria = """
        header{background:#cfe0cf;margin:-32px -16px 22px;padding:30px 16px 26px;border-radius:0 0 36px 36px}
        h1{font-family:'Playfair Display',serif;font-weight:700;color:#2f4a36;font-size:2.4rem}.tag{color:#4d6b54}
        .prod{border-radius:26px;text-align:center}.prod img{width:78%;margin:12px auto 0;border-radius:50%}
        .pb{align-items:center}.pr{background:#e07a5f;color:#fff;padding:3px 14px;border-radius:999px;font-size:.9rem;margin-top:auto}.qty{justify-content:center}
        """;
    private const string Joyeria = """
        header{border-bottom:1px solid #c9a24b;padding-bottom:18px}
        h1{font-family:'Playfair Display',serif;font-weight:700;color:#c9a24b;font-size:2.4rem}.logo{border:2px solid #c9a24b}
        .prod{background:#1d1d1d;color:#f3ece0;border:1px solid #c9a24b55;border-radius:4px;box-shadow:none}
        .pb h3{font-family:'Playfair Display',serif}.pb p{color:#a89c85}.pr{color:#c9a24b}
        .catt{font-family:'Playfair Display',serif}.btn{background:#c9a24b;color:#141414}
        """;
    private const string Cafe = """
        header{background:#4b2e1e;color:#f5e9da;margin:-32px -16px 22px;padding:30px 16px 26px;border-radius:0 0 28px 28px}
        h1{font-family:'Playfair Display',serif;font-weight:700;color:#f5e9da;font-size:2.4rem}.tag{color:#e0b98f}
        .prod{border-radius:14px}.pb h3{font-family:'Playfair Display',serif;font-size:.95rem}.pr{color:#a8643a}
        .catt{font-family:'Playfair Display',serif}.btn{background:#4b2e1e;color:#f5e9da}
        """;
    private const string Ropa = """
        header{text-align:left}
        h1{font-size:4.6rem;line-height:.9;color:#111;border-bottom:6px solid #111;padding-bottom:10px;margin-bottom:8px}.tag{color:#6b6b66;font-size:.8rem;letter-spacing:.12em}
        .logo{display:none}
        .grid{grid-template-columns:1fr 1fr 1fr;gap:10px}.grid.c1{grid-template-columns:1fr}.grid.c2{grid-template-columns:1fr 1fr}
        .prod{background:transparent;box-shadow:none;border-radius:0;color:#111}.prod img{aspect-ratio:3/4;border-radius:0}
        .pb{padding:6px 0}.pb h3{font-size:.75rem;font-weight:600}.pb p{display:none}.pr{font-size:.85rem;color:#111}
        .catt{color:#111;border-color:#111}.btn{background:#111;color:#f4f4f0}
        """;
    private const string Tecno = """
        header{border-bottom:3px solid #22d3ee;padding-bottom:18px}
        h1{font-family:'Montserrat',sans-serif;font-weight:800;font-size:2rem;color:#fff}.logo{border-radius:14px}
        .prod{background:#141c34;border:1px solid #1c2540;border-radius:12px;box-shadow:none;color:#e6edf7}
        .pb p{color:#8b97b1}.pr{color:#22d3ee;font-weight:800}.catt{font-family:'Montserrat',sans-serif;font-weight:800;font-size:1.1rem}
        .btn{background:#22d3ee;color:#0b1020}
        """;
    private const string Garnachas = """
        header{background:#f97316 repeating-linear-gradient(90deg,#e11d48 0 33px,#facc15 33px 66px,#16a34a 66px 99px,#2563eb 99px 132px) no-repeat top/100% 22px;margin:-32px -16px 22px;padding:44px 16px 22px;border-bottom:8px solid #e11d48;color:#fff}
        h1{color:#fff;text-shadow:3px 3px 0 #e11d48;font-size:3rem}.tag{color:#3a1a08;font-weight:700}
        .prod{border:3px solid #3a1a08;border-radius:16px;box-shadow:4px 4px 0 #3a1a08}
        .pr{background:#e11d48;color:#fff;padding:2px 10px;border-radius:999px;align-self:flex-start}
        .catt{color:#e11d48}.btn{background:#e11d48;color:#fff}
        """;

    private const string Html = """
        <div class="cat">
          <header>
            {{#foto}}<img class="logo" src="{{foto}}" alt="{{nombre}}">{{/foto}}
            <h1>{{nombre}}</h1>
            {{#eslogan}}<p class="tag">{{eslogan}}</p>{{/eslogan}}
            {{#descripcion}}<p class="desc">{{descripcion}}</p>{{/descripcion}}
          </header>
          {{#has_catalogo}}
          <section><div class="grid c{{cols}} p-{{forma}}">{{#catalogo}}{{.}}{{/catalogo}}</div></section>
          {{/has_catalogo}}
          {{#whatsapp}}<div id="cart" data-wa="{{whatsapp}}" data-msg="{{pedido_msg}}" hidden>
            <div class="cbar"><button type="button" class="copen">Ver mi pedido <b class="cn">0</b><span class="ct"></span></button></div>
            <div class="cpanel" hidden><div class="chead"><b>Mi pedido</b><button type="button" class="cclose" aria-label="Cerrar">✕</button></div><ul class="cl"></ul><p class="ctot"></p><a class="csend" href="#">Enviar pedido por WhatsApp</a></div>
          </div>{{/whatsapp}}
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

    private static string Base(string bg, string card, string ink, string accent, string wa, bool dark = false) => $$"""
        @import url('https://fonts.googleapis.com/css2?family=Poppins:wght@400;600;700&family=Bebas+Neue&family=Pacifico&family=Playfair+Display:wght@400;700&family=Montserrat:wght@400;600;800&display=swap');
        *{box-sizing:border-box}
        body{margin:0;background:{{bg}};color:{{ink}};font-family:'Poppins',system-ui,sans-serif}
        .cat{max-width:520px;margin:0 auto;padding:32px 16px 110px}
        header{text-align:center;margin-bottom:22px}
        .logo{width:88px;height:88px;object-fit:cover;border-radius:50%;margin-bottom:10px}
        h1{margin:0;font-family:'Bebas Neue',Impact,sans-serif;font-size:2.6rem;letter-spacing:.02em;font-weight:400}
        .tag{margin:4px 0 0;color:{{accent}}}
        .desc{margin:10px 0 0;font-size:.9rem;line-height:1.5;opacity:.85}
        .grid{display:grid;grid-template-columns:1fr 1fr;gap:12px}
        .catt{grid-column:1/-1;margin:14px 0 2px;font-family:'Bebas Neue',Impact,sans-serif;font-weight:400;font-size:1.5rem;letter-spacing:.04em;color:{{accent}};border-bottom:2px solid {{accent}}55;padding-bottom:4px}
        .catt:first-child{margin-top:0}
        .grid.c1{grid-template-columns:1fr}
        .grid.c3{grid-template-columns:1fr 1fr 1fr;gap:8px}
        .grid.c3 .pb h3{font-size:.72rem}.grid.c3 .pb p{display:none}.grid.c3 .pr{font-size:.85rem}
        .p-vitrina .prod{position:relative;min-height:150px;background:#000;color:#fff;border-radius:10px}.p-vitrina .prod img{aspect-ratio:3/4}
        .p-vitrina .pb{position:absolute;left:0;right:0;bottom:0;background:linear-gradient(transparent,rgba(0,0,0,.88) 45%);padding:34px 10px 10px;color:#fff;width:auto}
        .p-vitrina .pb p{display:none}.p-vitrina .pr{color:#fde68a}.p-vitrina .qty button{background:#fff;color:#111}
        .p-lista .prod{flex-direction:row;align-items:center;border-radius:12px}.p-lista .prod img{width:92px;height:92px;aspect-ratio:1/1;margin:8px;border-radius:10px;flex:none}
        .p-lista.grid{grid-template-columns:1fr}.p-lista .pb p{display:block}.p-lista .pb h3{font-size:.92rem}
        .p-redonda .prod{text-align:center;border-radius:24px}.p-redonda .prod img{width:78%;margin:12px auto 0;border-radius:50%}.p-redonda .pb{align-items:center}
        .p-redonda .pr{background:{{accent}};color:#fff;padding:3px 14px;border-radius:999px;margin-top:auto}.p-redonda .qty{justify-content:center}
        .p-marco .prod{border:2px solid {{ink}};border-radius:8px;box-shadow:none}.p-marco .prod img{border-radius:0}
        .p-marco .pr{margin:6px -12px -12px;padding:7px;background:{{accent}};color:#fff;text-align:center}.p-marco .qty{margin:8px 0 12px}
        .p-revista .prod{background:transparent;box-shadow:none;border-radius:0;color:inherit}.p-revista .prod img{aspect-ratio:3/4;border-radius:2px}
        .p-revista .pb{padding:8px 0}.p-revista .pb p{color:inherit;opacity:.7}.p-revista .pr{color:{{accent}}}
        .p-pop .prod{border:3px solid #111;border-radius:16px;box-shadow:5px 5px 0 #111}.p-pop .pr{align-self:flex-start;background:{{accent}};color:#fff;border:2px solid #111;padding:2px 10px;border-radius:999px}
        .p-color .prod{background:{{accent}};color:#fff;border-radius:22px;text-align:center}.p-color .prod img{width:74%;margin:12px auto 0;border-radius:50%}
        .p-color .pb{align-items:center}.p-color .pb p{color:#ffffffcc}.p-color .pr{background:#fff;color:#111;padding:3px 14px;border-radius:999px;margin-top:auto}.p-color .qty{justify-content:center}
        .grid.c1 .prod{flex-direction:row}.grid.c1 .prod img{width:42%;aspect-ratio:1/1}
        .prod{background:{{card}};color:#3c3228;border-radius:14px;overflow:hidden;display:flex;flex-direction:column;box-shadow:0 2px 10px rgba(0,0,0,.12)}
        .prod img{width:100%;aspect-ratio:1/1;object-fit:cover;display:block}
        .pb{padding:10px 12px 12px;display:flex;flex-direction:column;gap:4px;flex:1}
        .pb h3{margin:0;font-size:.88rem;line-height:1.25}
        .pb p{margin:0;font-size:.74rem;line-height:1.35;color:#7a6f63}
        .pr{margin-top:auto;padding-top:6px;font-size:1rem;color:#1d3b2f}
        footer{margin-top:28px;text-align:center}
        .info{margin:0 0 12px;font-size:.88rem;line-height:1.5;opacity:.9}
        .info a{color:{{accent}};font-weight:700}
        .actions{display:flex;flex-wrap:wrap;justify-content:center;gap:10px;margin-top:8px}
        .btn{display:inline-block;padding:12px 20px;border-radius:999px;background:{{(dark ? "#f3ede2" : "#3c4a3e")}};color:{{(dark ? "#12342b" : "#fff")}};font-weight:700;text-decoration:none;font-size:.92rem}
        .btn.wa{background:{{wa}};color:#fff}
        """;
}
