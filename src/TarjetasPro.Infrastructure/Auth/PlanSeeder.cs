using Microsoft.EntityFrameworkCore;
using TarjetasPro.Domain.Entities;
using TarjetasPro.Infrastructure.Payments;

namespace TarjetasPro.Infrastructure.Auth;

/// <summary>Crea los planes de Tarjetas la primera vez, tomando los precios de la configuración. Después manda la base de datos.</summary>
public static class PlanSeeder
{
    public static async Task SeedAsync(AppDbContext db, PaymentOptions opt)
    {
        if (!await db.ProductSettings.AnyAsync(x => x.Key == Products.Cards))
            db.ProductSettings.Add(new ProductSetting { Key = Products.Cards, Name = "Tarjetas digitales", Description = "Tarjeta de presentación con enlace y QR", SortOrder = 1 });
        if (!await db.ProductSettings.AnyAsync(x => x.Key == Products.Invitations))
            db.ProductSettings.Add(new ProductSetting { Key = Products.Invitations, Name = "Invitaciones", Description = "XV años, bodas, bautizos y más", SortOrder = 2 });
        if (!await db.ProductSettings.AnyAsync(x => x.Key == Products.Menu))
            db.ProductSettings.Add(new ProductSetting { Key = Products.Menu, Name = "Menú digital", Description = "Menú de tu restaurante con enlace y QR", SortOrder = 3 });
        var have = await db.PlanSettings.Where(p => p.Product == Products.Cards).Select(p => p.Code).ToListAsync();
        void Add(CardPlan plan, int order, string tagline, bool star, params string[] features)
        {
            var code = plan.ToString().ToLowerInvariant();
            if (have.Contains(code)) return;
            db.PlanSettings.Add(new PlanSetting
            {
                Product = Products.Cards, Code = code, Plan = plan, Name = plan.ToString(), Tagline = tagline,
                PriceCents = opt.Prices.For(plan), Features = string.Join('\n', features), IsHighlighted = star, SortOrder = order,
            });
        }
        Add(CardPlan.Sencilla, 1, "Para empezar", false, "Plantilla prediseñada", "Tus datos, WhatsApp y redes", "Enlace propio y código QR");
        Add(CardPlan.Media, 2, "Hazla tuya", true, "Todo lo de Sencilla", "Colores y tipografía a tu gusto", "Fondo de color o imagen", "Muestra, oculta y ordena secciones");
        Add(CardPlan.Pro, 3, "Para destacar", false, "Todo lo de Media", "Galería de fotos y video", "Estadísticas detalladas: visitas y clics");
        Add(CardPlan.ProMax, 4, "Diseña tú mismo", false, "Todo lo de Pro", "Editor libre tipo Canva: arrastra textos, fotos, botones y formas", "Capas, alineación y deshacer", "Tu tarjeta como tú la imaginas");
        // Menú digital: mismos niveles técnicos que Tarjetas. Los precios iniciales son un punto de partida: se ajustan en Admin → Productos y planes.
        var haveMenu = await db.PlanSettings.Where(p => p.Product == Products.Menu).Select(p => p.Code).ToListAsync();
        void AddMenu(CardPlan plan, int order, string tagline, bool star, params string[] features)
        {
            var code = plan.ToString().ToLowerInvariant();
            if (haveMenu.Contains(code)) return;
            db.PlanSettings.Add(new PlanSetting
            {
                Product = Products.Menu, Code = code, Plan = plan, Name = plan.ToString(), Tagline = tagline,
                PriceCents = opt.Prices.For(plan), Features = string.Join('\n', features), IsHighlighted = star, SortOrder = order,
            });
        }
        AddMenu(CardPlan.Sencilla, 1, "Tu menú en línea", false, "Menú con platillos y precios", "Enlace propio y código QR", "Botón para pedir por WhatsApp");
        AddMenu(CardPlan.Media, 2, "Con tu estilo", true, "Todo lo de Sencilla", "Colores y tipografía de tu marca", "Fondo de color o imagen");
        AddMenu(CardPlan.Pro, 3, "Para destacar", false, "Todo lo de Media", "Galería de fotos y video", "Estadísticas de visitas y clics");
        AddMenu(CardPlan.ProMax, 4, "Diseña tú mismo", false, "Todo lo de Pro", "Editor libre tipo Canva: arrastra platillos, fotos, precios y formas", "Capas, alineación y deshacer");
        await db.SaveChangesAsync();
    }
}
