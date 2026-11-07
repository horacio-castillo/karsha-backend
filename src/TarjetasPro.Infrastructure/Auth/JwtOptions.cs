namespace TarjetasPro.Infrastructure.Auth;

public class JwtOptions
{
    public const string Section = "Jwt";
    public string Key { get; set; } = "";
    public string Issuer { get; set; } = "TarjetasPro";
    public string Audience { get; set; } = "TarjetasPro";
    public int AccessMinutes { get; set; } = 15;
    public int RefreshDays { get; set; } = 7;
}

public class SeedAdminOptions
{
    public const string Section = "SeedAdmin";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string FullName { get; set; } = "Administrador";
}
