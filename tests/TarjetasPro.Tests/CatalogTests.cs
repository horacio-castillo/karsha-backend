using TarjetasPro.Application.Templates;
using TarjetasPro.Domain.Cards;

namespace TarjetasPro.Tests;

/// <summary>Productos del catálogo: JSON nuevo, formato viejo de líneas y ficha del producto (plan Pro).</summary>
public class CatalogTests
{
    private const string Html = """<div class="grid">{{#catalogo}}{{.}}{{/catalogo}}</div>""";
    private const string Json = """[{"name":"Vela Lavanda","price":"$189","desc":"Cera de soya","cat":"Velas","photos":["/uploads/a.jpg","/uploads/b.jpg"],"more":"Dura 40 horas.\nHecha a mano."}]""";

    private static string Render(string catalogo, bool detail) => TemplateRenderer.Render(Html, "",
        new Dictionary<string, string> { ["catalogo"] = catalogo }, "https://api.karsha.mx", extras: new TemplateRenderer.Extras(Detail: detail));

    [Fact]
    public void Lee_el_formato_viejo_de_lineas()
    {
        var l = CatalogItems.Parse("Vela | $189 | Soya | https://x.mx/a.jpg | Velas\n | $1 | sin nombre\nDifusor | $279");
        Assert.Equal(2, l.Count);
        Assert.Equal(("Vela", "$189", "Soya", "Velas"), (l[0].Name, l[0].Price, l[0].Desc, l[0].Cat));
        Assert.Equal("https://x.mx/a.jpg", l[0].MainPhoto);
        Assert.Equal("Difusor", l[1].Name);
    }

    [Fact]
    public void Guarda_en_JSON_y_lo_vuelve_a_leer_igual()
    {
        var once = CatalogItems.Parse(Json);
        var again = CatalogItems.Parse(CatalogItems.Serialize(once));
        Assert.Single(again);
        Assert.Equal(2, again[0].Photos!.Length);
        Assert.Equal("Dura 40 horas.\nHecha a mano.", again[0].More);
        Assert.StartsWith("[", CatalogItems.Serialize(once));
    }

    [Fact]
    public void Json_roto_no_truena() => Assert.Empty(CatalogItems.Parse("[{\"name\":"));

    [Fact]
    public void Recorta_fotos_y_textos()
    {
        var many = string.Join(",", Enumerable.Range(1, 9).Select(i => $"\"/uploads/{i}.jpg\""));
        var l = CatalogItems.Parse($$"""[{"name":"{{new string('a', 200)}}","photos":[{{many}}]}]""");
        Assert.Equal(80, l[0].Name.Length);
        Assert.Equal(CatalogItems.MaxPhotos, l[0].Photos!.Length);
    }

    [Fact]
    public void Sin_plan_Pro_solo_se_ve_la_foto_principal()
    {
        var html = Render(Json, detail: false);
        Assert.Contains("src=\"https://api.karsha.mx/uploads/a.jpg\"", html);
        Assert.DoesNotContain("b.jpg", html);
        Assert.DoesNotContain("Dura 40 horas", html);
        Assert.DoesNotContain("id=\"pdet\"", html);
        Assert.Contains("data-c=\"Velas\"", html);
    }

    [Fact]
    public void Con_plan_Pro_lleva_la_ficha_con_todas_las_fotos()
    {
        var html = Render(Json, detail: true);
        Assert.Contains("data-ph=\"https://api.karsha.mx/uploads/a.jpg https://api.karsha.mx/uploads/b.jpg\"", html);
        Assert.Contains("<div class=\"pmore\" hidden>Dura 40 horas.<br>Hecha a mano.</div>", html);
        Assert.Contains("id=\"pdet\"", html);
    }

    [Fact]
    public void Textos_del_producto_van_escapados()
    {
        var html = Render("""[{"name":"<script>x</script>","more":"<img onerror=1>"}]""", detail: true);
        Assert.DoesNotContain("<script>x", html);
        Assert.DoesNotContain("<img onerror", html);
    }
}
