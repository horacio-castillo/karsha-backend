using Microsoft.EntityFrameworkCore;
using TarjetasPro.Application.Canvas;
using TarjetasPro.Application.Templates;
using TarjetasPro.Domain.Entities;

namespace TarjetasPro.Infrastructure.Auth;

/// <summary>Plantilla interna del lienzo (Pro Max): no sale en los catálogos; solo da un lugar a las tarjetas de lienzo.</summary>
public static class CanvasTemplateSeed
{
    private const string Html = "<div hidden>{{nombre}}{{puesto}}{{empresa}}{{descripcion}}{{foto}}{{whatsapp}}{{telefono}}{{email}}{{web}}{{ubicacion}}{{mapa}}</div>";

    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Templates.AnyAsync(t => t.Code == CanvasTemplate.Code)) return;
        db.Templates.Add(new Template
        {
            Name = "Lienzo libre", Code = CanvasTemplate.Code, Description = "Editor libre del plan Pro Max", Category = "General",
            Profession = "", Html = Html, Css = "", Product = Products.Cards,
            FieldKeys = string.Join(',', TemplateRenderer.ExtractFields(Html)),
        });
        await db.SaveChangesAsync();
    }
}
