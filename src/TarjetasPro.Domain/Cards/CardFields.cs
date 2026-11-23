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
        new("foto", "Foto", FieldType.Image),
        new("logo", "Logo", FieldType.Image),
        new("imagen", "Imagen de tu tarjeta", FieldType.Image),
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
        // Sedes adicionales (consultorios, sucursales): hasta 3 direcciones en total.
        new("ubicacion2", "Dirección 2", FieldType.Text),
        new("mapa2", "Link de mapa 2", FieldType.Url),
        new("ubicacion3", "Dirección 3", FieldType.Text),
        new("mapa3", "Link de mapa 3", FieldType.Url),
        new("horarios", "Horarios", FieldType.TextArea),
        new("servicios", "Servicios (uno por línea)", FieldType.List),
        // Invitaciones (eventos): reutilizan ubicacion, mapa y whatsapp (confirmar asistencia).
        new("evento", "Tipo de evento (ej. Mis XV años)", FieldType.Text),
        new("fecha", "Fecha del evento", FieldType.Text),
        new("hora", "Hora", FieldType.Text),
        new("vestimenta", "Código de vestimenta", FieldType.Text),
        new("mensaje", "Mensaje a los invitados", FieldType.TextArea),
        new("programa", "Programa (uno por línea)", FieldType.List),
        new("edad", "Edad que cumple (ej. 5)", FieldType.Text),
        new("tema", "Tema de la fiesta (ej. Unicornios)", FieldType.Text),
        new("padres", "Papás (ej. Familia Pérez López)", FieldType.Text),
        new("padrinos", "Padrinos (uno por línea)", FieldType.List),
        new("regalos", "Mesa de regalos o sugerencia", FieldType.TextArea),
        // Menú digital: una línea por platillo, con el formato «Nombre — $precio».
        new("menu_entradas", "Entradas (una por línea: Nombre — $precio)", FieldType.List),
        new("menu_principales", "Platos fuertes (una por línea: Nombre — $precio)", FieldType.List),
        new("menu_bebidas", "Bebidas (una por línea: Nombre — $precio)", FieldType.List),
        new("menu_postres", "Postres (una por línea: Nombre — $precio)", FieldType.List),
        // Catálogo en línea: una línea por producto, con el formato «Nombre | $precio | descripción | URL de foto» (solo el nombre es obligatorio).
        new("catalogo", "Productos (una línea: Nombre | $precio | descripción | foto | categoría)", FieldType.List),
        new("pedido_msg", "Mensaje del pedido (usa {pedido} y {total})", FieldType.TextArea),
        new("cols", "Columnas del catálogo (1, 2 o 3)", FieldType.Text),
        new("forma", "Tipo de producto (forma de las tarjetas)", FieldType.Text),
        // Lo pone el sistema con el nombre del invitado; el anfitrión no lo llena.
        new("invitado", "Nombre del invitado (automático)", FieldType.Text),
    ];

    public static readonly IReadOnlyDictionary<string, CardField> ByKey =
        All.ToDictionary(f => f.Key);

    public static readonly IReadOnlyDictionary<string, string> Sample = new Dictionary<string, string>
    {
        ["nombre"] = "Ana López",
        ["puesto"] = "Diseñadora UX",
        ["empresa"] = "Estudio Aurora",
        ["descripcion"] = "Diseño experiencias digitales claras y bonitas para pequeños negocios.",
        ["foto"] = "data:image/svg+xml;utf8,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 200 200'%3E%3Crect width='200' height='200' fill='%23e5e9f0'/%3E%3Ccircle cx='100' cy='78' r='36' fill='%23b4bdcb'/%3E%3Cpath d='M30 200c0-44 31-72 70-72s70 28 70 72z' fill='%23b4bdcb'/%3E%3C/svg%3E",
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
        ["ubicacion2"] = "Calle Hidalgo 45, Monterrey",
        ["mapa2"] = "https://maps.google.com/?q=Calle+Hidalgo+45",
        ["horarios"] = "Lun a Vie: 9:00 - 18:00\nSáb: 10:00 - 14:00",
        ["evento"] = "Mis XV años",
        ["fecha"] = "Sábado 21 de marzo de 2026",
        ["hora"] = "7:00 p. m.",
        ["vestimenta"] = "Formal",
        ["mensaje"] = "Será un honor compartir contigo este día tan especial.",
        ["programa"] = "Misa\nRecepción\nVals\nCena y baile",
        ["invitado"] = "Familia Pérez",
        ["edad"] = "15",
        ["tema"] = "Rosa y dorado",
        ["padres"] = "Familia Pérez López",
        ["padrinos"] = "Rosa y Miguel Ramírez\nLaura y Jorge Torres",
        ["regalos"] = "Mesa de regalos en Liverpool, evento 51234567. ¡Tu presencia es lo más importante!",
        ["menu_entradas"] = "Guacamole con totopos — $95\nSopa de tortilla — $85\nTostadas de atún — $120",
        ["menu_principales"] = "Tacos al pastor (5) — $110\nEnchiladas suizas — $135\nArrachera con guarnición — $220",
        ["menu_bebidas"] = "Agua de horchata — $35\nCerveza artesanal — $65\nCafé de olla — $40",
        ["catalogo"] = "Vela de soya Lavanda | $189 | Cera de soya, 40 h de duración | https://picsum.photos/seed/vela1/400/400\nVela Vainilla en vaso ámbar | $229 | Aroma cálido y dulce | https://picsum.photos/seed/vela2/400/400\nDifusor Bambú | $279 | 100 ml con varitas de ratán | https://picsum.photos/seed/vela3/400/400",
        ["menu_postres"] = "Flan napolitano — $60\nChurros con chocolate — $75",
    };
}
