namespace TarjetasPro.Domain.Entities;

/// <summary>Clic en un botón de una tarjeta Pro (whatsapp, telefono, instagram…).</summary>
public class CardClick
{
    public long Id { get; set; }
    public Guid CardId { get; set; }
    public string Kind { get; set; } = "";
    public DateTime ClickedAt { get; set; } = DateTime.UtcNow;
}
