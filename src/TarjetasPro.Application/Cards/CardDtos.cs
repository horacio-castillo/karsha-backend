using System.ComponentModel.DataAnnotations;

namespace TarjetasPro.Application.Cards;

public record CardRequest(
    [Required, StringLength(100, MinimumLength = 2)] string Title,
    Guid TemplateId,
    [Required] string Slug,
    Dictionary<string, string>? Data,
    string? Plan = null,
    TarjetasPro.Application.Templates.CardTheme? Theme = null,
    string[]? Gallery = null,
    string? VideoUrl = null,
    TarjetasPro.Application.Canvas.CanvasDoc? Canvas = null,
    string? Product = null);

public record CardPreviewRequest(Guid TemplateId, Dictionary<string, string>? Data, string? Plan = null,
    TarjetasPro.Application.Templates.CardTheme? Theme = null, string[]? Gallery = null, string? VideoUrl = null,
    TarjetasPro.Application.Canvas.CanvasDoc? Canvas = null);

public record CardSummary(Guid Id, string Title, string Slug, Guid TemplateId, string TemplateName,
    string Status, DateTime UpdatedAt, int Visits, DateTime? ExpiresAt, string Plan, string Product = "tarjetas");

public record CardDetail(Guid Id, string Title, string Slug, Guid TemplateId, string Status,
    Dictionary<string, string> Data, string Plan, TarjetasPro.Application.Templates.CardTheme Theme,
    string[] Gallery, string? VideoUrl, bool PlanLocked, string Product = "tarjetas", TarjetasPro.Application.Canvas.CanvasDoc? Canvas = null);

public record SlugCheck(bool Available, string Slug, string? Reason);
/// <summary>PriceCents ya incluye la promoción; ListPriceCents es el precio sin descuento.</summary>
public record PlanInfo(string Key, string Name, int PriceCents, string[] Features, int ListPriceCents = 0, string? PromoLabel = null, DateTime? PromoEndsAt = null, string Tagline = "", bool IsHighlighted = false);
public record FontInfo(string Key, string Stack);
public record CardsConfig(int MaxCards, string PaymentProvider, string Currency, int Months, PlanInfo[] Plans, FontInfo[] Fonts,
    string[] Decors, string[] ButtonStyles, string[] PhotoStyles, string[] ButtonLayouts, string[] GalleryLayouts);

public record PublicCard(string Slug, string Html, string Name, string Title, string Description, string? ImageUrl);
