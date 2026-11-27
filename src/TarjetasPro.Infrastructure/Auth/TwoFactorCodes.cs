using Microsoft.AspNetCore.Identity;
using TarjetasPro.Infrastructure.Identity;

namespace TarjetasPro.Infrastructure.Auth;

/// <summary>Revisa el segundo paso: el código de 6 dígitos de la app o uno de respaldo (que se gasta al usarse).</summary>
public static class TwoFactorCodes
{
    public const int RecoveryCount = 10;

    public static async Task<(bool Ok, bool UsedRecovery)> CheckAsync(UserManager<ApplicationUser> users, ApplicationUser user, string? code)
    {
        var clean = new string((code ?? "").Where(char.IsLetterOrDigit).ToArray());
        if (clean.Length == 6 && clean.All(char.IsAsciiDigit))
            return (await users.VerifyTwoFactorTokenAsync(user, users.Options.Tokens.AuthenticatorTokenProvider, clean), false);
        if (clean.Length is >= 8 and <= 16)
        {
            // Identity guarda los códigos de respaldo como «XXXXX-XXXXX»; se aceptan sin guion o en minúsculas.
            var up = clean.ToUpperInvariant();
            var formatted = up.Length == 10 ? up[..5] + "-" + up[5..] : up;
            return ((await users.RedeemTwoFactorRecoveryCodeAsync(user, formatted)).Succeeded, true);
        }
        return (false, false);
    }

    /// <summary>Llave en grupos de 4 para escribirla a mano si no se puede escanear el QR.</summary>
    public static string Pretty(string key) =>
        string.Join(' ', Enumerable.Range(0, (key.Length + 3) / 4).Select(i => key.Substring(i * 4, Math.Min(4, key.Length - i * 4)))).ToLowerInvariant();

    /// <summary>Enlace que entienden Google Authenticator, Microsoft Authenticator, Authy, 1Password…</summary>
    public static string Uri(string email, string key) =>
        $"otpauth://totp/{System.Uri.EscapeDataString("KARSHA:" + email)}?secret={key}&issuer=KARSHA&digits=6";
}
