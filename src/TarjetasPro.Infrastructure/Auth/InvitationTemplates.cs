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
        foreach (var (name, code, desc, occasion, html, css) in All())
        {
            var fields = string.Join(',', TemplateRenderer.ExtractFields(html));
            var row = rows.FirstOrDefault(t => t.Code == code);
            if (row is null)
            {
                db.Templates.Add(new Template
                {
                    Name = name, Code = code, Description = desc, Product = Products.Invitations, Category = "Invitaciones",
                    Profession = occasion, Html = html, Css = css, FieldKeys = fields,
                });
                continue;
            }
            row.Name = name; row.Description = desc; row.Product = Products.Invitations; row.Category = "Invitaciones"; row.Profession = occasion;
            row.Html = html; row.Css = css; row.FieldKeys = fields; row.UpdatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync();
    }

    private static IEnumerable<(string Name, string Code, string Desc, string Occasion, string Html, string Css)> All()
    {
        yield return ("XV rosa y dorado", "xv-rosa-dorado", "Rosa pálido con dorado, letra cursiva y foto en círculo con marco dorado.", "XV años", XvHtml, XvFonts1 + Base + XvRosa);
        yield return ("XV princesa", "xv-princesa", "Morado con destellos, foto en arco dorado y letra de cuento.", "XV años", XvHtml, XvFonts2 + Base + XvPrincesa);
        yield return ("XV noche de gala", "xv-noche", "Azul noche con dorado, foto a todo lo ancho y letra clásica.", "XV años", XvHtml, XvFonts3 + Base + XvNoche);
        yield return ("Unicornio y arcoíris", "cumple-nina-unicornio", "Nubes, colores pastel y globo de edad con arcoíris.", "Cumpleaños niña", NinaHtml, NinaFonts1 + Base + Kid + NinaUnicornio);
        yield return ("Princesa", "cumple-nina-princesa", "Franja rosa con festón, dorado y letra divertida.", "Cumpleaños niña", NinaHtml, NinaFonts2 + Base + Kid + NinaPrincesa);
        yield return ("Jardín de flores", "cumple-nina-flores", "Flores arriba y abajo, verde fresco y coral.", "Cumpleaños niña", NinaHtml, NinaFonts3 + Base + Kid + NinaFlores);
        yield return ("Boda elegante", "boda-elegante", "Crema y dorado, tipografía clásica, foto con degradado y saludo para cada invitado.", "Boda", BodaHtml, BodaCss);
    }


    // ───────── XV años ─────────
    private const string XvHtml = """
        <div class="page inv xv">
          <header>
            {{#foto}}<div class="ph"><img src="{{foto}}" alt="{{nombre}}"></div>{{/foto}}
            {{#invitado}}<p class="hola">Para <b>{{invitado}}</b></p>{{/invitado}}
            <p class="kicker">{{#evento}}{{evento}}{{/evento}}{{^evento}}Mis XV años{{/evento}}</p>
            <h1>{{nombre}}</h1>
            {{#eslogan}}<p class="lema">{{eslogan}}</p>{{/eslogan}}
          </header>
          {{#mensaje}}<p class="msg">{{mensaje}}</p>{{/mensaje}}
          {{#padres}}<section class="fam"><small>Con la bendición de mis padres</small><b>{{padres}}</b></section>{{/padres}}
          {{#has_padrinos}}<section class="fam"><small>Mis padrinos</small><ul>{{#padrinos}}<li>{{.}}</li>{{/padrinos}}</ul></section>{{/has_padrinos}}
          {{#fecha}}<section class="date"><b>{{fecha}}</b>{{#hora}}<span>{{hora}}</span>{{/hora}}</section>{{/fecha}}
          {{#ubicacion}}<section class="card"><small>Lugar</small><b>{{ubicacion}}</b>{{#mapa}}<a class="btn" href="{{mapa}}">Cómo llegar</a>{{/mapa}}</section>{{/ubicacion}}
          {{#has_programa}}<section class="card prog"><small>Programa</small><ol>{{#programa}}<li>{{.}}</li>{{/programa}}</ol></section>{{/has_programa}}
          {{#vestimenta}}<section class="card"><small>Código de vestimenta</small><b>{{vestimenta}}</b></section>{{/vestimenta}}
          {{#regalos}}<section class="card"><small>Mesa de regalos</small><b>{{regalos}}</b></section>{{/regalos}}
          <p class="end">Confirma tu asistencia con el botón de abajo ♥</p>
        </div>
        """;

    // ───────── Cumpleaños niña ─────────
    private const string NinaHtml = """
        <div class="page inv kid">
          <header>
            {{#foto}}<div class="ph"><img src="{{foto}}" alt="{{nombre}}"></div>{{/foto}}
            {{#invitado}}<p class="hola">Para <b>{{invitado}}</b></p>{{/invitado}}
            <p class="kicker">{{#evento}}{{evento}}{{/evento}}{{^evento}}¡Estás invitado a mi fiesta!{{/evento}}</p>
            <h1>{{nombre}}</h1>
            {{#edad}}<div class="age"><b>{{edad}}</b><small>años</small></div>{{/edad}}
            {{#tema}}<p class="tema">{{tema}}</p>{{/tema}}
          </header>
          {{#mensaje}}<p class="msg">{{mensaje}}</p>{{/mensaje}}
          {{#fecha}}<section class="date"><b>{{fecha}}</b>{{#hora}}<span>{{hora}}</span>{{/hora}}</section>{{/fecha}}
          {{#ubicacion}}<section class="card"><small>Dónde</small><b>{{ubicacion}}</b>{{#mapa}}<a class="btn" href="{{mapa}}">Cómo llegar</a>{{/mapa}}</section>{{/ubicacion}}
          {{#has_programa}}<section class="card prog"><small>Qué haremos</small><ol>{{#programa}}<li>{{.}}</li>{{/programa}}</ol></section>{{/has_programa}}
          {{#padres}}<section class="card"><small>Te invitan</small><b>{{padres}}</b></section>{{/padres}}
          {{#vestimenta}}<section class="card"><small>Cómo vestir</small><b>{{vestimenta}}</b></section>{{/vestimenta}}
          {{#regalos}}<section class="card"><small>Regalos</small><b>{{regalos}}</b></section>{{/regalos}}
          <p class="end">¡Confirma tu asistencia con el botón de abajo!</p>
        </div>
        """;

    /// <summary>Estructura común: cada diseño solo cambia las variables de color y tipografía y sus adornos.</summary>
    private const string Base = """
        *{box-sizing:border-box}
        body{margin:0;background:var(--bg);color:var(--ink);font-family:var(--fb)}
        .inv{max-width:430px;margin:0 auto;padding:0 0 110px;text-align:center;position:relative;overflow:hidden;background:var(--page)}
        .inv>*{position:relative}
        header{padding:40px 22px 8px}
        .ph{width:220px;height:220px;margin:0 auto 14px}
        .ph img{width:100%;height:100%;object-fit:cover;display:block}
        .hola{margin:0 0 10px;font:500 .78rem var(--fb);letter-spacing:.18em;text-transform:uppercase;color:var(--soft)}
        .hola b{color:var(--acc);font-weight:600}
        .kicker{margin:0;font:600 .8rem var(--fb);letter-spacing:.28em;text-transform:uppercase;color:var(--acc2)}
        h1{margin:8px 0 4px;font:400 var(--ts)/1.05 var(--ft);color:var(--acc);overflow-wrap:anywhere}
        .lema{margin:6px 30px 0;font-style:italic;color:var(--soft);font-size:1.05rem}
        .msg{margin:22px 30px 0;font-size:1.15rem;line-height:1.55;font-style:italic;color:var(--ink)}
        .fam{margin:20px 28px 0}
        .fam small,.card small{display:block;margin-bottom:6px;font:600 .7rem var(--fb);letter-spacing:.22em;text-transform:uppercase;color:var(--acc2)}
        .fam b{display:block;font:600 1.25rem/1.3 var(--ft2);color:var(--ink)}
        .fam ul{margin:0;padding:0;list-style:none;display:grid;gap:4px}
        .fam li{font:500 1.1rem var(--ft2);color:var(--ink)}
        .date{margin:24px 22px 0;padding:18px 10px;border-top:1px solid var(--line);border-bottom:1px solid var(--line)}
        .date b{display:block;font:700 1.35rem var(--ft2);letter-spacing:.05em;text-transform:uppercase;color:var(--acc)}
        .date span{display:block;margin-top:4px;font:500 .95rem var(--fb);letter-spacing:.2em;color:var(--soft)}
        .card{margin:14px 22px 0;padding:18px 16px;background:var(--card);border-radius:var(--r);box-shadow:0 8px 24px rgba(0,0,0,.08)}
        .card b{display:block;font:600 1.2rem/1.35 var(--ft2);color:var(--ink)}
        .btn{display:inline-block;margin-top:12px;padding:10px 22px;border-radius:999px;background:var(--acc);color:var(--btn);text-decoration:none;font:600 .85rem var(--fb);letter-spacing:.08em}
        .prog ol{margin:6px 0 0;padding:0;list-style:none;display:grid;gap:6px}
        .prog li{padding:6px 0;border-bottom:1px dashed var(--line);font:500 1.1rem var(--ft2);color:var(--ink)}
        .prog li:last-child{border:0}
        .end{margin:28px 24px 0;font:600 .78rem var(--fb);letter-spacing:.14em;text-transform:uppercase;color:var(--acc2)}
        """;

    private const string Kid = """
        .age{width:100px;height:100px;margin:14px auto 0;border-radius:50%;display:flex;flex-direction:column;align-items:center;justify-content:center;background:#fff;color:var(--acc);line-height:1}
        .age b{font:700 3rem var(--ft2)}
        .age small{margin-top:2px;font:600 .7rem var(--fb);letter-spacing:.14em;text-transform:uppercase;color:var(--soft)}
        .tema{margin:14px 24px 0;font:600 1.05rem var(--ft2);color:var(--acc2)}
        .tema::before{content:'★ '}.tema::after{content:' ★'}
        .msg{font-style:normal;font-family:var(--ft2);font-weight:500}
        .date b{font-size:1.3rem}
        """;

    // ───────── Diseños XV ─────────
    private const string XvFonts1 = "@import url('https://fonts.googleapis.com/css2?family=Cormorant+Garamond:ital,wght@0,500;0,700;1,500&family=Great+Vibes&family=Jost:wght@400;500;600&display=swap');\n";
    private const string XvFonts2 = "@import url('https://fonts.googleapis.com/css2?family=Parisienne&family=Playfair+Display:ital,wght@0,500;0,700;1,500&family=Montserrat:wght@400;500;600&display=swap');\n";
    private const string XvFonts3 = "@import url('https://fonts.googleapis.com/css2?family=Cinzel:wght@500;700&family=Cormorant+Garamond:ital,wght@0,500;0,700;1,500&family=Jost:wght@400;500;600&display=swap');\n";

    private const string XvRosa = """
        :root{--bg:#fbe4ea;--page:linear-gradient(#fff4f6,#fbe1e8 60%,#f8d2dc);--ink:#5b2a3a;--soft:#9a6b7b;--acc:#b8893a;--acc2:#d4607f;--line:#e9c9a0;--card:#fff;--r:18px;--btn:#fff;--ft:'Great Vibes',cursive;--ft2:'Cormorant Garamond',serif;--fb:'Jost',sans-serif;--ts:4rem}
        .inv::before,.inv::after{content:'';position:absolute;width:220px;height:220px;border-radius:50%;background:radial-gradient(circle,rgba(247,170,190,.75) 0,rgba(247,170,190,0) 70%);pointer-events:none}
        .inv::before{top:-80px;left:-80px}.inv::after{top:260px;right:-110px}
        header::before{content:'✦  ✦  ✦';display:block;margin-bottom:14px;color:#d4a95a;letter-spacing:.4em;font-size:.8rem}
        .ph{border-radius:50%;padding:7px;background:linear-gradient(135deg,#f3dca0,#b8893a,#f3dca0)}
        .ph img{border-radius:50%;border:5px solid #fff}
        .msg{font-family:var(--ft2);font-size:1.25rem}
        """;

    private const string XvPrincesa = """
        :root{--bg:#e6d3fb;--page:linear-gradient(#f6ecff,#e6d3fb 55%,#d8bff7);--ink:#3b1d6b;--soft:#7a5fa8;--acc:#7c3aed;--acc2:#b07d0c;--line:#cdb4f0;--card:rgba(255,255,255,.88);--r:22px;--btn:#fff;--ft:'Parisienne',cursive;--ft2:'Playfair Display',serif;--fb:'Montserrat',sans-serif;--ts:4.2rem}
        .inv::before{content:'';position:absolute;inset:0;background-image:radial-gradient(circle,#fff 1.6px,transparent 2.2px),radial-gradient(circle,#d4a72c 1.6px,transparent 2.2px);background-size:90px 110px,140px 130px;background-position:12px 20px,64px 74px;opacity:.65;pointer-events:none}
        .ph{width:230px;height:290px;border-radius:115px 115px 22px 22px;padding:6px;background:linear-gradient(#f0d27a,#b07d0c)}
        .ph img{border-radius:110px 110px 17px 17px}
        .kicker{letter-spacing:.34em}
        .date b{font-family:var(--ft2)}
        .card{border:1px solid #e2d0fa}
        """;

    private const string XvNoche = """
        :root{--bg:#0b1020;--page:linear-gradient(#0b1020,#1a1240 55%,#0b1020);--ink:#f3e9d2;--soft:#b9ad92;--acc:#e7c873;--acc2:#e7c873;--line:rgba(231,200,115,.35);--card:rgba(255,255,255,.07);--r:16px;--btn:#0b1020;--ft:'Cinzel',serif;--ft2:'Cormorant Garamond',serif;--fb:'Jost',sans-serif;--ts:2.7rem}
        .inv::before{content:'';position:absolute;inset:0;background-image:radial-gradient(circle,#fff 1px,transparent 1.6px),radial-gradient(circle,#e7c873 1.4px,transparent 2px);background-size:70px 90px,120px 110px;background-position:10px 14px,50px 60px;opacity:.5;pointer-events:none}
        h1{font-weight:700;letter-spacing:.06em;text-transform:uppercase}
        header{padding-top:0}
        .kicker{padding-top:40px}
        .ph{width:calc(100% + 44px);height:360px;margin:0 -22px 18px;position:relative}
        .ph::after{content:'';position:absolute;inset:0;background:linear-gradient(transparent 45%,#0b1020)}
        header:has(.ph) .kicker{padding-top:0}
        .card{border:1px solid var(--line);box-shadow:none}
        .msg{font-family:var(--ft2);font-size:1.25rem;color:#f3e9d2}
        .btn{color:#0b1020}
        """;

    // ───────── Diseños cumpleaños niña ─────────
    private const string NinaFonts1 = "@import url('https://fonts.googleapis.com/css2?family=Pacifico&family=Fredoka:wght@400;500;600;700&display=swap');\n";
    private const string NinaFonts2 = "@import url('https://fonts.googleapis.com/css2?family=Pacifico&family=Quicksand:wght@500;600;700&display=swap');\n";
    private const string NinaFonts3 = "@import url('https://fonts.googleapis.com/css2?family=Baloo+2:wght@500;700&family=Quicksand:wght@500;600;700&display=swap');\n";

    private const string NinaUnicornio = """
        :root{--bg:#fde7f4;--page:linear-gradient(#e6e9ff,#fde2f3 38%,#ffeedd);--ink:#5a2d6e;--soft:#8c6a9d;--acc:#d946a8;--acc2:#7c6df0;--line:#f1c6e3;--card:#fff;--r:26px;--btn:#fff;--ft:'Pacifico',cursive;--ft2:'Fredoka',sans-serif;--fb:'Fredoka',sans-serif;--ts:3rem}
        .inv::before{content:'';position:absolute;top:0;left:0;right:0;height:240px;background:radial-gradient(circle at 14% 30%,#fff 0 36px,transparent 37px),radial-gradient(circle at 28% 24%,#fff 0 28px,transparent 29px),radial-gradient(circle at 86% 18%,#fff 0 40px,transparent 41px),radial-gradient(circle at 70% 30%,#fff 0 28px,transparent 29px);opacity:.9;pointer-events:none}
        .age{border:7px solid transparent;background:linear-gradient(#fff,#fff) padding-box,conic-gradient(#ff7eb6,#ffd36e,#8fe3a7,#7cc7ff,#b794f6,#ff7eb6) border-box;box-shadow:0 8px 22px rgba(217,70,168,.25)}
        .ph{border-radius:50%;width:200px;height:200px;border:7px solid transparent;background:linear-gradient(#fff,#fff) padding-box,conic-gradient(#ff7eb6,#ffd36e,#8fe3a7,#7cc7ff,#b794f6,#ff7eb6) border-box;overflow:hidden}
        .ph img{border-radius:50%}
        """;

    private const string NinaPrincesa = """
        :root{--bg:#fff3e3;--page:#fff8ee;--ink:#6b2d4a;--soft:#a1708a;--acc:#ff5a97;--acc2:#d99a1b;--line:#f7d6e2;--card:#fff;--r:24px;--btn:#fff;--ft:'Pacifico',cursive;--ft2:'Quicksand',sans-serif;--fb:'Quicksand',sans-serif;--ts:2.9rem}
        header{background:#ff8fb8;padding-bottom:34px;margin-bottom:18px;color:#fff}
        header::after{content:'';position:absolute;left:0;right:0;bottom:-27px;height:28px;background:radial-gradient(circle at 14px 0,#ff8fb8 13px,transparent 14px) 0 0/28px 28px repeat-x}
        header::before{content:'✦  ✦  ✦';display:block;margin-bottom:12px;color:#ffe27a;letter-spacing:.4em;font-size:.8rem}
        header{position:relative}
        .kicker{color:#fff7c2}
        h1{color:#fff;text-shadow:0 3px 0 rgba(180,50,100,.25)}
        .hola,.hola b{color:#fff}
        .ph{border-radius:50%;width:190px;height:190px;border:6px solid #fff;overflow:hidden;box-shadow:0 8px 20px rgba(180,50,100,.3)}
        .ph img{border-radius:50%}
        .age{border:6px solid #f5c542;box-shadow:0 8px 20px rgba(180,50,100,.25)}
        .tema{color:#fff}
        .date b{color:var(--acc)}
        """;

    private const string NinaFlores = """
        :root{--bg:#f1fbe9;--page:linear-gradient(#fffbe6,#f1fbe9 50%,#e2f5d3);--ink:#2f5d3a;--soft:#6a8f6f;--acc:#e5546b;--acc2:#3f9b5a;--line:#cfe8c4;--card:#fff;--r:22px;--btn:#fff;--ft:'Baloo 2',cursive;--ft2:'Quicksand',sans-serif;--fb:'Quicksand',sans-serif;--ts:3.1rem}
        .inv::before,.inv::after{content:'';position:absolute;left:0;right:0;height:64px;background:radial-gradient(circle at 50% 50%,#ffd23f 0 8px,transparent 9px),radial-gradient(circle at 50% 27%,#ff8fa3 0 10px,transparent 11px),radial-gradient(circle at 50% 73%,#ff8fa3 0 10px,transparent 11px),radial-gradient(circle at 27% 50%,#ff8fa3 0 10px,transparent 11px),radial-gradient(circle at 73% 50%,#ff8fa3 0 10px,transparent 11px);background-size:64px 64px;background-repeat:repeat-x;pointer-events:none}
        .inv::before{top:0}.inv::after{bottom:0}
        header{padding-top:84px}
        h1{font-weight:700}
        .age{background:#ffd23f;color:#7a4a00;box-shadow:0 0 0 6px #ff8fa3}
        .age small{color:#7a4a00}
        .ph{border-radius:50%;width:200px;height:200px;border:6px solid #fff;box-shadow:0 0 0 6px #ff8fa3;overflow:hidden}
        .ph img{border-radius:50%}
        .card{border:2px solid var(--line);box-shadow:none}
        """;

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
