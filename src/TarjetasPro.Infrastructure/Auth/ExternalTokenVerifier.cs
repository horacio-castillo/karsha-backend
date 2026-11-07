using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace TarjetasPro.Infrastructure.Auth;

public class ExternalAuthOptions
{
    public const string Section = "ExternalAuth";
    public GoogleOptions Google { get; set; } = new();
    public FacebookOptions Facebook { get; set; } = new();

    public class GoogleOptions { public string ClientId { get; set; } = ""; }
    public class FacebookOptions { public string AppId { get; set; } = ""; public string AppSecret { get; set; } = ""; }
}

public record ExternalIdentity(string Provider, string Subject, string Email, string Name);

/// <summary>Valida en el servidor del proveedor el token que obtuvo el navegador; nunca se confía en datos enviados por el cliente.</summary>
public class ExternalTokenVerifier(HttpClient http, IOptions<ExternalAuthOptions> options)
{
    public const string Google = "Google";
    public const string Facebook = "Facebook";
    private readonly ExternalAuthOptions _opt = options.Value;

    public bool IsConfigured(string provider) => provider switch
    {
        Google => _opt.Google.ClientId.Length > 0,
        Facebook => _opt.Facebook.AppId.Length > 0 && _opt.Facebook.AppSecret.Length > 0,
        _ => false,
    };

    public async Task<ExternalIdentity?> VerifyAsync(string provider, string token, CancellationToken ct = default)
    {
        if (!IsConfigured(provider) || string.IsNullOrWhiteSpace(token)) return null;
        try
        {
            return provider switch
            {
                Google => await VerifyGoogleAsync(token, ct),
                Facebook => await VerifyFacebookAsync(token, ct),
                _ => null,
            };
        }
        catch (HttpRequestException) { return null; }
    }

    private async Task<ExternalIdentity?> VerifyGoogleAsync(string idToken, CancellationToken ct)
    {
        var res = await http.GetAsync($"https://oauth2.googleapis.com/tokeninfo?id_token={Uri.EscapeDataString(idToken)}", ct);
        if (!res.IsSuccessStatusCode) return null;
        var info = await res.Content.ReadFromJsonAsync<GoogleInfo>(ct);
        if (info is null || info.Aud != _opt.Google.ClientId || string.IsNullOrEmpty(info.Sub)
            || string.IsNullOrEmpty(info.Email) || info.EmailVerified != "true") return null;
        return new(Google, info.Sub, info.Email, info.Name ?? info.Email);
    }

    private async Task<ExternalIdentity?> VerifyFacebookAsync(string accessToken, CancellationToken ct)
    {
        var appToken = $"{_opt.Facebook.AppId}|{_opt.Facebook.AppSecret}";
        var debug = await http.GetFromJsonAsync<FbDebug>(
            $"https://graph.facebook.com/debug_token?input_token={Uri.EscapeDataString(accessToken)}&access_token={Uri.EscapeDataString(appToken)}", ct);
        if (debug?.Data is not { IsValid: true } d || d.AppId != _opt.Facebook.AppId) return null;

        var me = await http.GetFromJsonAsync<FbMe>(
            $"https://graph.facebook.com/me?fields=id,name,email&access_token={Uri.EscapeDataString(accessToken)}", ct);
        if (me is null || string.IsNullOrEmpty(me.Id) || string.IsNullOrEmpty(me.Email)) return null;
        return new(Facebook, me.Id, me.Email, me.Name ?? me.Email);
    }

    private record GoogleInfo(
        [property: JsonPropertyName("aud")] string? Aud,
        [property: JsonPropertyName("sub")] string? Sub,
        [property: JsonPropertyName("email")] string? Email,
        [property: JsonPropertyName("email_verified")] string? EmailVerified,
        [property: JsonPropertyName("name")] string? Name);

    private record FbDebug([property: JsonPropertyName("data")] FbDebugData? Data);
    private record FbDebugData(
        [property: JsonPropertyName("app_id")] string? AppId,
        [property: JsonPropertyName("is_valid")] bool IsValid);
    private record FbMe(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("email")] string? Email);
}
