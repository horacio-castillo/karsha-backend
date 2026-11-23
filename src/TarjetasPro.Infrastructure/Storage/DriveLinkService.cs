using System.Text;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DriveFile = Google.Apis.Drive.v3.Data.File;

namespace TarjetasPro.Infrastructure.Storage;

public record DriveStatus(bool HasClient, bool HasSecret, bool Linked, string? Email, DateTime? LinkedAt, string? ClientId);

/// <summary>
/// Vínculo de la plataforma con la cuenta de Google Drive del Admin (OAuth) y reglas de carpetas.
/// Regla: KARSHA / Clientes / «Nombre (id corto)» — una carpeta por cliente, identificada por su id, no por su nombre.
/// Solo pide el permiso «drive.file»: la plataforma ve únicamente lo que ella misma crea, nada más de tu Drive.
/// </summary>
public class DriveLinkService(AppDbContext db, IDataProtectionProvider dp, ILogger<DriveLinkService> logger)
{
    public static readonly string Scope = DriveService.Scope.DriveFile;
    private const string App = "KARSHA";
    private static readonly string[] Scopes = ["openid", "email", Scope];

    private const string KClientId = "drive.clientId", KSecret = "drive.clientSecret", KRefresh = "drive.refreshToken",
        KEmail = "drive.email", KLinkedAt = "drive.linkedAt", KRoot = "drive.rootFolderId";

    private IDataProtector Protector => dp.CreateProtector("KARSHA.Drive.v1");
    public ITimeLimitedDataProtector StateProtector => dp.CreateProtector("KARSHA.Drive.State.v1").ToTimeLimitedDataProtector();

    private async Task<string?> GetAsync(string key) => await db.AppSettings.AsNoTracking().Where(a => a.Key == key).Select(a => a.Value).FirstOrDefaultAsync();

    private async Task SetAsync(string key, string? value)
    {
        var row = await db.AppSettings.FirstOrDefaultAsync(a => a.Key == key);
        if (value is null) { if (row is not null) db.AppSettings.Remove(row); }
        else if (row is null) db.AppSettings.Add(new Domain.Entities.AppSetting { Key = key, Value = value });
        else row.Value = value;
        await db.SaveChangesAsync();
    }

    private async Task<string?> GetSecretAsync(string key)
    {
        var raw = await GetAsync(key);
        if (string.IsNullOrEmpty(raw)) return null;
        try { return Protector.Unprotect(raw); } catch { return null; } // llaves cambiadas: hay que volver a vincular
    }

    public async Task<DriveStatus> StatusAsync()
    {
        var id = await GetAsync(KClientId);
        var hasSecret = !string.IsNullOrEmpty(await GetAsync(KSecret));
        var linked = await GetSecretAsync(KRefresh) is not null;
        DateTime? at = DateTime.TryParse(await GetAsync(KLinkedAt), null, System.Globalization.DateTimeStyles.RoundtripKind, out var d) ? d : null;
        return new DriveStatus(!string.IsNullOrEmpty(id), hasSecret, linked, linked ? await GetAsync(KEmail) : null, linked ? at : null, id);
    }

    public async Task<bool> IsLinkedAsync() => await GetSecretAsync(KRefresh) is not null;

    /// <summary>Guarda el ID de cliente y el secreto. Un secreto vacío conserva el anterior. Cambiar de cliente OAuth desvincula.</summary>
    public async Task SaveClientAsync(string clientId, string? clientSecret)
    {
        var old = await GetAsync(KClientId);
        await SetAsync(KClientId, clientId.Trim());
        if (!string.IsNullOrWhiteSpace(clientSecret)) await SetAsync(KSecret, Protector.Protect(clientSecret.Trim()));
        if (old != clientId.Trim() || !string.IsNullOrWhiteSpace(clientSecret)) await ClearLinkAsync();
    }

    private async Task ClearLinkAsync()
    {
        foreach (var k in new[] { KRefresh, KEmail, KLinkedAt, KRoot }) await SetAsync(k, null);
    }

    public async Task UnlinkAsync()
    {
        await ClearLinkAsync();
        // Las carpetas de los clientes se recrean si se vuelve a vincular otra cuenta.
        var keys = await db.AppSettings.Where(a => a.Key.StartsWith("drive.folder.")).ToListAsync();
        db.AppSettings.RemoveRange(keys);
        await db.SaveChangesAsync();
    }

    private async Task<GoogleAuthorizationCodeFlow?> FlowAsync()
    {
        var id = await GetAsync(KClientId);
        var secret = await GetSecretAsync(KSecret);
        if (string.IsNullOrEmpty(id) || secret is null) return null;
        return new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets { ClientId = id, ClientSecret = secret },
            Scopes = Scopes,
        });
    }

    public async Task<string?> BuildAuthUrlAsync(string redirectUri, string state)
    {
        var id = await GetAsync(KClientId);
        if (string.IsNullOrEmpty(id)) return null;
        var url = new GoogleAuthorizationCodeRequestUrl(new Uri(GoogleAuthConsts.OidcAuthorizationUrl))
        {
            ClientId = id, RedirectUri = redirectUri, Scope = string.Join(' ', Scopes), State = state,
            AccessType = "offline", Prompt = "consent", // consent: Google siempre devuelve el token de larga duración
        };
        return url.Build().AbsoluteUri;
    }

    /// <summary>Cambia el código de Google por el token de larga duración y lo guarda cifrado. Devuelve la cuenta vinculada.</summary>
    public async Task<string> CompleteAsync(string code, string redirectUri, CancellationToken ct = default)
    {
        var flow = await FlowAsync() ?? throw new InvalidOperationException("Primero guarda el ID de cliente y el secreto.");
        var token = await flow.ExchangeCodeForTokenAsync("admin", code, redirectUri, ct);
        if (string.IsNullOrEmpty(token.RefreshToken))
            throw new InvalidOperationException("Google no devolvió el permiso permanente. Quita el acceso de KARSHA en tu cuenta de Google y vuelve a vincular.");
        var email = EmailFromIdToken(token.IdToken) ?? "cuenta de Google";
        await SetAsync(KRefresh, Protector.Protect(token.RefreshToken));
        await SetAsync(KEmail, email);
        await SetAsync(KLinkedAt, DateTime.UtcNow.ToString("O"));
        await SetAsync(KRoot, null);
        return email;
    }

    private static string? EmailFromIdToken(string? jwt)
    {
        try
        {
            var part = jwt?.Split('.')[1]; if (part is null) return null;
            part = part.Replace('-', '+').Replace('_', '/'); part = part.PadRight(part.Length + (4 - part.Length % 4) % 4, '=');
            using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(part)));
            return doc.RootElement.TryGetProperty("email", out var e) ? e.GetString() : null;
        }
        catch { return null; }
    }

    /// <summary>Cliente de Drive con el permiso del Admin, o null si no hay vínculo.</summary>
    public async Task<DriveService?> CreateAsync()
    {
        var flow = await FlowAsync();
        var refresh = await GetSecretAsync(KRefresh);
        if (flow is null || refresh is null) return null;
        var cred = new UserCredential(flow, "admin", new TokenResponse { RefreshToken = refresh });
        return new DriveService(new BaseClientService.Initializer { HttpClientInitializer = cred, ApplicationName = App });
    }

    private static string Esc(string s) => s.Replace("\\", "\\\\").Replace("'", "\\'");

    private async Task<string> EnsureFolderAsync(DriveService drive, string name, string? parent, CancellationToken ct)
    {
        var list = drive.Files.List();
        list.Q = $"mimeType='application/vnd.google-apps.folder' and trashed=false and name='{Esc(name)}'" + (parent is null ? " and 'root' in parents" : $" and '{parent}' in parents");
        list.Fields = "files(id)"; list.PageSize = 1;
        var found = (await list.ExecuteAsync(ct)).Files.FirstOrDefault();
        if (found is not null) return found.Id;
        var req = drive.Files.Create(new DriveFile { Name = name, MimeType = "application/vnd.google-apps.folder", Parents = parent is null ? null : [parent] });
        req.Fields = "id";
        return (await req.ExecuteAsync(ct)).Id;
    }

    /// <summary>KARSHA / Clientes (se crea la primera vez).</summary>
    private async Task<string> ClientsFolderAsync(DriveService drive, CancellationToken ct)
    {
        if (await GetAsync(KRoot) is { Length: > 0 } cached) return cached;
        var root = await EnsureFolderAsync(drive, "KARSHA", null, ct);
        var clients = await EnsureFolderAsync(drive, "Clientes", root, ct);
        await SetAsync(KRoot, clients);
        return clients;
    }

    private static string Clean(string s)
    {
        var bad = Path.GetInvalidFileNameChars().Concat(['/', '\\', '\'', '"']).ToHashSet();
        var t = new string(s.Where(c => !bad.Contains(c)).ToArray()).Trim();
        return t.Length == 0 ? "Cliente" : t[..Math.Min(t.Length, 60)];
    }

    /// <summary>La carpeta del cliente: una sola, siempre la misma aunque cambie de nombre.</summary>
    public async Task<string> UserFolderAsync(DriveService drive, Guid userId, string fullName, CancellationToken ct = default)
    {
        var key = $"drive.folder.{userId:N}";
        if (await GetAsync(key) is { Length: > 0 } known) return known;
        var parent = await ClientsFolderAsync(drive, ct);
        var id = await EnsureFolderAsync(drive, $"{Clean(fullName)} ({userId.ToString("N")[..6]})", parent, ct);
        await SetAsync(key, id);
        return id;
    }

    /// <summary>Sube el archivo a la carpeta del cliente. Devuelve el id del archivo en Drive.</summary>
    public async Task<string?> UploadAsync(Guid userId, string fullName, string localPath, string fileName, string contentType, CancellationToken ct = default)
    {
        try
        {
            using var drive = await CreateAsync();
            if (drive is null) return null;
            for (var attempt = 0; attempt < 2; attempt++)
            {
                var folder = await UserFolderAsync(drive, userId, fullName, ct);
                await using var fs = File.OpenRead(localPath);
                var req = drive.Files.Create(new DriveFile { Name = fileName, Parents = [folder] }, fs, contentType);
                req.Fields = "id";
                var res = await req.UploadAsync(ct);
                if (res.Status == Google.Apis.Upload.UploadStatus.Completed) return req.ResponseBody.Id;
                // Carpeta borrada a mano: se olvida y se recrea una vez.
                if (attempt == 0 && res.Exception is Google.GoogleApiException { HttpStatusCode: System.Net.HttpStatusCode.NotFound })
                { await SetAsync($"drive.folder.{userId:N}", null); await SetAsync(KRoot, null); continue; }
                logger.LogWarning(res.Exception, "No se pudo subir {File} a Drive", fileName);
                return null;
            }
            return null;
        }
        catch (Exception ex) { logger.LogWarning(ex, "Drive no disponible; el archivo se queda en el servidor"); return null; }
    }

    /// <summary>Abre un archivo para entregarlo. Solo existen los que la plataforma creó (permiso drive.file).</summary>
    public async Task<(Stream Stream, string ContentType)?> OpenAsync(string driveFileId, CancellationToken ct = default)
    {
        var drive = await CreateAsync();
        if (drive is null) return null;
        try
        {
            var meta = drive.Files.Get(driveFileId); meta.Fields = "mimeType,trashed";
            var m = await meta.ExecuteAsync(ct);
            if (m.Trashed == true || m.MimeType is null || !m.MimeType.StartsWith("image/")) { drive.Dispose(); return null; }
            var ms = new MemoryStream();
            await drive.Files.Get(driveFileId).DownloadAsync(ms, ct);
            ms.Position = 0; drive.Dispose();
            return (ms, m.MimeType);
        }
        catch { drive.Dispose(); return null; }
    }
}
