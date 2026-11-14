namespace TarjetasPro.Domain.Entities;

/// <summary>Producto que se vende (Tarjetas, Invitaciones…). Los alta el Admin; cada plan pertenece a uno.</summary>
public class ProductSetting
{
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Identificador estable (minúsculas, sin espacios). No cambia una vez creado.</summary>
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}

/// <summary>Claves de los productos que el sistema ya sabe cobrar.</summary>
public static class Products
{
    public const string Cards = "tarjetas";
    public const string Invitations = "invitaciones";
    public const string Menu = "menu";

    /// <summary>Productos que se arman con el editor de tarjetas (una página pública con enlace y QR).</summary>
    public static bool IsCardLike(string product) => product is Cards or Menu;
}

/// <summary>Datos comerciales de un plan, editables por el Admin: nombre, precio y qué incluye.
/// Para el producto Tarjetas, <see cref="Plan"/> liga el plan con sus capacidades técnicas (temas, galería, estadísticas).
/// Los planes de otros productos no tienen <see cref="Plan"/>.</summary>
public class PlanSetting
{
    public const int MinPriceCents = 1000;
    public const int MaxPriceCents = 10_000_000;

    public Guid Id { get; set; } = Guid.NewGuid();
    public string Product { get; set; } = Products.Cards;
    /// <summary>Identificador estable dentro del producto (minúsculas, sin espacios).</summary>
    public string Code { get; set; } = "";
    public CardPlan? Plan { get; set; }
    public string Name { get; set; } = "";
    /// <summary>Frase corta bajo el nombre ("Para empezar").</summary>
    public string Tagline { get; set; } = "";
    /// <summary>Precio por periodo, en centavos.</summary>
    public int PriceCents { get; set; }
    /// <summary>Una ventaja por línea.</summary>
    public string Features { get; set; } = "";
    /// <summary>Se marca como "Más popular" en la web.</summary>
    public bool IsHighlighted { get; set; }
    /// <summary>Un plan apagado no se ofrece a clientes nuevos.</summary>
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string[] FeatureList() =>
        Features.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
