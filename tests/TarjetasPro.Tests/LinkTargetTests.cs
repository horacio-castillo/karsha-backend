using TarjetasPro.Application.Templates;

namespace TarjetasPro.Tests;

/// <summary>WhatsApp, llamar y mapa se abren en la misma pestaña para que «atrás» regrese a la tarjeta.</summary>
public class LinkTargetTests
{
    private const string Html = """
        {{#whatsapp}}<a class="btn" href="https://wa.me/{{whatsapp}}">WhatsApp</a>{{/whatsapp}}
        {{#telefono}}<a class="btn" href="tel:{{telefono}}">Llamar</a>{{/telefono}}
        {{#mapa}}<a class="btn" href="{{mapa}}">Ubicación</a>{{/mapa}}
        {{#web}}<a class="btn" href="{{web}}">Sitio web</a>{{/web}}
        """;

    private static string RenderPublic() => TemplateRenderer.Render(Html, "", new Dictionary<string, string>
    {
        ["whatsapp"] = "5512345678",
        ["telefono"] = "5512345678",
        ["mapa"] = "https://maps.google.com/?q=Reforma+123",
        ["web"] = "https://ejemplo.mx",
    }, extras: new TemplateRenderer.Extras(TrackBase: "https://api.karsha.mx/api/public/c/demo/go"));

    [Theory]
    [InlineData("whatsapp")]
    [InlineData("telefono")]
    [InlineData("mapa")]
    public void Botones_de_app_abren_en_la_misma_pestana(string key)
    {
        Assert.Contains($"href=\"https://api.karsha.mx/api/public/c/demo/go/{key}\" target=\"_top\"", RenderPublic());
    }

    [Fact]
    public void Sitio_web_sigue_en_pestana_nueva()
    {
        var html = RenderPublic();
        Assert.Contains("href=\"https://api.karsha.mx/api/public/c/demo/go/web\"", html);
        Assert.DoesNotContain("go/web\" target=\"_top\"", html);
    }
}
