namespace TarjetasPro.Application.Cards;

/// <summary>
/// Teléfonos: el cliente captura 10 dígitos y elige el país (lada, por defecto México +52).
/// Se guardan separados: <c>whatsapp</c> + <c>whatsapp_lada</c> (igual <c>whatsapp2</c> y <c>telefono</c>).
/// Sin lada guardada (tarjetas anteriores) se asume México y se aceptan los formatos viejos.
/// </summary>
public static class Phones
{
    public const string DefaultLada = "52";
    private static readonly string[] Keys = ["whatsapp", "whatsapp2", "telefono"];

    public static bool IsPhoneKey(string key) => Keys.Contains(key);
    public static string LadaKey(string key) => key + "_lada";

    /// <summary>Claves extra (`*_lada`) que se deben conservar junto a cada campo de teléfono permitido.</summary>
    public static IEnumerable<string> WithLadas(IEnumerable<string> keys)
    {
        var list = keys.ToList();
        return list.Concat(list.Where(IsPhoneKey).Select(LadaKey));
    }

    public static string Digits(string? s) => new((s ?? "").Where(char.IsAsciiDigit).ToArray());

    /// <summary>Lada solo con dígitos (1 a 4); si no es válida, México.</summary>
    public static string CleanLada(string? lada)
    {
        var d = Digits(lada);
        return d.Length is >= 1 and <= 4 ? d : DefaultLada;
    }

    /// <summary>Número internacional solo con dígitos (para wa.me), o "" si no hay número usable.</summary>
    public static string Intl(string? raw, string? lada)
    {
        raw = (raw ?? "").Trim();
        var d = Digits(raw);
        if (d.Length < 7) return "";
        if (raw.StartsWith('+')) return d;                      // ya viene con país
        if (d.StartsWith("044") || d.StartsWith("045")) d = d[3..];
        else if (d.StartsWith("01") && d.Length == 12) d = d[2..];

        if (string.IsNullOrWhiteSpace(lada))
        {
            // Tarjetas anteriores: 10 dígitos = México; 12 con 52 o 13 con 521 ya traen país.
            if (d.Length == 10) return DefaultLada + d;
            if (d.Length == 13 && d.StartsWith("521")) return "52" + d[3..];
            return d;
        }
        var l = CleanLada(lada);
        if (d.Length > 10 && d.StartsWith(l)) d = d[l.Length..];  // lo pegaron completo
        if (l == "52" && d.Length == 11 && d[0] == '1') d = d[1..];
        return l + d;
    }

    /// <summary>"+52 55 1234 5678" para mostrar y para tel:.</summary>
    public static string Display(string? raw, string? lada)
    {
        var i = Intl(raw, lada);
        if (i.Length == 0) return "";
        if (i.StartsWith("52") && i.Length == 12) return $"+52 {i[2..4]} {i[4..8]} {i[8..]}";
        var l = string.IsNullOrWhiteSpace(lada) || raw!.TrimStart().StartsWith('+') ? "" : CleanLada(lada);
        return l.Length > 0 && i.StartsWith(l) ? $"+{l} {i[l.Length..]}" : "+" + i;
    }
}
