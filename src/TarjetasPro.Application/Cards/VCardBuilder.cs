using System.Text;
using System.Text.RegularExpressions;

namespace TarjetasPro.Application.Cards;

public static partial class VCardBuilder
{
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRx();

    public static string Build(IReadOnlyDictionary<string, string> d)
    {
        string Get(string k) => d.TryGetValue(k, out var v) ? v.Trim() : "";

        var name = Get("nombre");
        var sb = new StringBuilder();
        sb.Append("BEGIN:VCARD\r\nVERSION:3.0\r\n");

        var parts = name.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        sb.Append($"N:{Esc(parts.Length > 1 ? parts[1] : "")};{Esc(parts.Length > 0 ? parts[0] : "")};;;\r\n");
        sb.Append($"FN:{Esc(name)}\r\n");

        Add(sb, "ORG", Get("empresa"));
        Add(sb, "TITLE", Get("puesto"));

        var wa = Phones.Intl(Get("whatsapp"), Get("whatsapp_lada")) is { Length: > 0 } w1 ? "+" + w1 : "";
        if (wa.Length > 0) sb.Append($"TEL;TYPE=CELL:{wa}\r\n");
        var wa2 = Phones.Intl(Get("whatsapp2"), Get("whatsapp2_lada")) is { Length: > 0 } w2 ? "+" + w2 : "";
        if (wa2.Length > 0) sb.Append($"TEL;TYPE=CELL:{wa2}\r\n");
        var tel = Phones.Intl(Get("telefono"), Get("telefono_lada")) is { Length: > 0 } w3 ? "+" + w3 : "";
        if (tel.Length > 0) sb.Append($"TEL;TYPE=WORK,VOICE:{tel}\r\n");

        var email = Get("email");
        if (EmailRx().IsMatch(email)) sb.Append($"EMAIL;TYPE=INTERNET:{Esc(email)}\r\n");

        var web = Get("web");
        if (web.Length > 0 && !web.Contains("://")) web = "https://" + web;
        if (Uri.TryCreate(web, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
            sb.Append($"URL:{uri.AbsoluteUri}\r\n");

        foreach (var k in new[] { "ubicacion", "ubicacion2", "ubicacion3" })
        {
            var addr = Get(k);
            if (addr.Length > 0) sb.Append($"ADR;TYPE=WORK:;;{Esc(addr)};;;;\r\n");
        }
        Add(sb, "NOTE", Get("descripcion"));

        sb.Append("END:VCARD\r\n");
        return sb.ToString();
    }

    private static void Add(StringBuilder sb, string key, string value)
    {
        if (value.Length > 0) sb.Append($"{key}:{Esc(value)}\r\n");
    }

    private static string Esc(string s) =>
        s.Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,")
         .Replace("\r\n", "\\n").Replace("\n", "\\n").Replace("\r", "");
}
