namespace TarjetasPro.Application.Templates;

/// <summary>Buscador, botones por categoría y ficha del producto del catálogo.</summary>
public static partial class TemplateRenderer
{
    /// <summary>
    /// Script fijo y propio (nunca de clientes), permitido por su huella SHA-256.
    /// Buscador desde 8 productos y botones por categoría desde 2 categorías. Si existe #pdet (plan Pro),
    /// al tocar un producto se abre su ficha: fotos deslizables, descripción completa y agregar al pedido.
    /// </summary>
    private const string CatJs = """
        (function(){var prods=[].slice.call(document.querySelectorAll('.prod'));if(!prods.length)return;var grid=prods[0].parentElement,box=grid.parentElement;
        function norm(s){return(s||'').toLowerCase().normalize('NFD').replace(/[̀-ͯ]/g,'').trim()}
        function el(t,c,x){var e=document.createElement(t);if(c)e.className=c;if(x!=null)e.textContent=x;return e}
        var heads=[].slice.call(grid.querySelectorAll('.catt')),cats=[],cur='',term='',none=null;
        heads.forEach(function(h){var t=h.textContent.trim();if(cats.map(norm).indexOf(norm(t))<0)cats.push(t)});
        function apply(){var shown=0;prods.forEach(function(p){var d=p.querySelector('.pb p');var ok=(!cur||norm(p.getAttribute('data-c'))===cur)&&(!term||norm((p.getAttribute('data-n')||'')+' '+(d?d.textContent:'')+' '+(p.getAttribute('data-c')||'')).indexOf(term)>=0);p.hidden=!ok;if(ok)shown++});
        heads.forEach(function(h){var n=h.nextElementSibling,v=false;while(n&&!n.classList.contains('catt')){if(!n.hidden)v=true;n=n.nextElementSibling}h.hidden=!v});if(none)none.hidden=shown>0}
        if(prods.length>=8||cats.length>1){var bar=el('div','cfind');
        if(prods.length>=8){var inp=el('input');inp.type='search';inp.placeholder='Buscar producto';inp.setAttribute('aria-label','Buscar producto');inp.addEventListener('input',function(){term=norm(inp.value);apply()});bar.appendChild(inp)}
        if(cats.length>1){var row=el('div','cchips');row.setAttribute('role','group');row.setAttribute('aria-label','Categorías');['Todos'].concat(cats).forEach(function(t,i){var b=el('button',i?'':'on',t);b.type='button';b.setAttribute('aria-pressed',i?'false':'true');
        b.addEventListener('click',function(){cur=i?norm(t):'';[].forEach.call(row.children,function(x){x.classList.toggle('on',x===b);x.setAttribute('aria-pressed',x===b?'true':'false')});apply();if(bar.getBoundingClientRect().top<0)bar.scrollIntoView({block:'start'})});row.appendChild(b)});bar.appendChild(row)}
        none=el('p','cnone','No encontramos productos con esa búsqueda.');none.hidden=true;box.insertBefore(bar,grid);box.insertBefore(none,grid.nextSibling)}
        var det=document.getElementById('pdet');if(!det)return;document.body.classList.add('det-on');
        var ph=det.querySelector('.dph'),dots=det.querySelector('.ddots'),dn=det.querySelector('.dn'),dp=det.querySelector('.dp'),dd=det.querySelector('.dd'),dq=det.querySelector('.dq'),open=null,last=null;
        function btn(t,l,f){var b=el('button','',t);b.type='button';if(l)b.setAttribute('aria-label',l);b.addEventListener('click',function(){f();sync()});return b}
        function act(a){var b=open&&open.querySelector('[data-act='+a+']');if(b)b.click()}
        function sync(){dq.textContent='';if(!open||!document.getElementById('cart')||!open.querySelector('[data-act=inc]'))return;var s=open.querySelector('.qty span'),n=s?parseInt(s.textContent,10)||0:0;
        if(!n){var a=btn('Agregar al pedido','',function(){act('inc')});a.className='dadd';dq.appendChild(a)}else{dq.appendChild(btn('−','Quitar uno',function(){act('dec')}));dq.appendChild(el('span','',n+' en tu pedido'));dq.appendChild(btn('+','Agregar uno',function(){act('inc')}))}}
        function show(p){open=p;last=document.activeElement;ph.textContent='';dots.textContent='';var list=(p.getAttribute('data-ph')||'').split(' ').filter(Boolean);
        list.forEach(function(u,i){var im=el('img');im.src=u;im.alt=(p.getAttribute('data-n')||'')+(list.length>1?' · foto '+(i+1):'');ph.appendChild(im);if(list.length>1)dots.appendChild(el('i',i?'':'on'))});ph.hidden=!list.length;
        dn.textContent=p.getAttribute('data-n')||'';var pr=p.querySelector('.pr');dp.textContent=pr?pr.textContent:'';var m=p.querySelector('.pmore'),s=p.querySelector('.pb p');dd.textContent='';if(m)dd.innerHTML=m.innerHTML;else if(s)dd.textContent=s.textContent;
        sync();det.hidden=false;ph.scrollLeft=0;det.querySelector('.dbox').scrollTop=0;document.documentElement.classList.add('det-lock');det.querySelector('.dclose').focus()}
        function hide(){det.hidden=true;open=null;document.documentElement.classList.remove('det-lock');if(last&&last.focus)last.focus()}
        ph.addEventListener('scroll',function(){var i=Math.round(ph.scrollLeft/Math.max(1,ph.clientWidth));[].forEach.call(dots.children,function(d,j){d.className=j===i?'on':''})});
        document.addEventListener('click',function(e){var t=e.target;if(!t.closest)return;if(t===det||t.closest('.dclose')){hide();return}if(t.closest('#pdet')||t.closest('[data-act]'))return;var p=t.closest('.prod');if(p)show(p)});
        document.addEventListener('keydown',function(e){if(!det.hidden&&e.key==='Escape')hide()});
        prods.forEach(function(p){p.tabIndex=0;p.setAttribute('role','button');p.setAttribute('aria-label','Ver detalle de '+(p.getAttribute('data-n')||'producto'));p.addEventListener('keydown',function(e){if((e.key==='Enter'||e.key===' ')&&e.target===p){e.preventDefault();show(p)}})})})();
        """;

    private static readonly string CatHash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(CatJs)));

    /// <summary>Ventana de la ficha del producto (plan Pro); el script la llena con el producto que se toca.</summary>
    private const string DetailHtml =
        "<div id=\"pdet\" hidden role=\"dialog\" aria-modal=\"true\" aria-label=\"Detalle del producto\"><div class=\"dbox\">" +
        "<button type=\"button\" class=\"dclose\" aria-label=\"Cerrar\">✕</button><div class=\"dph\"></div><div class=\"ddots\"></div>" +
        "<div class=\"dtx\"><h3 class=\"dn\"></h3><b class=\"dp\"></b><div class=\"dd\"></div><div class=\"dq\"></div></div></div></div>";

    private const string CatCss = """
        .prod[hidden],.catt[hidden]{display:none!important}
        .cfind{position:sticky;top:8px;z-index:20;display:grid;gap:8px;margin:0 0 14px;padding:8px;border-radius:16px;background:rgba(127,127,127,.16);-webkit-backdrop-filter:blur(12px);backdrop-filter:blur(12px)}
        .cfind input{width:100%;box-sizing:border-box;padding:11px 14px;border:1px solid rgba(127,127,127,.3);border-radius:12px;background:#fff;color:#111;font:16px system-ui,sans-serif}
        .cchips{display:flex;gap:6px;overflow-x:auto;scrollbar-width:none}.cchips::-webkit-scrollbar{display:none}
        .cchips button{flex:none;padding:7px 14px;border-radius:999px;border:1px solid rgba(127,127,127,.4);background:rgba(255,255,255,.08);color:inherit;font:600 .85rem system-ui,sans-serif;cursor:pointer;white-space:nowrap}
        .cchips button.on{background:#111;color:#fff;border-color:#111}
        .cnone{text-align:center;opacity:.7;margin:18px 0}
        .det-on .prod{cursor:pointer}.det-on .prod[data-more-ph]{position:relative}
        .det-on .prod[data-more-ph]::after{content:attr(data-more-ph);position:absolute;top:8px;left:8px;padding:2px 8px;border-radius:999px;background:rgba(0,0,0,.6);color:#fff;font:600 .7rem system-ui,sans-serif;pointer-events:none}
        .det-lock,.det-lock body{overflow:hidden}
        #pdet{position:fixed;inset:0;z-index:70;display:flex;align-items:flex-end;justify-content:center;background:rgba(0,0,0,.55)}#pdet[hidden]{display:none}
        #pdet .dbox{position:relative;width:100%;max-width:480px;max-height:92vh;overflow:auto;background:#fff;color:#111;border-radius:20px 20px 0 0;font-family:system-ui,sans-serif;text-align:left}
        #pdet .dclose{position:absolute;top:10px;right:10px;z-index:2;width:36px;height:36px;border:0;border-radius:50%;background:rgba(255,255,255,.94);color:#111;font-size:1rem;cursor:pointer;box-shadow:0 1px 6px rgba(0,0,0,.25)}
        #pdet .dph{display:flex;overflow-x:auto;scroll-snap-type:x mandatory;scrollbar-width:none;background:#f3f4f6}#pdet .dph::-webkit-scrollbar{display:none}#pdet .dph[hidden]{display:none}
        #pdet .dph img{flex:0 0 100%;width:100%;aspect-ratio:1/1;object-fit:cover;scroll-snap-align:center;display:block}
        #pdet .ddots{display:flex;justify-content:center;gap:6px;margin-top:10px}#pdet .ddots:empty{display:none}
        #pdet .ddots i{width:7px;height:7px;border-radius:50%;background:#d1d5db}#pdet .ddots i.on{background:#111}
        #pdet .dtx{display:grid;gap:8px;padding:16px 18px 24px}#pdet .dn{margin:0;padding-right:40px;font-size:1.25rem;line-height:1.25}
        #pdet .dp{font-size:1.15rem}#pdet .dp:empty{display:none}#pdet .dd{color:#4b5563;font-size:.95rem;line-height:1.55}
        #pdet .dq{display:flex;align-items:center;gap:12px;margin-top:8px;padding-right:56px}#pdet .dq:empty{display:none}
        #pdet .dq button{width:40px;height:40px;border:0;border-radius:50%;background:#0b0f14;color:#fff;font:700 1.2rem/1 system-ui,sans-serif;cursor:pointer}
        #pdet .dq span{flex:1;text-align:center;font-weight:600}
        #pdet .dq .dadd{flex:1;width:auto;height:auto;padding:14px;border-radius:999px;background:#1e9e54;font-size:1rem}
        """;

    /// <summary>
    /// Un producto se vuelve su tarjeta (todo escapado). Con la ficha (plan Pro) lleva además sus fotos en data-ph
    /// y la descripción larga oculta, que el script muestra al tocar el producto.
    /// </summary>
    private static string CatalogItem(TarjetasPro.Domain.Cards.CatalogProduct p, string assetBase, bool detail)
    {
        var photos = (p.Photos ?? []).Select(u => SafeUrl(u, assetBase)).Where(u => u.Length > 0).ToArray();
        var name = Enc(p.Name);
        var pn = PriceRx().Match(p.Price).Value.Replace(",", "");
        var sb = new System.Text.StringBuilder($"<article class=\"prod\" data-n=\"{name}\"");
        if (pn.Length > 0) sb.Append(" data-p=\"").Append(Enc(pn)).Append('"');
        if (p.Cat.Length > 0) sb.Append(" data-c=\"").Append(Enc(p.Cat)).Append('"');
        if (detail && photos.Length > 0) sb.Append(" data-ph=\"").Append(string.Join(' ', photos)).Append('"');
        if (detail && photos.Length > 1) sb.Append(" data-more-ph=\"+").Append(photos.Length - 1).Append(photos.Length == 2 ? " foto" : " fotos").Append('"');
        sb.Append('>');
        if (photos.Length > 0) sb.Append("<img src=\"").Append(photos[0]).Append("\" alt=\"").Append(name).Append("\" loading=\"lazy\">");
        sb.Append("<div class=\"pb\"><h3>").Append(name).Append("</h3>");
        if (p.Desc.Length > 0) sb.Append("<p>").Append(Enc(p.Desc).Replace("\n", " ")).Append("</p>");
        if (p.Price.Length > 0) sb.Append("<b class=\"pr\">").Append(Enc(p.Price)).Append("</b>");
        if (detail && p.More.Length > 0) sb.Append("<div class=\"pmore\" hidden>").Append(Enc(p.More).Replace("\n", "<br>")).Append("</div>");
        return sb.Append("<div class=\"qty\"><button type=\"button\" data-act=\"inc\" aria-label=\"Agregar\">+</button></div></div></article>").ToString();
    }
}
