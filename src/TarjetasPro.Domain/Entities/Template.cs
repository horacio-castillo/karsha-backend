namespace TarjetasPro.Domain.Entities;

public class Template
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string Description { get; set; } = "";
    /// <summary>Giro (ver TemplateCategories) y profesión u oficio opcional, p. ej. "Pediatra".</summary>
    public string Category { get; set; } = "General";
    public string Profession { get; set; } = "";
    public string Html { get; set; } = "";
    public string Css { get; set; } = "";
    /// <summary>Claves de campos usadas por el HTML, separadas por coma (se calcula al guardar).</summary>
    public string FieldKeys { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
