using System.Net;

namespace TarjetasPro.Infrastructure.Auth;

/// <summary>Diseño común de los correos de KARSHA: tablas y estilos en línea para que se vea igual en Gmail, Outlook y el celular.</summary>
public static class EmailTemplate
{
    public static string Render(string preheader, string title, string introHtml, string buttonText, string link, string noteHtml,
        string detailsHtml = "", string footer = "Recibiste este correo porque tienes una cuenta en KARSHA.", string icon = "&#9993;")
    {
        var href = WebUtility.HtmlEncode(link);
        var hasButton = !string.IsNullOrWhiteSpace(link) && !string.IsNullOrWhiteSpace(buttonText);
        var buttonRow = hasButton
            ? $"<tr><td align=\"left\" style=\"padding:22px 32px 8px\"><a href=\"{href}\" style=\"display:inline-block;padding:14px 28px;border-radius:999px;background:#0b0f14;color:#ffffff;font-family:system-ui,-apple-system,'Segoe UI',Roboto,sans-serif;font-size:16px;font-weight:700;text-decoration:none\">{WebUtility.HtmlEncode(buttonText)}</a></td></tr>"
            : "";
        var fallback = hasButton
            ? $"<p style=\"margin:14px 0 0\">¿El botón no funciona? Copia y pega este enlace en tu navegador:<br><a href=\"{href}\" style=\"color:#0369a1;word-break:break-all\">{href}</a></p>"
            : "";
        var details = string.IsNullOrWhiteSpace(detailsHtml) ? "" : $"<tr><td style=\"padding:6px 32px 0\">{detailsHtml}</td></tr>";
        return $"""
            <!doctype html>
            <html lang="es"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><meta name="color-scheme" content="light"></head>
            <body style="margin:0;padding:0;background:#f5f8fb">
            <div style="display:none;max-height:0;overflow:hidden;opacity:0;color:transparent">{WebUtility.HtmlEncode(preheader)}</div>
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#f5f8fb;padding:28px 12px">
              <tr><td align="center">
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:520px">
                  <tr><td align="center" style="padding:0 0 18px;font-family:system-ui,-apple-system,'Segoe UI',Roboto,sans-serif;font-size:22px;font-weight:800;letter-spacing:.14em;color:#0b0f14">KARSHA</td></tr>
                  <tr><td style="background:#ffffff;border:1px solid #e6ecf2;border-radius:20px;overflow:hidden">
                    <table role="presentation" width="100%" cellpadding="0" cellspacing="0">
                      <tr><td style="height:6px;background:#0ea5e9;background-image:linear-gradient(90deg,#0369a1,#0ea5e9,#38bdf8);font-size:0;line-height:0">&nbsp;</td></tr>
                      <tr><td style="padding:34px 32px 8px;font-family:system-ui,-apple-system,'Segoe UI',Roboto,sans-serif;color:#1f2937">
                        <div style="width:52px;height:52px;border-radius:50%;background:#e0f2fe;color:#0369a1;font-size:26px;line-height:52px;text-align:center;margin:0 0 18px">{icon}</div>
                        <h1 style="margin:0 0 12px;font-size:24px;line-height:1.2;letter-spacing:-.02em;color:#0b0f14">{WebUtility.HtmlEncode(title)}</h1>
                        <div style="font-size:16px;line-height:1.6;color:#374151">{introHtml}</div>
                      </td></tr>
                      {details}
                      {buttonRow}
                      <tr><td style="padding:18px 32px 30px;font-family:system-ui,-apple-system,'Segoe UI',Roboto,sans-serif;font-size:13px;line-height:1.55;color:#6b7280">
                        {noteHtml}
                        {fallback}
                      </td></tr>
                    </table>
                  </td></tr>
                  <tr><td align="center" style="padding:18px 8px 0;font-family:system-ui,-apple-system,'Segoe UI',Roboto,sans-serif;font-size:12px;line-height:1.5;color:#9ca3af">
                    Tu tarjeta digital con enlace y código QR · KARSHA<br>{WebUtility.HtmlEncode(footer)}
                  </td></tr>
                </table>
              </td></tr>
            </table>
            </body></html>
            """;
    }
}
