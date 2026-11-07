namespace TarjetasPro.Application.Auth;

public interface IAuthService
{
    /// <summary>Crea la cuenta sin sesión: queda pendiente de verificar el correo.</summary>
    Task<MessageResult> RegisterAsync(RegisterRequest request);
    Task<MessageResult> ConfirmEmailAsync(ConfirmEmailRequest request);
    Task<MessageResult> ResendConfirmationAsync(string email);
    Task<AuthResult> LoginAsync(LoginRequest request);
    Task<AuthResult> ExternalLoginAsync(ExternalLoginRequest request);
    AuthProviders GetProviders();
    Task<AuthResult> RefreshAsync(string refreshToken);
    Task LogoutAsync(string refreshToken);
    Task<UserDto?> GetUserAsync(Guid userId);
}
