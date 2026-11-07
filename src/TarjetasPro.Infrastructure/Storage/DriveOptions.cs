namespace TarjetasPro.Infrastructure.Storage;

public class DriveOptions
{
    public const string Section = "GoogleDrive";

    /// <summary>Ruta al JSON de la cuenta de servicio. Alternativa: <see cref="ServiceAccountJson"/> (user-secrets).</summary>
    public string CredentialsPath { get; set; } = "";
    public string ServiceAccountJson { get; set; } = "";

    /// <summary>Porcentaje de uso a partir del cual se muestra "se está llenando".</summary>
    public int WarnPercent { get; set; } = 80;
    public int CriticalPercent { get; set; } = 95;
}
