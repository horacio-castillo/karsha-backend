using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Microsoft.Extensions.Options;

namespace TarjetasPro.Infrastructure.Storage;

public record StorageInfo(bool Configured, long LimitBytes, long UsedBytes, long DriveBytes, long TrashBytes,
    double Percent, string Level, string? Error);

public class DriveStorageService(IOptions<DriveOptions> options)
{
    private readonly DriveOptions _o = options.Value;

    public async Task<StorageInfo> GetAsync(CancellationToken ct)
    {
        GoogleCredential cred;
        try
        {
            if (!string.IsNullOrWhiteSpace(_o.ServiceAccountJson))
                cred = CredentialFactory.FromJson<ServiceAccountCredential>(_o.ServiceAccountJson).ToGoogleCredential();
            else if (!string.IsNullOrWhiteSpace(_o.CredentialsPath) && File.Exists(_o.CredentialsPath))
                cred = CredentialFactory.FromFile<ServiceAccountCredential>(_o.CredentialsPath).ToGoogleCredential();
            else
                return new StorageInfo(false, 0, 0, 0, 0, 0, "unconfigured", null);

            using var drive = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = cred.CreateScoped(DriveService.Scope.DriveMetadataReadonly),
                ApplicationName = "TarjetasPro",
            });
            var req = drive.About.Get();
            req.Fields = "storageQuota";
            var q = (await req.ExecuteAsync(ct)).StorageQuota;

            var used = q.Usage ?? 0;
            var limit = q.Limit ?? 0; // null = almacenamiento ilimitado (Workspace)
            var percent = limit > 0 ? Math.Round(used * 100.0 / limit, 1) : 0;
            var level = limit == 0 ? "unlimited"
                : percent >= _o.CriticalPercent ? "critical"
                : percent >= _o.WarnPercent ? "warning" : "ok";
            return new StorageInfo(true, limit, used, q.UsageInDrive ?? 0, q.UsageInDriveTrash ?? 0, percent, level, null);
        }
        catch (Exception ex)
        {
            return new StorageInfo(true, 0, 0, 0, 0, 0, "error", ex.Message);
        }
    }
}
