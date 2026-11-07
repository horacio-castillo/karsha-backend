namespace TarjetasPro.Domain.Entities;

public class CardVisit
{
    public long Id { get; set; }
    public Guid CardId { get; set; }
    public DateTime VisitedAt { get; set; } = DateTime.UtcNow;
}
