using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TarjetasPro.Application.Auth;

namespace TarjetasPro.Infrastructure.Auth;

public class EmailOptions
{
    public const string Section = "Email";
    /// <summary>Vacío = no se envía nada; el mensaje (con su enlace) se escribe en el log. Solo para desarrollo.</summary>
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public string User { get; set; } = "";
    public string Password { get; set; } = "";
    public bool EnableSsl { get; set; } = true;
    public string From { get; set; } = "";
    public string FromName { get; set; } = "Tarjetas Pro";
}

public partial class SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly EmailOptions _opt = options.Value;

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex TagRx();

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_opt.Host))
        {
            logger.LogWarning("[CORREO sin configurar] Para: {To} · {Subject}\n{Body}", to, subject,
                TagRx().Replace(htmlBody.Replace("</a>", " </a>"), " ") + "\nEnlaces: " +
                string.Join(" ", Regex.Matches(htmlBody, "href=\"([^\"]+)\"").Select(m => System.Net.WebUtility.HtmlDecode(m.Groups[1].Value))));
            return;
        }

        using var client = new SmtpClient(_opt.Host, _opt.Port)
        {
            EnableSsl = _opt.EnableSsl,
            Credentials = string.IsNullOrEmpty(_opt.User) ? null : new NetworkCredential(_opt.User, _opt.Password),
        };
        using var msg = new MailMessage
        {
            From = new MailAddress(string.IsNullOrWhiteSpace(_opt.From) ? _opt.User : _opt.From, _opt.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true,
        };
        msg.To.Add(to);
        await client.SendMailAsync(msg, ct);
    }
}
