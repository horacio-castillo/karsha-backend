namespace TarjetasPro.Domain.Security;

public static class Permissions
{
    public const string ClaimType = "permission";

    public const string CardsCreate = "cards.create";
    public const string CardsViewOwn = "cards.view.own";
    public const string CardsEditOwn = "cards.edit.own";
    public const string CardsManageAll = "cards.manage.all";
    public const string TemplatesManage = "templates.manage";
    public const string UsersManage = "users.manage";
    public const string RolesManage = "roles.manage";
    /// <summary>Marca en el rol: «este permiso ya se le dio una vez». Evita que la semilla regrese lo que el Super Admin quitó.</summary>
    public const string SeededClaimType = "permission.seeded";

    public static readonly string[] All =
    [
        CardsCreate, CardsViewOwn, CardsEditOwn, CardsManageAll, TemplatesManage, UsersManage, RolesManage
    ];

    /// <summary>Permisos con los que nace cada rol del sistema.</summary>
    public static readonly IReadOnlyDictionary<string, string[]> ByRole = new Dictionary<string, string[]>
    {
        [Roles.SuperAdmin] = All,
        [Roles.Admin] = [CardsCreate, CardsViewOwn, CardsEditOwn, CardsManageAll, TemplatesManage, UsersManage],
        [Roles.Cliente] = [CardsCreate, CardsViewOwn, CardsEditOwn],
    };

    /// <summary>Permisos de administración: el rol Cliente nunca los puede tener.</summary>
    public static readonly string[] AdminOnly = [CardsManageAll, TemplatesManage, UsersManage];

    /// <summary>Permisos que no se pueden dar a un rol desde la pantalla (además del Super Admin, que tiene todo).</summary>
    public static string[] Blocked(string role) => role == Roles.Cliente ? [.. AdminOnly, RolesManage] : [RolesManage];

    public record Info(string Key, string Label, string Description, string Group);

    /// <summary>Nombres en español claro para la pantalla «Roles y permisos».</summary>
    public static readonly Info[] Catalog =
    [
        new(CardsCreate, "Crear tarjetas", "Puede crear sus propias tarjetas, menús y catálogos.", "Clientes"),
        new(CardsViewOwn, "Ver sus tarjetas", "Ve la lista de sus tarjetas y sus estadísticas.", "Clientes"),
        new(CardsEditOwn, "Editar sus tarjetas", "Cambia datos, diseño y estado de sus propias tarjetas.", "Clientes"),
        new(CardsManageAll, "Administrar tarjetas y ventas", "Entra al panel: resumen, todas las tarjetas, pagos y venta asistida.", "Administración"),
        new(TemplatesManage, "Administrar plantillas", "Crea y edita plantillas, giros y diseños.", "Administración"),
        new(UsersManage, "Administrar usuarios", "Ve usuarios, los activa o desactiva y les cambia el rol (sin dar acceso de admin).", "Administración"),
        new(RolesManage, "Roles y permisos", "Crea roles, decide qué puede hacer cada uno y da o quita acceso de admin.", "Seguridad"),
    ];
}
