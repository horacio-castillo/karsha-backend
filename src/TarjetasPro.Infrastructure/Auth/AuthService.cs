using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
    IConfiguration config) : IAuthService
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
        await SendConfirmationAsync(user);
        return MessageResult.Success($"Te enviamos un correo a {address}. Abre el enlace para activar tu cuenta.");
    }

    public async Task<MessageResult> ConfirmEmailAsync(ConfirmEmailRequest request)
    {
        const string invalid = "El enlace no es válido o ya venció. Pide uno nuevo desde «Iniciar sesión».";
        var user = await users.FindByIdAsync(request.UserId.ToString());
        if (user is null || string.IsNullOrWhiteSpace(request.Token)) return MessageResult.Fail(invalid);
        if (user.EmailConfirmed) return MessageResult.Success("Tu correo ya estaba verificado. Ya puedes iniciar sesión.");
        var result = await users.ConfirmEmailAsync(user, request.Token);
        return result.Succeeded ? MessageResult.Success("¡Correo verificado! Ya puedes iniciar sesión.") : MessageResult.Fail(invalid);
    }

    /// <summary>Respuesta siempre igual, exista o no el correo, para no revelar qué cuentas hay.</summary>
    public async Task<MessageResult> ResendConfirmationAsync(string address)
    {
        var user = await users.FindByEmailAsync(address.Trim());
        if (user is { EmailConfirmed: false, IsActive: true }) await SendConfirmationAsync(user);
        return MessageResult.Success("Si el correo está pendiente de verificar, te enviamos un nuevo enlace.");
    }

    private async Task SendConfirmationAsync(ApplicationUser user)
    {
        var token = await users.GenerateEmailConfirmationTokenAsync(user);
        var link = $"{PublicUrl}/verificar-correo?userId={user.Id}&token={Uri.EscapeDataString(token)}";
        var name = WebUtility.HtmlEncode(user.FullName);
        var html = $"""
            <div style="font-family:system-ui,sans-serif;max-width:480px;margin:auto;color:#1f2937">
              <h2>Verifica tu correo</h2>
              <p>Hola {name}, gracias por registrarte en Tarjetas Pro.</p>
              <p><a href="{WebUtility.HtmlEncode(link)}" style="display:inline-block;padding:12px 20px;border-radius:10px;background:#2563eb;color:#fff;text-decoration:none;font-weight:600">Verificar mi correo</a></p>
              <p style="color:#6b7280;font-size:.9rem">El enlace vence en 24 horas. Si no creaste esta cuenta, ignora este mensaje.</p>
            </div>
            """;
        await email.SendAsync(user.Email!, "Verifica tu correo · Tarjetas Pro", html);
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
        return AuthResult.Success(await IssueAsync(user));
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
        return AuthResult.Success(await IssueAsync(user));
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
        return new UserDto(user.Id, user.Email!, user.FullName, userRoles, perms.Order().ToArray());
    }

    private async Task<AuthResponse> IssueAsync(ApplicationUser user)
    {
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
