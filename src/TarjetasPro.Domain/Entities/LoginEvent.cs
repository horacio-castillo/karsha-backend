namespace TarjetasPro.Domain.Entities;

/// <summary>Un inicio de sesión exitoso y con qué método se hizo (para saber qué usa más la gente).</summary>
public class LoginEvent
{
    public const string Email = "Correo";
    public const string Google = "Google";
    public const string Facebook = "Facebook";

    public long Id { get; set; }
    public Guid UserId { get; set; }
    /// <summary>"Correo", "Google" o "Facebook".</summary>
    public string Method { get; set; } = Email;
    public DateTime At { get; set; } = DateTime.UtcNow;
}
