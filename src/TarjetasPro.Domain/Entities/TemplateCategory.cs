namespace TarjetasPro.Domain.Entities;

public class TemplateCategory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
}
