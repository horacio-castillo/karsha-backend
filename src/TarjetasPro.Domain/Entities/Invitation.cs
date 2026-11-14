namespace TarjetasPro.Domain.Entities;

public enum InvitationStatus { Draft, Published, Closed }
public enum GuestStatus { Pending, Confirmed, Declined }

/// <summary>Invitación a un evento (boda, XV años…). Es un producto aparte de las tarjetas de presentación.</summary>
public class Invitation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid TemplateId { get; set; }
    public Template? Template { get; set; }

    /// <summary>Nombre interno para que el anfitrión distinga sus invitaciones.</summary>
    public string Title { get; set; } = "";
    /// <summary>Se publica en /i/{slug}; cada invitado entra con su token personal: /i/{slug}?g={token}.</summary>
    public string Slug { get; set; } = "";
    /// <summary>JSON {campo: valor} con los datos del evento (se sanea al renderizar).</summary>
    public string DataJson { get; set; } = "{}";
    public InvitationStatus Status { get; set; } = InvitationStatus.Draft;
    /// <summary>Fecha del evento; alimenta la cuenta regresiva del panel.</summary>
    public DateTime? EventDate { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<Guest> Guests { get; set; } = [];
}

/// <summary>Persona (o familia) invitada, con su enlace personal y su respuesta.</summary>
public class Guest
{
    public const int MaxPassesLimit = 20;

    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InvitationId { get; set; }
    public string Name { get; set; } = "";
    /// <summary>Solo dígitos con lada de país (ej. 5215512345678), para abrir WhatsApp.</summary>
    public string Phone { get; set; } = "";
    /// <summary>Pases que el anfitrión le asignó.</summary>
    public int MaxPasses { get; set; } = 1;
    /// <summary>Código aleatorio no adivinable que va en su enlace.</summary>
    public string Token { get; set; } = "";
    public GuestStatus Status { get; set; } = GuestStatus.Pending;
    public int ConfirmedPasses { get; set; }
    public string? Message { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? OpenedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
