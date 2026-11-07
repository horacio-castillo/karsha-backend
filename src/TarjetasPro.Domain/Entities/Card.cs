namespace TarjetasPro.Domain.Entities;

public enum CardStatus { Draft, Published, Suspended, Expired }

public class Card
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid TemplateId { get; set; }
    public Template? Template { get; set; }

    /// <summary>Nombre interno para que el cliente distinga sus tarjetas.</summary>
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    /// <summary>JSON {campo: valor}. Se sanea al renderizar.</summary>
    public string DataJson { get; set; } = "{}";

    /// <summary>Plan Media+: JSON de personalización (colores, tipografía, fondo, campos ocultos).</summary>
    public string ThemeJson { get; set; } = "{}";
    /// <summary>Plan Pro: JSON con la lista de imágenes de la galería.</summary>
    public string GalleryJson { get; set; } = "[]";
    /// <summary>Plan Pro: enlace a un video (YouTube u otro).</summary>
    public string? VideoUrl { get; set; }

    public CardStatus Status { get; set; } = CardStatus.Draft;
    /// <summary>Plan vigente: define qué funciones están disponibles (personalización, galería, estadísticas).</summary>
    public CardPlan Plan { get; set; } = CardPlan.Sencilla;
    public DateTime? PaidAt { get; set; }
    /// <summary>Fin del periodo pagado. Null = sin vencimiento (publicada por el Admin).</summary>
    public DateTime? ExpiresAt { get; set; }
    /// <summary>Borrado lógico: la tarjeta nunca se elimina de la base, solo se oculta.</summary>
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
