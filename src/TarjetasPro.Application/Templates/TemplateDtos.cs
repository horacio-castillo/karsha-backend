using System.ComponentModel.DataAnnotations;

namespace TarjetasPro.Application.Templates;

public record TemplateSummary(Guid Id, string Name, string Code, string Description, string Category,
    string Profession, string[] Fields, bool IsActive, TemplateKnobs Knobs, string[] Sections, string Kind = "General", string[]? Icons = null, string Product = "tarjetas");

public record TemplateDetail(Guid Id, string Name, string Code, string Description, string Category,
    string Profession, string Html, string Css, string[] Fields, bool IsActive, string Product = "tarjetas");

public record TemplateRequest(
    [Required, StringLength(100, MinimumLength = 2)] string Name,
    [Required, RegularExpression("^[a-z0-9-]{2,40}$", ErrorMessage = "El código solo admite minúsculas, números y guiones (2 a 40).")] string Code,
    [StringLength(300)] string? Description,
    [Required, StringLength(50)] string Category,
    [StringLength(80)] string? Profession,
    [Required, StringLength(100_000)] string Html,
    [StringLength(100_000)] string? Css,
    bool IsActive = true,
    [StringLength(30)] string? Product = null);

public record PreviewRequest(string Html, string? Css, Dictionary<string, string>? Data);
public record PreviewResponse(string Html, string[] Fields, string[] UnknownFields);
