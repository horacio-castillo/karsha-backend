using Microsoft.EntityFrameworkCore;
using TarjetasPro.Application.Templates;
using TarjetasPro.Domain.Entities;

namespace TarjetasPro.Infrastructure.Auth;

/// <summary>Catálogo de plantillas del producto Invitaciones. Se actualizan al arrancar si cambia su diseño.</summary>
public static class InvitationTemplates
{
    public static async Task SeedAsync(AppDbContext db)
    {
        var rows = await db.Templates.ToListAsync();
        foreach (var (name, code, desc, html, css) in All())
        {
            var fields = string.Join(',', TemplateRenderer.ExtractFields(html));
            var row = rows.FirstOrDefault(t => t.Code == code);
            if (row is null)
            {
                db.Templates.Add(new Template
                {
                    Name = name, Code = code, Description = desc, Product = Products.Invitations, Category = "Invitaciones",
                    Profession = "Boda", Html = html, Css = css, FieldKeys = fields,
                });
                continue;
            }
            row.Name = name; row.Description = desc; row.Product = Products.Invitations; row.Category = "Invitaciones";
            row.Html = html; row.Css = css; row.FieldKeys = fields; row.UpdatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync();
    }

    private static IEnumerable<(string Name, string Code, string Desc, string Html, string Css)> All()
    {
        yield return ("Boda elegante", "boda-elegante", "Crema y dorado, tipografía clásica, foto con degradado y saludo para cada invitado.", BodaHtml, BodaCss);
    }

    private const string BodaHtml = """
        <div class="page inv">
          <header>
            {{#foto}}<div class="hero"><img src="{{foto}}" alt="{{nombre}}"><i></i></div>{{/foto}}
            {{#invitado}}<p class="hola">Para <b>{{invitado}}</b></p>{{/invitado}}
            {{#evento}}<p class="kicker">{{evento}}</p>{{/evento}}
            <h1>{{nombre}}</h1>
            <div class="orn"><span></span><em>♥</em><span></span></div>
          </header>
          {{#fecha}}
          <section class="date">
            <b>{{fecha}}</b>
            {{#hora}}<span>{{hora}}</span>{{/hora}}
          </section>
          {{/fecha}}
          {{#mensaje}}<p class="msg">{{mensaje}}</p>{{/mensaje}}
          {{#ubicacion}}
          <section class="card">
            <small>Lugar</small>
            <b>{{ubicacion}}</b>
            {{#mapa}}<a class="btn" href="{{mapa}}">Cómo llegar</a>{{/mapa}}
          </section>
          {{/ubicacion}}
          {{#vestimenta}}
          <section class="card">
            <small>Código de vestimenta</small>
            <b>{{vestimenta}}</b>
          </section>
          {{/vestimenta}}
          {{#has_programa}}
          <section class="card prog">
            <small>Programa</small>
            <ol>{{#programa}}<li>{{.}}</li>{{/programa}}</ol>
          </section>
          {{/has_programa}}
          {{#eslogan}}<p class="quote">{{eslogan}}</p>{{/eslogan}}
          <p class="end">Confirma tu asistencia con el botón de abajo ♥</p>
        </div>
        """;

    private const string BodaCss = """
        @import url('https://fonts.googleapis.com/css2?family=Cormorant+Garamond:ital,wght@0,400;0,600;1,400&family=Great+Vibes&family=Jost:wght@400;500&display=swap');
        *{box-sizing:border-box}
        body{margin:0;background:#f7efe6;color:#4a3a2a;font-family:'Cormorant Garamond',Georgia,serif}
        .inv{max-width:430px;margin:0 auto;padding:0 0 110px;text-align:center;background:linear-gradient(#fbf6ef,#f4e9db)}
        .hero{position:relative;height:420px;overflow:hidden}
        .hero img{width:100%;height:100%;object-fit:cover;object-position:center 20%;display:block}
        .hero i{position:absolute;inset:0;background:linear-gradient(180deg,rgba(251,246,239,0) 45%,#fbf6ef 98%)}
        header{padding:0 22px}
        header:has(.hero){margin-top:0}
        .hola{margin:14px 0 0;font:500 .78rem 'Jost',system-ui,sans-serif;letter-spacing:.18em;text-transform:uppercase;color:#8a6a2f}
        .hola b{font-weight:500;color:#5a3d12}
        .kicker{margin:18px 0 0;font:500 .78rem 'Jost',system-ui,sans-serif;letter-spacing:.32em;text-transform:uppercase;color:#7b6a58}
        h1{margin:6px 0 2px;font:400 3.4rem/1.05 'Great Vibes','Cormorant Garamond',cursive;color:#6b4a14}
        .orn{display:flex;align-items:center;justify-content:center;gap:12px;margin:10px 0 4px;color:#a97c2e}
        .orn span{width:70px;height:1px;background:#c9a45c}.orn em{font-style:normal;font-size:.9rem}
        .date{margin:20px 22px 0;padding:18px 10px;border-top:1px solid #d9c29a;border-bottom:1px solid #d9c29a}
        .date b{display:block;font:600 1.45rem 'Cormorant Garamond',serif;letter-spacing:.06em;text-transform:uppercase;color:#5a3d12}
        .date span{display:block;margin-top:4px;font:500 .95rem 'Jost',sans-serif;letter-spacing:.2em;color:#7b6a58}
        .msg{margin:22px 30px;font-size:1.2rem;line-height:1.55;font-style:italic;color:#5c4a38}
        .card{margin:14px 22px;padding:18px 16px;background:#fff;border-radius:16px;box-shadow:0 8px 24px rgba(106,74,20,.1)}
        .card small{display:block;margin-bottom:6px;font:500 .7rem 'Jost',sans-serif;letter-spacing:.22em;text-transform:uppercase;color:#a97c2e}
        .card b{display:block;font:600 1.25rem/1.3 'Cormorant Garamond',serif;color:#4a3a2a}
        .btn{display:inline-block;margin-top:12px;padding:10px 22px;border-radius:999px;background:#6b4a14;color:#fff;text-decoration:none;font:500 .85rem 'Jost',sans-serif;letter-spacing:.08em}
        .prog ol{margin:6px 0 0;padding:0;list-style:none;display:grid;gap:8px}
        .prog li{font-size:1.15rem;color:#4a3a2a;padding:6px 0;border-bottom:1px dashed #e4d3b4}.prog li:last-child{border:0}
        .quote{margin:24px 36px 0;font-size:1.15rem;font-style:italic;color:#7b6a58}
        .end{margin:26px 24px 0;font:500 .8rem 'Jost',sans-serif;letter-spacing:.12em;text-transform:uppercase;color:#a97c2e}
        """;
}
