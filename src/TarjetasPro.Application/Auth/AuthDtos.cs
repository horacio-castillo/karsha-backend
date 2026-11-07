namespace TarjetasPro.Application.Auth;

public record RegisterRequest(string Email, string Password, string FullName);
public record LoginRequest(string Email, string Password);
public record RefreshRequest(string RefreshToken);
public record ConfirmEmailRequest(Guid UserId, string Token);
public record ResendConfirmationRequest(string Email);

public record MessageResult(string? Message, string[] Errors)
{
    public bool Ok => Message is not null;
    public static MessageResult Success(string message) => new(message, []);
    public static MessageResult Fail(params string[] errors) => new(null, errors);
}
/// <summary>Provider: "Google" o "Facebook". Token: id_token de Google o access_token de Facebook.</summary>
public record ExternalLoginRequest(string Provider, string Token);
public record AuthProviders(string? GoogleClientId, string? FacebookAppId);

public record UserDto(Guid Id, string Email, string FullName, string[] Roles, string[] Permissions);
public record AuthResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt, UserDto User);

public record AuthResult(AuthResponse? Data, string[] Errors)
{
    public bool Ok => Data is not null;
    public static AuthResult Success(AuthResponse data) => new(data, []);
    public static AuthResult Fail(params string[] errors) => new(null, errors);
}
