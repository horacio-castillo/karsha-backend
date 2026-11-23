namespace TarjetasPro.Application.Auth;

public interface IAuthService
{
    /// <summary>Crea la cuenta sin sesión: queda pendiente de verificar el correo.</summary>
    Task<MessageResult> RegisterAsync(RegisterRequest request);
    Task<MessageResult> ConfirmEmailAsync(ConfirmEmailRequest request);
    Task<MessageResult> ResendConfirmationAsync(string email);
    /// <summary>Respuesta siempre igual, exista o no el correo.</summary>
    Task<MessageResult> ForgotPasswordAsync(string email);
    Task<MessageResult> ResetPasswordAsync(ResetPasswordRequest request);
    /// <summary>El Admin da de alta a alguien: se le manda el enlace para crear su contraseña.</summary>
    Task<MessageResult> InviteAsync(Guid userId);
    Task<AuthResult> LoginAsync(LoginRequest request);
    Task<AuthResult> ExternalLoginAsync(ExternalLoginRequest request);
    AuthProviders GetProviders();
    Task<AuthResult> RefreshAsync(string refreshToken);
    Task LogoutAsync(string refreshToken);
    Task<UserDto?> GetUserAsync(Guid userId);
}
