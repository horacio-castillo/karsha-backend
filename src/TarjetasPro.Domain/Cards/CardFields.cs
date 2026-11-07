namespace TarjetasPro.Domain.Cards;

public enum FieldType { Text, TextArea, Phone, Whatsapp, Email, Url, Image, List }

public record CardField(string Key, string Label, FieldType Type);

/// <summary>Catálogo de campos que una tarjeta puede llevar. Las plantillas solo pueden usar estas claves.</summary>
public static class CardFields
{
    public static readonly IReadOnlyList<CardField> All =
    [
        new("nombre", "Nombre", FieldType.Text),
        new("puesto", "Puesto", FieldType.Text),
        new("empresa", "Empresa", FieldType.Text),
        new("eslogan", "Eslogan", FieldType.Text),
        new("descripcion", "Descripción", FieldType.TextArea),
        new("foto", "Foto (URL)", FieldType.Image),
        new("logo", "Logo (URL)", FieldType.Image),
        new("imagen", "Imagen de tu tarjeta (URL)", FieldType.Image),
        new("whatsapp", "WhatsApp", FieldType.Whatsapp),
        new("whatsapp2", "WhatsApp 2", FieldType.Whatsapp),
        new("telefono", "Teléfono", FieldType.Phone),
        new("email", "Correo", FieldType.Email),
        new("web", "Sitio web", FieldType.Url),
        new("facebook", "Facebook", FieldType.Url),
        new("instagram", "Instagram", FieldType.Url),
        new("tiktok", "TikTok", FieldType.Url),
        new("linkedin", "LinkedIn", FieldType.Url),
        new("ubicacion", "Dirección", FieldType.Text),
        new("mapa", "Link de mapa", FieldType.Url),
        new("horarios", "Horarios", FieldType.TextArea),
        new("servicios", "Servicios (uno por línea)", FieldType.List),
        // Invitaciones (eventos): reutilizan ubicacion, mapa y whatsapp (confirmar asistencia).
        new("evento", "Tipo de evento (ej. Mis XV años)", FieldType.Text),
        new("fecha", "Fecha del evento", FieldType.Text),
        new("hora", "Hora", FieldType.Text),
        new("vestimenta", "Código de vestimenta", FieldType.Text),
        new("mensaje", "Mensaje a los invitados", FieldType.TextArea),
        new("programa", "Programa (uno por línea)", FieldType.List),
    ];

    public static readonly IReadOnlyDictionary<string, CardField> ByKey =
        All.ToDictionary(f => f.Key);

    public static readonly IReadOnlyDictionary<string, string> Sample = new Dictionary<string, string>
    {
        ["nombre"] = "Ana López",
        ["puesto"] = "Diseñadora UX",
        ["empresa"] = "Estudio Aurora",
        ["descripcion"] = "Diseño experiencias digitales claras y bonitas para pequeños negocios.",
        ["foto"] = "https://i.pravatar.cc/200?img=47",
        ["whatsapp"] = "5215512345678",
        ["whatsapp2"] = "5215587654321",
        ["eslogan"] = "Cuidamos de ti con cariño",
        ["servicios"] = "Consulta general\nControl de crecimiento\nVacunación\nTamiz auditivo\nAsesoría en lactancia\nUrgencias",
        ["telefono"] = "+52 55 1234 5678",
        ["email"] = "ana@estudioaurora.com",
        ["web"] = "https://estudioaurora.com",
        ["facebook"] = "https://facebook.com/estudioaurora",
        ["instagram"] = "https://instagram.com/estudioaurora",
        ["ubicacion"] = "Av. Reforma 123, CDMX",
        ["mapa"] = "https://maps.google.com/?q=Av.+Reforma+123",
        ["horarios"] = "Lun a Vie: 9:00 - 18:00\nSáb: 10:00 - 14:00",
        ["evento"] = "Mis XV años",
        ["fecha"] = "Sábado 21 de marzo de 2026",
        ["hora"] = "7:00 p. m.",
        ["vestimenta"] = "Formal",
        ["mensaje"] = "Será un honor compartir contigo este día tan especial.",
        ["programa"] = "Misa\nRecepción\nVals\nCena y baile",
    };
}
