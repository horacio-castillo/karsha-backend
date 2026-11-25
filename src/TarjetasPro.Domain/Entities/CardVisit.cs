namespace TarjetasPro.Domain.Entities;

public class CardVisit
{
    public long Id { get; set; }
    public Guid CardId { get; set; }
    public DateTime VisitedAt { get; set; } = DateTime.UtcNow;

    /// <summary>IP pública del visitante (solo la ve el admin).</summary>
    public string? Ip { get; set; }
    /// <summary>De dónde llegó: WhatsApp, Facebook, Instagram, Google, Código QR, Directo…</summary>
    public string? Source { get; set; }
    /// <summary>Móvil, Tablet o Computadora, más el sistema (iPhone, Android, Windows…).</summary>
    public string? Device { get; set; }
    public string? Country { get; set; }
    public string? Region { get; set; }
    public string? City { get; set; }
}
