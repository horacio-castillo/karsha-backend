using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace TarjetasPro.Domain.Cards;

public static partial class SlugRules
{
    public const int Min = 3;
    public const int Max = 30;

    /// <summary>Rutas de la app y palabras que no se pueden usar como slug (viven en la raíz del dominio).</summary>
    public static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "login", "registro", "dashboard", "admin", "api", "uploads", "assets", "static", "tarjetas",
        "plantillas", "usuarios", "perfil", "cuenta", "ayuda", "soporte", "contacto", "precios", "planes",
        "terminos", "privacidad", "about", "www", "app", "mail", "root", "system", "sistema", "tarjetaspro",
        "logout", "salir", "pago", "pagos", "checkout", "docs", "swagger", "openapi", "health", "c", "verificar-correo", "i", "invitaciones", "invitacion", "recibo",
    };

    [GeneratedRegex(@"^[a-z0-9](?:[a-z0-9-]{1,28})[a-z0-9]$")]
    private static partial Regex ValidRx();

    [GeneratedRegex(@"-{2,}")]
    private static partial Regex DashesRx();

    /// <summary>"Juan Pérez" → "juan-perez".</summary>
    public static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";
        var sb = new StringBuilder();
        foreach (var ch in input.Trim().Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            var c = char.ToLowerInvariant(ch);
            sb.Append(c is >= 'a' and <= 'z' or >= '0' and <= '9' ? c : '-');
        }
        var s = DashesRx().Replace(sb.ToString(), "-").Trim('-');
        return s.Length > Max ? s[..Max].TrimEnd('-') : s;
    }

    /// <summary>Devuelve null si es válido o el motivo del rechazo.</summary>
    public static string? Validate(string slug)
    {
        if (slug.Length < Min || slug.Length > Max)
            return $"El enlace debe tener entre {Min} y {Max} caracteres.";
        if (!ValidRx().IsMatch(slug))
            return "Solo minúsculas, números y guiones; sin guion al inicio o al final.";
        if (Reserved.Contains(slug))
            return "Ese nombre está reservado. Elige otro.";
        return null;
    }
}
