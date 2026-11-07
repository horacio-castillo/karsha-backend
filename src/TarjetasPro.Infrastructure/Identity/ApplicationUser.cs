using Microsoft.AspNetCore.Identity;

namespace TarjetasPro.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>Una cuenta desactivada no puede iniciar sesión ni renovar su sesión, y sus tarjetas dejan de ser públicas.</summary>
    public bool IsActive { get; set; } = true;
}

public class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole() { }
    public ApplicationRole(string name) : base(name) { }
}
