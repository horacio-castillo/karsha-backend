namespace TarjetasPro.Domain.Cards;

/// <summary>Giros disponibles para clasificar plantillas. Editable desde el panel Admin en una fase posterior.</summary>
public static class TemplateCategories
{
    public const string General = "General";
    public const string Profesion = "Profesión";
    public const string Oficio = "Oficio";
    /// <summary>Invitaciones a eventos: otro producto, no se ofrecen en el editor de tarjetas.</summary>
    public const string Invitacion = "Invitación";

    public static readonly string[] All =
    [
        "Salud", "Legal y contable", "Educación", "Arquitectura e ingeniería",
        "Belleza y bienestar", "Oficios y servicios", "Comida", "Comercio", "Eventos y entretenimiento", "Invitaciones", General,
    ];

    private static readonly HashSet<string> Profesiones = ["Salud", "Legal y contable", "Educación", "Arquitectura e ingeniería"];
    private static readonly HashSet<string> Oficios =
        ["Belleza y bienestar", "Oficios y servicios", "Comida", "Comercio", "Eventos y entretenimiento"];

    /// <summary>Profesión (estudios) u Oficio (trabajo manual, servicio o negocio). General/otras se muestran en ambos.</summary>
    public static string KindOf(string category) =>
        Profesiones.Contains(category) ? Profesion : Oficios.Contains(category) ? Oficio : category == "Invitaciones" ? Invitacion : General;
}
