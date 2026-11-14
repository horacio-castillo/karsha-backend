using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TarjetasPro.Application.Templates;
using TarjetasPro.Domain.Cards;
using TarjetasPro.Domain.Entities;
using TarjetasPro.Domain.Security;
using TarjetasPro.Infrastructure;

namespace TarjetasPro.Api.Controllers;

public record InvitationInput(string? Title, Guid TemplateId, string? Slug, Dictionary<string, string>? Data, DateTime? EventDate);
public record GuestInput(string? Name, string? Phone, int Passes = 1);
public record AddGuestsRequest(GuestInput[]? Guests);

/// <summary>Invitaciones del anfitrión: datos del evento, lista de invitados y panel de respuestas.</summary>
[ApiController]
[Route("api/invitations")]
[Authorize]
public class InvitationsController(AppDbContext db) : ControllerBase
{
    private const int MaxGuests = 500;
    private const int MaxPerRequest = 200;

    private Guid UserId => Guid.Parse(User.FindFirstValue("sub")!);
    private string ApiBase => $"{Request.Scheme}://{Request.Host}";

    // ---------- Invitaciones ----------

    [HttpGet]
    [Authorize(Policy = Permissions.CardsViewOwn)]
    public async Task<IActionResult> List()
    {
        var uid = UserId;
        var rows = await db.Invitations.AsNoTracking().Where(i => i.UserId == uid)
            .OrderByDescending(i => i.UpdatedAt)
            .Select(i => new
            {
                i.Id, i.Title, i.Slug, Status = i.Status.ToString(), i.EventDate, TemplateName = i.Template!.Name,
                Guests = i.Guests.Count,
                Confirmed = i.Guests.Where(g => g.Status == GuestStatus.Confirmed).Sum(g => g.ConfirmedPasses),
                Declined = i.Guests.Count(g => g.Status == GuestStatus.Declined),
                Pending = i.Guests.Count(g => g.Status == GuestStatus.Pending),
            }).ToListAsync();
        return Ok(rows);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.CardsViewOwn)]
    public async Task<IActionResult> Get(Guid id)
    {
        var inv = await Find(id, withTemplate: true);
        return inv is null ? NotFound() : Ok(Detail(inv));
    }

    [HttpGet("slug-check")]
    [Authorize(Policy = Permissions.CardsCreate)]
    public async Task<IActionResult> SlugCheck(string slug, Guid? excludeId)
    {
        var normalized = SlugRules.Normalize(slug);
        if (SlugRules.Validate(normalized) is { } reason) return Ok(new { available = false, slug = normalized, reason });
        var taken = await db.Invitations.IgnoreQueryFilters().AnyAsync(i => i.Slug == normalized && i.Id != excludeId);
        return Ok(new { available = !taken, slug = normalized, reason = taken ? "Ese enlace ya está en uso." : null });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.CardsCreate)]
    public async Task<IActionResult> Create(InvitationInput r)
    {
        var template = await InvitationTemplate(r.TemplateId);
        if (template is null) return Bad("Elige una plantilla de invitación válida.");
        var title = (r.Title ?? "").Trim();
        if (title.Length is < 2 or > 100) return Bad("Ponle un nombre a tu invitación (2 a 100 caracteres).");

        var slug = SlugRules.Normalize(string.IsNullOrWhiteSpace(r.Slug) ? title : r.Slug);
        if (string.IsNullOrWhiteSpace(r.Slug)) slug = await UniqueSlug(slug);
        if (SlugRules.Validate(slug) is { } reason) return Bad(reason);
        if (await db.Invitations.IgnoreQueryFilters().AnyAsync(i => i.Slug == slug)) return Bad("Ese enlace ya está en uso.");

        var inv = new Invitation
        {
            UserId = UserId, TemplateId = template.Id, Title = title, Slug = slug,
            DataJson = JsonSerializer.Serialize(CleanData(r.Data)), EventDate = r.EventDate?.ToUniversalTime(),
        };
        db.Invitations.Add(inv);
        await db.SaveChangesAsync();
        inv.Template = template;
        return Ok(Detail(inv));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.CardsEditOwn)]
    public async Task<IActionResult> Update(Guid id, InvitationInput r)
    {
        var inv = await Find(id, withTemplate: true);
        if (inv is null) return NotFound();
        var title = (r.Title ?? "").Trim();
        if (title.Length is < 2 or > 100) return Bad("Ponle un nombre a tu invitación (2 a 100 caracteres).");
        if (r.TemplateId != inv.TemplateId)
        {
            var template = await InvitationTemplate(r.TemplateId);
            if (template is null) return Bad("Elige una plantilla de invitación válida.");
            inv.TemplateId = template.Id; inv.Template = template;
        }
        if (!string.IsNullOrWhiteSpace(r.Slug))
        {
            var slug = SlugRules.Normalize(r.Slug);
            if (slug != inv.Slug)
            {
                if (SlugRules.Validate(slug) is { } reason) return Bad(reason);
                if (await db.Invitations.IgnoreQueryFilters().AnyAsync(i => i.Slug == slug && i.Id != id)) return Bad("Ese enlace ya está en uso.");
                inv.Slug = slug;
            }
        }
        inv.Title = title;
        inv.DataJson = JsonSerializer.Serialize(CleanData(r.Data));
        inv.EventDate = r.EventDate?.ToUniversalTime();
        inv.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(Detail(inv));
    }

    /// <summary>Publica o pausa el enlace. (El cobro por evento se conecta después.)</summary>
    [HttpPost("{id:guid}/status")]
    [Authorize(Policy = Permissions.CardsEditOwn)]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] StatusRequestDto r)
    {
        var inv = await Find(id);
        if (inv is null) return NotFound();
        if (r.Status is not (InvitationStatus.Draft or InvitationStatus.Published or InvitationStatus.Closed)) return Bad("Estado no válido.");
        inv.Status = r.Status; inv.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(new { status = inv.Status.ToString() });
    }

    /// <summary>Borrado lógico: la invitación se oculta, nunca se elimina de la base.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.CardsEditOwn)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var inv = await Find(id);
        if (inv is null) return NotFound();
        inv.IsDeleted = true; inv.Status = InvitationStatus.Closed; inv.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Vista previa con un invitado de ejemplo.</summary>
    [HttpGet("{id:guid}/preview")]
    [Authorize(Policy = Permissions.CardsViewOwn)]
    public async Task<IActionResult> Preview(Guid id)
    {
        var inv = await Find(id, withTemplate: true);
        if (inv?.Template is null) return NotFound();
        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(inv.DataJson) ?? [];
        data["invitado"] = "Familia Pérez";
        return Ok(new { html = TemplateRenderer.Render(inv.Template.Html, inv.Template.Css, data, ApiBase) });
    }

    // ---------- Panel e invitados ----------

    /// <summary>Todo lo que muestra el panel en una sola llamada (la pantalla la consulta cada pocos segundos).</summary>
    [HttpGet("{id:guid}/panel")]
    [Authorize(Policy = Permissions.CardsViewOwn)]
    public async Task<IActionResult> Panel(Guid id)
    {
        var inv = await Find(id, withGuests: true);
        if (inv is null) return NotFound();
        var g = inv.Guests.OrderBy(x => x.Name).ToList();
        var confirmed = g.Where(x => x.Status == GuestStatus.Confirmed).ToList();
        var daysLeft = inv.EventDate is { } d ? (int?)Math.Max(0, (int)Math.Ceiling((d.Date - DateTime.UtcNow.Date).TotalDays)) : null;
        return Ok(new
        {
            inv.Id, inv.Title, inv.Slug, Status = inv.Status.ToString(), inv.EventDate, DaysLeft = daysLeft,
            Totals = new
            {
                Guests = g.Count,
                PassesInvited = g.Sum(x => x.MaxPasses),
                ConfirmedGuests = confirmed.Count,
                ConfirmedPasses = confirmed.Sum(x => x.ConfirmedPasses),
                Declined = g.Count(x => x.Status == GuestStatus.Declined),
                Pending = g.Count(x => x.Status == GuestStatus.Pending),
                Sent = g.Count(x => x.SentAt != null),
                Opened = g.Count(x => x.OpenedAt != null),
            },
            Guests = g.Select(x => new
            {
                x.Id, x.Name, x.Phone, x.MaxPasses, x.Token, Status = x.Status.ToString(), x.ConfirmedPasses, x.Message,
                x.SentAt, x.OpenedAt, x.RespondedAt,
            }),
            Activity = g.Where(x => x.RespondedAt != null).OrderByDescending(x => x.RespondedAt).Take(8)
                .Select(x => new { x.Id, x.Name, Status = x.Status.ToString(), x.ConfirmedPasses, x.Message, At = x.RespondedAt }),
        });
    }

    [HttpPost("{id:guid}/guests")]
    [Authorize(Policy = Permissions.CardsEditOwn)]
    public async Task<IActionResult> AddGuests(Guid id, AddGuestsRequest r)
    {
        var inv = await Find(id);
        if (inv is null) return NotFound();
        var list = (r.Guests ?? []).Where(x => !string.IsNullOrWhiteSpace(x.Name)).ToList();
        if (list.Count == 0) return Bad("Agrega al menos un invitado con nombre.");
        if (list.Count > MaxPerRequest) return Bad($"Máximo {MaxPerRequest} invitados por vez.");
        var current = await db.Guests.CountAsync(x => x.InvitationId == id);
        if (current + list.Count > MaxGuests) return Bad($"Una invitación admite hasta {MaxGuests} invitados.");

        var added = new List<Guest>();
        foreach (var x in list)
        {
            var phone = NormalizePhone(x.Phone);
            if (phone is null) return Bad($"El teléfono de «{x.Name!.Trim()}» no es válido. Usa 10 dígitos o con lada de país.");
            added.Add(new Guest
            {
                InvitationId = id, Name = Truncate(x.Name!.Trim(), 120), Phone = phone,
                MaxPasses = Math.Clamp(x.Passes, 1, Guest.MaxPassesLimit), Token = NewToken(),
            });
        }
        db.Guests.AddRange(added);
        await db.SaveChangesAsync();
        return Ok(new { added = added.Count });
    }

    [HttpPut("{id:guid}/guests/{guestId:guid}")]
    [Authorize(Policy = Permissions.CardsEditOwn)]
    public async Task<IActionResult> UpdateGuest(Guid id, Guid guestId, GuestInput r)
    {
        var guest = await FindGuest(id, guestId);
        if (guest is null) return NotFound();
        if (string.IsNullOrWhiteSpace(r.Name)) return Bad("El invitado necesita un nombre.");
        var phone = NormalizePhone(r.Phone);
        if (phone is null) return Bad("El teléfono no es válido. Usa 10 dígitos o con lada de país.");
        guest.Name = Truncate(r.Name.Trim(), 120);
        guest.Phone = phone;
        guest.MaxPasses = Math.Clamp(r.Passes, 1, Guest.MaxPassesLimit);
        if (guest.Status == GuestStatus.Confirmed && guest.ConfirmedPasses > guest.MaxPasses) guest.ConfirmedPasses = guest.MaxPasses;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}/guests/{guestId:guid}")]
    [Authorize(Policy = Permissions.CardsEditOwn)]
    public async Task<IActionResult> DeleteGuest(Guid id, Guid guestId)
    {
        var guest = await FindGuest(id, guestId);
        if (guest is null) return NotFound();
        db.Guests.Remove(guest);
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Marca que ya se le mandó la invitación por WhatsApp.</summary>
    [HttpPost("{id:guid}/guests/{guestId:guid}/sent")]
    [Authorize(Policy = Permissions.CardsEditOwn)]
    public async Task<IActionResult> MarkSent(Guid id, Guid guestId)
    {
        var guest = await FindGuest(id, guestId);
        if (guest is null) return NotFound();
        guest.SentAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ---------- Utilidades ----------

    private async Task<Invitation?> Find(Guid id, bool withTemplate = false, bool withGuests = false)
    {
        var uid = UserId;
        IQueryable<Invitation> q = db.Invitations;
        if (withTemplate) q = q.Include(i => i.Template);
        if (withGuests) q = q.Include(i => i.Guests);
        return await q.FirstOrDefaultAsync(i => i.Id == id && i.UserId == uid);
    }

    private async Task<Guest?> FindGuest(Guid id, Guid guestId)
    {
        var uid = UserId;
        return await db.Guests.FirstOrDefaultAsync(g => g.Id == guestId && g.InvitationId == id
            && db.Invitations.Any(i => i.Id == id && i.UserId == uid));
    }

    private Task<Template?> InvitationTemplate(Guid id) =>
        db.Templates.FirstOrDefaultAsync(t => t.Id == id && t.IsActive && t.Product == Products.Invitations);

    private async Task<string> UniqueSlug(string baseSlug)
    {
        var slug = string.IsNullOrEmpty(baseSlug) ? "evento" : baseSlug;
        if (slug.Length > 24) slug = slug[..24].TrimEnd('-');
        var candidate = slug;
        for (var i = 0; i < 6 && (SlugRules.Validate(candidate) is not null || await db.Invitations.IgnoreQueryFilters().AnyAsync(x => x.Slug == candidate)); i++)
            candidate = $"{slug}-{RandomNumberGenerator.GetInt32(100, 999)}";
        return candidate;
    }

    private static Dictionary<string, string> CleanData(Dictionary<string, string>? data) =>
        (data ?? []).Where(kv => CardFields.ByKey.ContainsKey(kv.Key) && kv.Key != "invitado" && !string.IsNullOrWhiteSpace(kv.Value))
            .ToDictionary(kv => kv.Key, kv => Truncate(kv.Value.Trim(), 1000));

    private static string Truncate(string s, int max) => s.Length > max ? s[..max] : s;

    /// <summary>Solo dígitos. 10 dígitos = México (antepone 52). Vacío es válido (se comparte el enlace a mano).</summary>
    public static string? NormalizePhone(string? raw)
    {
        var digits = new string((raw ?? "").Where(char.IsDigit).ToArray());
        if (digits.Length == 0) return "";
        if (digits.Length == 10) return "52" + digits;
        return digits.Length is >= 11 and <= 15 ? digits : null;
    }

    private static string NewToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static object Detail(Invitation i) => new
    {
        i.Id, i.Title, i.Slug, Status = i.Status.ToString(), i.EventDate, i.TemplateId,
        TemplateName = i.Template?.Name, TemplateFields = i.Template?.FieldKeys.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? [],
        Data = JsonSerializer.Deserialize<Dictionary<string, string>>(i.DataJson) ?? [],
    };

    private BadRequestObjectResult Bad(string msg) => BadRequest(new { errors = new[] { msg } });
}

public record StatusRequestDto(InvitationStatus Status);
