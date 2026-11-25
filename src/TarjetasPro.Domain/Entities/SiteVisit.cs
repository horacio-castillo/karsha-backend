namespace TarjetasPro.Domain.Entities;

/// <summary>Visita a las páginas de KARSHA (inicio, landings, registro…), no a una tarjeta. Solo la ve el admin.</summary>
public class SiteVisit
{
    public long Id { get; set; }
    public DateTime VisitedAt { get; set; } = DateTime.UtcNow;
    /// <summary>Página que abrió, sin parámetros: /, /tarjetas-digitales, /registro…</summary>
    public string Path { get; set; } = "/";
    public string? Ip { get; set; }
    public string? Source { get; set; }
    public string? Device { get; set; }
    public string? Country { get; set; }
    public string? Region { get; set; }
    public string? City { get; set; }
}
