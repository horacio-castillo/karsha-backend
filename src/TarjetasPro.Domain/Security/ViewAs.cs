namespace TarjetasPro.Domain.Security;

/// <summary>
/// «Ver como cliente»: un admin entra a la cuenta de un cliente en solo lectura para ver lo mismo que él.
/// La sesión lleva quién la abrió; con ella la API rechaza todo lo que cambie datos.
/// </summary>
public static class ViewAs
{
    public const string AdminClaim = "view_as";
    public const string AdminNameClaim = "view_as_name";

    /// <summary>Peticiones POST que solo calculan o muestran algo (vistas previas y cotizaciones) y se permiten en solo lectura.</summary>
    public static readonly string[] ReadOnlyPosts =
    [
        "/api/cards/preview", "/api/invitations/preview", "/api/templates/preview", "/api/payments/quote", "/api/auth/logout",
    ];
}
