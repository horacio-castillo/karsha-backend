using System.Globalization;
using System.Net;

namespace TarjetasPro.Infrastructure.Auth;

/// <summary>Correos que envía KARSHA. Un solo lugar: lo usan el envío real y la vista previa del Admin.</summary>
public static class EmailCatalog
{
    public const string VerificationSubject = "Verifica tu correo · KARSHA";
    public const string PasswordResetSubject = "Restablece tu contraseña · KARSHA";
    public const string WelcomeSubject = "¡Bienvenido a KARSHA!";
    public const string AccountCreatedSubject = "Tu cuenta de KARSHA está lista";
    public const string SecuritySubject = "Cambió la seguridad de tu cuenta · KARSHA";

    public const string VerifiedByTeamSubject = "Verificamos tu correo · KARSHA";

    private static string E(string? s) => WebUtility.HtmlEncode(s ?? "");
    private static string P(string html, string margin = "0 0 10px") => $"<p style=\"margin:{margin}\">{html}</p>";
    private static readonly CultureInfo Mx = CultureInfo.GetCultureInfo("es-MX");

    public static string Money(int cents) => "$" + (cents / 100m).ToString("N2", CultureInfo.GetCultureInfo("en-US")) + " MXN";
    public static string Date(DateTime d) => d.ToString("d 'de' MMMM 'de' yyyy", Mx);

    /// <summary>Tabla de datos clave (folio, monto, vigencia…) con el mismo estilo en todos los correos.</summary>
    private static string Rows(params (string Label, string Value)[] rows) =>
        "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"border:1px solid #e6ecf2;border-radius:12px;background:#f8fafc\">" +
        string.Concat(rows.Select((r, i) =>
            $"<tr><td style=\"padding:10px 14px;{(i > 0 ? "border-top:1px solid #e6ecf2;" : "")}font-family:system-ui,-apple-system,'Segoe UI',Roboto,sans-serif;font-size:13px;color:#6b7280\">{E(r.Label)}</td>" +
            $"<td align=\"right\" style=\"padding:10px 14px;{(i > 0 ? "border-top:1px solid #e6ecf2;" : "")}font-family:system-ui,-apple-system,'Segoe UI',Roboto,sans-serif;font-size:14px;font-weight:600;color:#0b0f14\">{E(r.Value)}</td></tr>")) +
        "</table>";

    // 0 · Cuenta
    public static string Verification(string fullName, string link) => EmailTemplate.Render(
        "Un toque y tu cuenta queda lista para crear tu tarjeta.",
        "Confirma tu correo",
        P($"Hola <b>{E(fullName)}</b>, ¡bienvenido a KARSHA!") + P("Solo falta confirmar que este correo es tuyo para que puedas crear y publicar tu tarjeta digital.", "0"),
        "Confirmar mi correo", link,
        P("El enlace vence en 24 horas. Si tú no creaste esta cuenta, ignora este mensaje y no pasará nada.", "0"),
        footer: "Recibiste este correo porque alguien usó esta dirección para crear una cuenta.");

    public static string PasswordReset(string fullName, string link) => EmailTemplate.Render(
        "Crea una contraseña nueva en un minuto.",
        "Restablece tu contraseña",
        P($"Hola <b>{E(fullName)}</b>, recibimos una solicitud para cambiar la contraseña de tu cuenta.") + P("Toca el botón para elegir una nueva.", "0"),
        "Crear contraseña nueva", link,
        P("El enlace vence en 1 hora y solo sirve una vez. Si tú no lo pediste, ignora este mensaje: tu contraseña actual sigue funcionando.", "0"),
        icon: "&#128273;");

    public static string AccountCreated(string fullName, string link) => EmailTemplate.Render(
        "Crea tu contraseña y entra a tu cuenta.",
        "Tu cuenta está lista",
        P($"Hola <b>{E(fullName)}</b>, el equipo de KARSHA creó una cuenta para ti.") + P("Solo falta que elijas tu contraseña para poder entrar y administrar tus tarjetas.", "0"),
        "Crear mi contraseña", link,
        P("El enlace vence en 1 hora. Si caduca, entra a «¿Olvidaste tu contraseña?» y pide uno nuevo.", "0"),
        icon: "&#128100;");

    public static string Welcome(string fullName, string link) => EmailTemplate.Render(
        "Tu cuenta está lista. Crea tu primera tarjeta.",
        "¡Ya eres parte de KARSHA!",
        P($"Hola <b>{E(fullName)}</b>, tu correo quedó verificado.") + P("Elige un diseño, llena tus datos y comparte tu tarjeta con un enlace y un código QR. Te toma unos minutos.", "0"),
        "Crear mi tarjeta", link,
        P("Si necesitas ayuda, responde a este correo y con gusto te apoyamos.", "0"),
        icon: "&#127881;");

    public static string VerifiedByTeam(string fullName, string loginUrl, string resetUrl) => EmailTemplate.Render(
        "Tu cuenta ya está lista para entrar.",
        "Tu correo quedó verificado",
        P($"Hola <b>{E(fullName)}</b>, el equipo de KARSHA verificó tu correo.") + P("Ya puedes entrar con la contraseña que elegiste al crear tu cuenta.", "0"),
        "Iniciar sesión", loginUrl,
        P($"Si no pediste ayuda o no creaste esta cuenta, <a href=\"{E(resetUrl)}\" style=\"color:#0369a1\">cambia la contraseña</a> para que nadie más pueda entrar.", "0"),
        icon: "&#9989;");

    public static string SecurityNotice(string fullName, string what, DateTime whenUtc, string link) => EmailTemplate.Render(
        $"Se cambió tu {what}.",
        $"Se cambió tu {what}",
        P($"Hola <b>{E(fullName)}</b>, te avisamos que el {E(Date(whenUtc))} se cambió el {E(what)} de tu cuenta.") + P("Si fuiste tú, no tienes que hacer nada.", "0"),
        "No fui yo: proteger mi cuenta", link,
        P("Si no reconoces este cambio, restablece tu contraseña de inmediato y revisa tus sesiones.", "0"),
        icon: "&#128737;");

    // 1 · Pagos
    public static string Receipt(string fullName, string folio, string cardTitle, string plan, int cents, string method, DateTime paidAt, DateTime? periodEnd, string link) => EmailTemplate.Render(
        $"Recibo {folio} · {Money(cents)}",
        "Recibimos tu pago",
        P($"Hola <b>{E(fullName)}</b>, gracias. Este es el resumen de tu pago.", "0"),
        "Ver mi recibo", link,
        P("Guarda este correo como comprobante. También puedes descargar tu recibo desde tu panel.", "0"),
        detailsHtml: Rows(("Folio", folio), ("Tarjeta", cardTitle), ("Plan", plan), ("Monto", Money(cents)), ("Forma de pago", method), ("Fecha", Date(paidAt)),
            ("Vigente hasta", periodEnd is { } e ? Date(e) : "—")),
        icon: "&#129534;");

    public static string CashPending(string fullName, string cardTitle, int cents, string? reference, string? voucherUrl, DateTime? limit, string link) => EmailTemplate.Render(
        $"Tu pago de {Money(cents)} está pendiente.",
        "Tu pago está pendiente",
        P($"Hola <b>{E(fullName)}</b>, ya apartamos <b>{E(cardTitle)}</b>. Se publica en cuanto recibamos tu pago.", "0"),
        voucherUrl is null ? "Ver mi pago" : "Ver mi ficha de pago", voucherUrl ?? link,
        P(reference is null
            ? "Si pagarás en efectivo con tu asesor, entrégale el monto y él lo registra. Te avisamos por aquí cuando quede."
            : "Paga en cualquier tienda OXXO mostrando la referencia. Puede tardar unas horas en reflejarse.", "0"),
        detailsHtml: Rows(("Tarjeta", cardTitle), ("Monto a pagar", Money(cents)),
            ("Referencia", reference ?? "Pago en efectivo con tu asesor"), ("Pagar antes del", limit is { } l ? Date(l) : "—")),
        icon: "&#128181;");

    public static string PaymentFailed(string fullName, string cardTitle, int cents, string link) => EmailTemplate.Render(
        "No pudimos completar tu pago.",
        "No se completó tu pago",
        P($"Hola <b>{E(fullName)}</b>, el pago de <b>{E(Money(cents))}</b> por <b>{E(cardTitle)}</b> no se completó: la tarjeta no pasó o la ficha venció.") + P("No se hizo ningún cargo. Puedes intentarlo de nuevo cuando quieras.", "0"),
        "Intentar de nuevo", link,
        P("Si el problema continúa, prueba con otra tarjeta o paga en efectivo en OXXO.", "0"),
        icon: "&#9888;");

    // 2 · Tarjeta
    public static string CardPublished(string fullName, string cardTitle, string publicUrl) => EmailTemplate.Render(
        "Tu tarjeta ya está en línea. Compártela.",
        "¡Tu tarjeta ya está publicada!",
        P($"Hola <b>{E(fullName)}</b>, <b>{E(cardTitle)}</b> ya está visible para todos.") + P("Comparte este enlace por WhatsApp, redes o ponlo en tu código QR:", "0"),
        "Ver mi tarjeta", publicUrl,
        P("Puedes editar tus datos cuando quieras desde tu panel; los cambios se ven al instante.", "0"),
        icon: "&#128640;");

    public static string ExpiringSoon(string fullName, string cardTitle, DateTime expires, int daysLeft, string link) => EmailTemplate.Render(
        $"Tu tarjeta vence en {daysLeft} {(daysLeft == 1 ? "día" : "días")}.",
        daysLeft <= 1 ? "Tu tarjeta vence mañana" : $"Tu tarjeta vence en {daysLeft} días",
        P($"Hola <b>{E(fullName)}</b>, tu tarjeta <b>{E(cardTitle)}</b> vence el <b>{E(Date(expires))}</b>.") + P("Renueva ahora y sumamos el tiempo nuevo al que te queda: no pierdes ningún día.", "0"),
        "Renovar mi tarjeta", link,
        P("Si no renuevas, tu tarjeta dejará de mostrarse al vencer, pero tus datos se conservan.", "0"),
        icon: "&#9203;");

    public static string Expired(string fullName, string cardTitle, string link) => EmailTemplate.Render(
        "Tu tarjeta dejó de mostrarse.",
        "Tu tarjeta venció",
        P($"Hola <b>{E(fullName)}</b>, el periodo de <b>{E(cardTitle)}</b> terminó y su enlace ya no se muestra.") + P("Tus datos siguen guardados. Renueva para volver a publicarla con el mismo enlace.", "0"),
        "Reactivar mi tarjeta", link,
        P("Si ya no la necesitas, no tienes que hacer nada.", "0"),
        icon: "&#128276;");

    public static string Suspended(string fullName, string what, string link) => EmailTemplate.Render(
        $"Suspendimos {what}.",
        $"Suspendimos {what}",
        P($"Hola <b>{E(fullName)}</b>, por ahora {E(what)} está suspendida y no se puede usar.") + P("Esto lo hace nuestro equipo cuando detecta un problema o a petición tuya. Escríbenos y lo revisamos contigo.", "0"),
        "Contactar a soporte", link,
        P("Tus datos no se eliminan.", "0"),
        icon: "&#9208;");

    // 3 · Venta asistida
    public static string AssistedSale(string fullName, string cardTitle, string publicUrl, string loginUrl, bool paid, string? folio) => EmailTemplate.Render(
        $"{cardTitle} ya está creada para ti.",
        paid ? "Tu tarjeta ya está lista" : "Creamos tu tarjeta",
        P($"Hola <b>{E(fullName)}</b>, tu asesor creó <b>{E(cardTitle)}</b> por ti en KARSHA.") +
        P(paid ? $"Tu pago quedó registrado{(folio is null ? "" : $" (recibo <b>{E(folio)}</b>)")} y tu tarjeta ya está publicada." : "Se publica en cuanto se registre tu pago.", "0"),
        paid ? "Ver mi tarjeta" : "Entrar a mi cuenta", paid ? publicUrl : loginUrl,
        P($"Para editarla cuando quieras, entra con este correo en <a href=\"{E(loginUrl)}\" style=\"color:#0369a1\">{E(loginUrl)}</a> y usa «Olvidé mi contraseña» para crear la tuya.", "0"),
        icon: "&#129309;");

    /// <summary>Lista para el panel: cada correo con su asunto, cuándo se manda y el HTML con datos de ejemplo.</summary>
    public static IEnumerable<object> Samples(string publicUrl)
    {
        var now = DateTime.UtcNow;
        var card = "Despacho Pérez";
        var link = publicUrl + "/ejemplo";
        object S(string key, string group, string name, string subject, string trigger, string html) => new { key, group, name, subject, trigger, html };
        return
        [
            S("verificacion", "Cuenta", "Confirmar correo", VerificationSubject, "Cuando alguien crea su cuenta, o pide que le reenviemos el enlace.", Verification("María Pérez", link)),
            S("recuperar", "Cuenta", "Recuperar contraseña", PasswordResetSubject, "Cuando alguien toca «Olvidé mi contraseña».", PasswordReset("María Pérez", link)),
            S("cuenta-creada", "Cuenta", "Cuenta creada por el equipo", AccountCreatedSubject, "Cuando el Admin da de alta a alguien con correo y elige avisarle.", AccountCreated("María Pérez", link)),
            S("bienvenida", "Cuenta", "Bienvenida", WelcomeSubject, "Al confirmar su correo por primera vez.", Welcome("María Pérez", link)),
            S("verificado-equipo", "Cuenta", "Correo verificado por el equipo", VerifiedByTeamSubject, "Cuando el Admin verifica a mano el correo de alguien.", VerifiedByTeam("María Pérez", publicUrl + "/login", publicUrl + "/olvide-contrasena")),
            S("seguridad", "Cuenta", "Aviso de seguridad", SecuritySubject, "Cuando cambia su contraseña o su correo.", SecurityNotice("María Pérez", "contraseña", now, link)),
            S("recibo", "Pagos", "Recibo de pago", "Recibo KR-2026-000021 · KARSHA", "Cada vez que un pago queda cobrado (en línea o a mano).", Receipt("María Pérez", "KR-2026-000021", card, "Pro", 80000, "Tarjeta", now, now.AddYears(1), link)),
            S("cobro-pendiente", "Pagos", "Pago pendiente (efectivo)", "Tu pago está pendiente · KARSHA", "Al generar una ficha de OXXO o un cobro en efectivo pendiente.", CashPending("María Pérez", card, 80000, "9876 5432 1098 7654", link, now.AddDays(3), link)),
            S("pago-fallido", "Pagos", "Pago fallido o vencido", "No se completó tu pago · KARSHA", "Cuando el cobro falla o la ficha de pago vence.", PaymentFailed("María Pérez", card, 80000, link)),
            S("publicada", "Tarjeta", "Tarjeta publicada", "Tu tarjeta ya está publicada · KARSHA", "La primera vez que se publica una tarjeta.", CardPublished("María Pérez", card, link)),
            S("por-vencer", "Tarjeta", "Tarjeta por vencer", "Tu tarjeta vence pronto · KARSHA", "7 días antes de que venza, y de nuevo 1 día antes.", ExpiringSoon("María Pérez", card, now.AddDays(7), 7, link)),
            S("vencida", "Tarjeta", "Tarjeta vencida", "Tu tarjeta venció · KARSHA", "Cuando termina el periodo pagado.", Expired("María Pérez", card, link)),
            S("suspendida", "Tarjeta", "Cuenta o tarjeta suspendida", "Suspendimos tu tarjeta · KARSHA", "Cuando el Admin suspende una tarjeta o desactiva una cuenta.", Suspended("María Pérez", "tu tarjeta " + card, link)),
            S("venta-asistida", "Venta asistida", "Tarjeta creada por tu asesor", "Tu tarjeta ya está lista · KARSHA", "En una venta asistida, si el cliente tiene correo propio.", AssistedSale("María Pérez", card, link, publicUrl + "/login", true, "KR-2026-000021")),
        ];
    }
}
