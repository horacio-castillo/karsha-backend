namespace TarjetasPro.Domain.Entities;

/// <summary>Bitácora: quién hizo qué en el panel de administración.</summary>
public class AuditLog
{
    public long Id { get; set; }
    public DateTime At { get; set; } = DateTime.UtcNow;
    public Guid? ActorId { get; set; }
    public string ActorName { get; set; } = "";
    /// <summary>Frase en español: «Cambió el rol de un usuario».</summary>
    public string Action { get; set; } = "";
    /// <summary>Área para filtrar: usuarios, tarjetas, pagos, planes, correos, sistema…</summary>
    public string Area { get; set; } = "";
    public string Target { get; set; } = "";
    public string Detail { get; set; } = "";
    public string Ip { get; set; } = "";
}

/// <summary>Error capturado. Los iguales se agrupan (Count) para no llenar la tabla.</summary>
public class ErrorLog
{
    public long Id { get; set; }
    public string Fingerprint { get; set; } = "";
    /// <summary>api, web, job o correo.</summary>
    public string Source { get; set; } = "api";
    public string Message { get; set; } = "";
    public string Stack { get; set; } = "";
    public string Path { get; set; } = "";
    public string Method { get; set; } = "";
    public DateTime FirstAt { get; set; } = DateTime.UtcNow;
    public DateTime LastAt { get; set; } = DateTime.UtcNow;
    public int Count { get; set; } = 1;
    public bool Resolved { get; set; }
    public Guid? LastUserId { get; set; }
    public string UserAgent { get; set; } = "";
}
