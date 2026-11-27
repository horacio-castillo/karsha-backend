namespace TarjetasPro.Application.Auth;

public record RegisterRequest(string Email, string Password, string FullName);
public record LoginRequest(string Email, string Password);
public record RefreshRequest(string RefreshToken);
/// <summary>SignupKey: la que recibió el navegador al registrarse. Solo con ella se entra directo al confirmar.</summary>
public record ConfirmEmailRequest(Guid UserId, string Token, string? SignupKey = null);
public record ResendConfirmationRequest(string Email);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(Guid UserId, string Token, string NewPassword);

/// <summary>Session: solo al confirmar por primera vez desde el navegador del registro. SignupKey: solo al registrarse.</summary>
public record MessageResult(string? Message, string[] Errors, AuthResponse? Session = null)
{
    public bool Ok => Message is not null;
    public string? SignupKey { get; init; }
    public static MessageResult Success(string message) => new(message, []);
    public static MessageResult Success(string message, AuthResponse session) => new(message, [], session);
    public static MessageResult Fail(params string[] errors) => new(null, errors);
}
/// <summary>Provider: "Google" o "Facebook". Token: id_token de Google o access_token de Facebook.</summary>
public record ExternalLoginRequest(string Provider, string Token);
public record AuthProviders(string? GoogleClientId, string? FacebookAppId);

public record UserDto(Guid Id, string Email, string FullName, string[] Roles, string[] Permissions, string? AvatarUrl = null);
/// <summary>ViewedBy: nombre del admin cuando es una sesión de «ver como cliente» (solo lectura, sin renovación).</summary>
public record AuthResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt, UserDto User, string? ViewedBy = null);

/// <summary>Segundo paso del inicio de sesión: el pase que dio el primer paso y el código de la app (o uno de respaldo).</summary>
public record TwoFactorLoginRequest(string Ticket, string Code);

/// <summary>TwoFactorTicket: la contraseña (o Google/Facebook) fue correcta y falta el código de la app de autenticación.</summary>
public record AuthResult(AuthResponse? Data, string[] Errors, string? TwoFactorTicket = null)
{
    public bool Ok => Data is not null || TwoFactorTicket is not null;
    public static AuthResult Success(AuthResponse data) => new(data, []);
    public static AuthResult NeedsCode(string ticket) => new(null, [], ticket);
    public static AuthResult Fail(params string[] errors) => new(null, errors);
}
