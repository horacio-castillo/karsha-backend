using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TarjetasPro.Application.Auth;
using TarjetasPro.Domain.Entities;
using TarjetasPro.Domain.Security;
using TarjetasPro.Infrastructure.Identity;

namespace TarjetasPro.Infrastructure.Auth;

public class AuthService(
    UserManager<ApplicationUser> users,
    RoleManager<ApplicationRole> roles,
    AppDbContext db,
    IOptions<JwtOptions> jwtOptions,
    ExternalTokenVerifier verifier,
    IOptions<ExternalAuthOptions> externalOptions,
    IEmailSender email,
    IConfiguration config,
    Microsoft.Extensions.Logging.ILogger<AuthService> logger) : IAuthService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    private const string Unverified = "Debes verificar tu correo antes de entrar. Revisa tu bandeja (y el spam) o pide un nuevo enlace.";
    private string PublicUrl => (config["App:PublicUrl"] ?? "http://localhost:4200").TrimEnd('/');

    public async Task<MessageResult> RegisterAsync(RegisterRequest request)
    {
        var address = request.Email.Trim();

        // Una cuenta sin verificar no ha podido entrar nunca: si alguien se registró con un correo ajeno,
        // el dueño real puede registrarse de nuevo y reemplazarla.
        var stale = await users.FindByEmailAsync(address);
        if (stale is { EmailConfirmed: false } && (await users.GetLoginsAsync(stale)).Count == 0)
            await users.DeleteAsync(stale);

        var user = new ApplicationUser
        {
            UserName = address,
            Email = address,
            FullName = request.FullName.Trim(),
        };
        var created = await users.CreateAsync(user, request.Password);
        if (!created.Succeeded)
            return MessageResult.Fail(created.Errors.Select(e => e.Description).ToArray());

        await users.AddToRoleAsync(user, Roles.Cliente);
        try { await SendConfirmationAsync(user); }
        catch (Exception ex)
        {
            // La cuenta ya existe: un fallo del correo no debe verse como error de registro.
            logger.LogError(ex, "No se pudo enviar el correo de verificación a {Email}", address);
            return MessageResult.Success($"Creamos tu cuenta, pero no pudimos enviar el correo a {address}. Toca «Reenviar» en un momento.");
        }
        return MessageResult.Success($"Te enviamos un correo a {address}. Abre el enlace para activar tu cuenta.");
    }

    public async Task<MessageResult> ConfirmEmailAsync(ConfirmEmailRequest request)
    {
        const string invalid = "El enlace no es válido o ya venció. Pide uno nuevo desde «Iniciar sesión».";
        var user = await users.FindByIdAsync(request.UserId.ToString());
        if (user is null || string.IsNullOrWhiteSpace(request.Token)) return MessageResult.Fail(invalid);
        if (user.EmailConfirmed) return MessageResult.Success("Tu correo ya estaba verificado. Ya puedes iniciar sesión.");
        var result = await users.ConfirmEmailAsync(user, request.Token);
        if (!result.Succeeded) return MessageResult.Fail(invalid);
        try { await email.SendAsync(user.Email!, EmailCatalog.WelcomeSubject, EmailCatalog.Welcome(user.FullName, $"{PublicUrl}/login")); }
        catch (Exception ex) { logger.LogError(ex, "No se pudo enviar la bienvenida a {Email}", user.Email); }
        // El enlace es de un solo uso y llegó al correo de la persona: vale como prueba de que es ella, así que entra directo.
        if (user.IsActive)
            return MessageResult.Success("¡Correo verificado! Ya puedes usar tu cuenta.", await IssueAsync(user, LoginEvent.Email));
        return MessageResult.Success("¡Correo verificado! Ya puedes iniciar sesión.");
    }

    /// <summary>Respuesta siempre igual, exista o no el correo, para no revelar qué cuentas hay.</summary>
    public async Task<MessageResult> ResendConfirmationAsync(string address)
    {
        var user = await users.FindByEmailAsync(address.Trim());
        if (user is { EmailConfirmed: false, IsActive: true })
        {
            try { await SendConfirmationAsync(user); }
            catch (Exception ex) { logger.LogError(ex, "No se pudo reenviar la verificación a {Email}", address); }
        }
        return MessageResult.Success("Si el correo está pendiente de verificar, te enviamos un nuevo enlace.");
    }

    public async Task<MessageResult> ForgotPasswordAsync(string address)
    {
        const string same = "Si el correo tiene una cuenta, te enviamos un enlace para crear una contraseña nueva.";
        var user = await users.FindByEmailAsync((address ?? "").Trim());
        if (user is { IsActive: true })
        {
            try
            {
                var token = await users.GeneratePasswordResetTokenAsync(user);
                var link = $"{PublicUrl}/restablecer-contrasena?userId={user.Id}&token={Uri.EscapeDataString(token)}";
                await email.SendAsync(user.Email!, EmailCatalog.PasswordResetSubject, EmailCatalog.PasswordReset(user.FullName, link));
            }
            catch (Exception ex) { logger.LogError(ex, "No se pudo enviar la recuperación a {Email}", address); }
        }
        return MessageResult.Success(same);
    }

    public async Task<MessageResult> ResetPasswordAsync(ResetPasswordRequest request)
    {
        const string invalid = "El enlace no es válido o ya venció. Pide uno nuevo desde «Olvidé mi contraseña».";
        var user = await users.FindByIdAsync(request.UserId.ToString());
        if (user is not { IsActive: true } || string.IsNullOrWhiteSpace(request.Token)) return MessageResult.Fail(invalid);
        var result = await users.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            var bad = result.Errors.Where(e => e.Code != "InvalidToken").Select(e => e.Description).ToArray();
            return MessageResult.Fail(bad.Length > 0 ? bad : [invalid]);
        }
        // Tener el enlace del correo prueba que el correo es suyo.
        if (!user.EmailConfirmed) { user.EmailConfirmed = true; await users.UpdateAsync(user); }
        // Se cierran las sesiones abiertas: quien tuviera la contraseña anterior ya no entra.
        await db.RefreshTokens.Where(t => t.UserId == user.Id && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, DateTime.UtcNow));
        try { await email.SendAsync(user.Email!, EmailCatalog.SecuritySubject, EmailCatalog.SecurityNotice(user.FullName, "contraseña", DateTime.UtcNow, $"{PublicUrl}/olvide-contrasena")); }
        catch (Exception ex) { logger.LogError(ex, "No se pudo enviar el aviso de seguridad a {Email}", user.Email); }
        return MessageResult.Success("¡Listo! Ya puedes iniciar sesión con tu contraseña nueva.");
    }

    /// <summary>Correo de «tu cuenta está lista»: lo manda el Admin al dar de alta a alguien. El botón lleva a crear su contraseña.</summary>
    public async Task<MessageResult> InviteAsync(Guid userId)
    {
        var user = await users.FindByIdAsync(userId.ToString());
        if (user is null) return MessageResult.Fail("Usuario no encontrado.");
        if (user.Email is null || user.Email.EndsWith("@clientes.karsha.mx", StringComparison.OrdinalIgnoreCase))
            return MessageResult.Fail("Este usuario no tiene correo propio.");
        var token = await users.GeneratePasswordResetTokenAsync(user);
        var link = $"{PublicUrl}/restablecer-contrasena?userId={user.Id}&token={Uri.EscapeDataString(token)}";
        await email.SendAsync(user.Email, EmailCatalog.AccountCreatedSubject, EmailCatalog.AccountCreated(user.FullName, link));
        return MessageResult.Success("Le enviamos el correo para crear su contraseña.");
    }

    private async Task SendConfirmationAsync(ApplicationUser user)
    {
        var token = await users.GenerateEmailConfirmationTokenAsync(user);
        var link = $"{PublicUrl}/verificar-correo?userId={user.Id}&token={Uri.EscapeDataString(token)}";
        var html = EmailCatalog.Verification(user.FullName, link);
        await email.SendAsync(user.Email!, EmailCatalog.VerificationSubject, html);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        const string invalid = "Correo o contraseña incorrectos.";
        var user = await users.FindByEmailAsync(request.Email.Trim());
        if (user is null) return AuthResult.Fail(invalid);

        if (!user.IsActive)
            return AuthResult.Fail("Tu cuenta está desactivada. Contacta al administrador.");

        if (await users.IsLockedOutAsync(user))
            return AuthResult.Fail("Cuenta bloqueada temporalmente. Intenta más tarde.");

        if (!await users.CheckPasswordAsync(user, request.Password))
        {
            await users.AccessFailedAsync(user);
            return AuthResult.Fail(invalid);
        }

        // Se avisa solo con la contraseña correcta, para no revelar qué correos existen.
        if (!user.EmailConfirmed) return AuthResult.Fail(Unverified);

        await users.ResetAccessFailedCountAsync(user);
        return AuthResult.Success(await IssueAsync(user, LoginEvent.Email));
    }

    public AuthProviders GetProviders() => new(
        verifier.IsConfigured(ExternalTokenVerifier.Google) ? externalOptions.Value.Google.ClientId : null,
        verifier.IsConfigured(ExternalTokenVerifier.Facebook) ? externalOptions.Value.Facebook.AppId : null);

    public async Task<AuthResult> ExternalLoginAsync(ExternalLoginRequest request)
    {
        var identity = await verifier.VerifyAsync(request.Provider, request.Token);
        if (identity is null)
            return AuthResult.Fail("No se pudo verificar tu cuenta. Revisa que el correo esté disponible y verificado.");

        // 1) Cuenta ya vinculada a este proveedor. 2) Cuenta existente con el mismo correo (se vincula). 3) Cuenta nueva.
        var user = await users.FindByLoginAsync(identity.Provider, identity.Subject);
        if (user is null)
        {
            user = await users.FindByEmailAsync(identity.Email);
            // El proveedor ya verificó este correo: vale como verificación de la cuenta existente.
            if (user is { EmailConfirmed: false })
            {
                user.EmailConfirmed = true;
                await users.UpdateAsync(user);
            }
            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = identity.Email,
                    Email = identity.Email,
                    EmailConfirmed = true,
                    FullName = identity.Name.Trim(),
                };
                var created = await users.CreateAsync(user);
                if (!created.Succeeded) return AuthResult.Fail(created.Errors.Select(e => e.Description).ToArray());
                await users.AddToRoleAsync(user, Roles.Cliente);
            }
            var linked = await users.AddLoginAsync(user, new UserLoginInfo(identity.Provider, identity.Subject, identity.Provider));
            if (!linked.Succeeded) return AuthResult.Fail(linked.Errors.Select(e => e.Description).ToArray());
        }

        if (!user.IsActive)
            return AuthResult.Fail("Tu cuenta está desactivada. Contacta al administrador.");
        return AuthResult.Success(await IssueAsync(user, identity.Provider));
    }

    public async Task<AuthResult> RefreshAsync(string refreshToken)
    {
        var hash = Hash(refreshToken);
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);
        if (stored is null || !stored.IsActive) return AuthResult.Fail("Sesión expirada.");

        var user = await users.FindByIdAsync(stored.UserId.ToString());
        if (user is null || !user.IsActive) return AuthResult.Fail("Sesión expirada.");

        stored.RevokedAt = DateTime.UtcNow;
        return AuthResult.Success(await IssueAsync(user));
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var hash = Hash(refreshToken);
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);
        if (stored is { RevokedAt: null })
        {
            stored.RevokedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    public async Task<UserDto?> GetUserAsync(Guid userId)
    {
        var user = await users.FindByIdAsync(userId.ToString());
        return user is null ? null : await ToDtoAsync(user);
    }

    private async Task<UserDto> ToDtoAsync(ApplicationUser user)
    {
        var userRoles = (await users.GetRolesAsync(user)).ToArray();
        var perms = new HashSet<string>();
        foreach (var name in userRoles)
        {
            var role = await roles.FindByNameAsync(name);
            if (role is null) continue;
            foreach (var c in await roles.GetClaimsAsync(role))
                if (c.Type == Permissions.ClaimType) perms.Add(c.Value);
        }
        return new UserDto(user.Id, user.Email!, user.FullName, userRoles, perms.Order().ToArray(), user.AvatarUrl);
    }

    /// <param name="loginMethod">Con qué entró (Correo, Google, Facebook). Nulo al renovar la sesión: eso no cuenta como inicio de sesión.</param>
    private async Task<AuthResponse> IssueAsync(ApplicationUser user, string? loginMethod = null)
    {
        await db.Users.Where(u => u.Id == user.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.LastLoginAt, DateTime.UtcNow));
        if (loginMethod is not null) db.LoginEvents.Add(new LoginEvent { UserId = user.Id, Method = loginMethod });
        var dto = await ToDtoAsync(user);
        var expires = DateTime.UtcNow.AddMinutes(_jwt.AccessMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, dto.Email),
            new("name", dto.FullName),
        };
        claims.AddRange(dto.Roles.Select(r => new Claim(ClaimTypes.Role, r)));
        claims.AddRange(dto.Permissions.Select(p => new Claim(Permissions.ClaimType, p)));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _jwt.Issuer,
            Audience = _jwt.Audience,
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key)),
                SecurityAlgorithms.HmacSha256),
        };
        var access = new JsonWebTokenHandler().CreateToken(descriptor);

        var refresh = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = Hash(refresh),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwt.RefreshDays),
        });
        await db.SaveChangesAsync();

        return new AuthResponse(access, refresh, expires, dto);
    }

    private static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
