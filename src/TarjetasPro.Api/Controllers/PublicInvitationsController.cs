using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TarjetasPro.Application.Templates;
using TarjetasPro.Domain.Cards;
using TarjetasPro.Domain.Entities;
using TarjetasPro.Infrastructure;

namespace TarjetasPro.Api.Controllers;

public record RespondRequest(string? Token, bool Attending, int Passes = 1, string? Message = null);

/// <summary>Lo que ve el invitado: la invitación con su nombre y el formulario para confirmar. Anónimo; el token personal identifica al invitado.</summary>
[ApiController]
[Route("api/public-invitations")]
[EnableRateLimiting("public")]
public class PublicInvitationsController(AppDbContext db) : ControllerBase
{
    private string ApiBase => $"{Request.Scheme}://{Request.Host}";

    [HttpGet("{slug}")]
    public async Task<IActionResult> Get(string slug, [FromQuery(Name = "g")] string? token)
    {
        var inv = await Load(slug);
        if (inv?.Template is null) return NotFound();

        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(inv.DataJson) ?? [];
        Guest? guest = null;
        if (!string.IsNullOrWhiteSpace(token))
        {
            guest = await db.Guests.FirstOrDefaultAsync(g => g.InvitationId == inv.Id && g.Token == token);
            if (guest is not null)
            {
                data["invitado"] = guest.Name;
                if (guest.OpenedAt is null) { guest.OpenedAt = DateTime.UtcNow; await db.SaveChangesAsync(); }
            }
        }

        string Get(string k) => data.TryGetValue(k, out var v) ? v.Trim() : "";
        var name = Get("nombre") is { Length: > 0 } n ? n : inv.Title;
        var html = TemplateRenderer.Render(inv.Template.Html, inv.Template.Css, data, ApiBase);
        return Ok(new
        {
            inv.Slug, Title = name, Html = html, inv.EventDate,
            Guest = guest is null ? null : new { guest.Name, guest.MaxPasses, Status = guest.Status.ToString(), guest.ConfirmedPasses, guest.Message },
        });
    }

    [HttpPost("{slug}/respond")]
    public async Task<IActionResult> Respond(string slug, RespondRequest r)
    {
        var inv = await Load(slug);
        if (inv is null) return NotFound();
        if (string.IsNullOrWhiteSpace(r.Token)) return BadRequest(new { errors = new[] { "Abre tu invitación desde el enlace que te enviaron para poder confirmar." } });
        var guest = await db.Guests.FirstOrDefaultAsync(g => g.InvitationId == inv.Id && g.Token == r.Token);
        if (guest is null) return NotFound();

        if (r.Attending)
        {
            if (r.Passes < 1 || r.Passes > guest.MaxPasses)
                return BadRequest(new { errors = new[] { $"Tienes hasta {guest.MaxPasses} {(guest.MaxPasses == 1 ? "pase" : "pases")}." } });
            guest.Status = GuestStatus.Confirmed;
            guest.ConfirmedPasses = r.Passes;
        }
        else
        {
            guest.Status = GuestStatus.Declined;
            guest.ConfirmedPasses = 0;
        }
        var msg = (r.Message ?? "").Trim();
        guest.Message = msg.Length == 0 ? null : msg[..Math.Min(msg.Length, 300)];
        guest.RespondedAt = DateTime.UtcNow;
        if (guest.OpenedAt is null) guest.OpenedAt = guest.RespondedAt;
        await db.SaveChangesAsync();
        return Ok(new { status = guest.Status.ToString(), guest.ConfirmedPasses });
    }

    private Task<Invitation?> Load(string slug)
    {
        var normalized = SlugRules.Normalize(slug);
        return db.Invitations.Include(i => i.Template)
            .FirstOrDefaultAsync(i => i.Slug == normalized && i.Status == InvitationStatus.Published
                && db.Users.Any(u => u.Id == i.UserId && u.IsActive));
    }
}
