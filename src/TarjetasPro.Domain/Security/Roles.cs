namespace TarjetasPro.Domain.Security;

public static class Roles
{
    /// <summary>Dueño de la plataforma: todo, incluidos roles y permisos. Su rol no se puede editar.</summary>
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string Cliente = "Cliente";

    /// <summary>Roles que vienen con el sistema: no se pueden borrar.</summary>
    public static readonly string[] System = [SuperAdmin, Admin, Cliente];

    /// <summary>Roles con acceso al panel: solo quien tiene «roles.manage» puede darlos o quitarlos.</summary>
    public static bool IsPrivileged(string? role) => role is SuperAdmin or Admin;
}
