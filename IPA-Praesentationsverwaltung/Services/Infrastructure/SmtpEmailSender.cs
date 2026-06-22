using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IPA_Praesentationsverwaltung.Services.Infrastructure;

/// <summary>
/// Sends e-mail over SMTP using the framework's <see cref="SmtpClient"/>. The
/// server, credentials and sender address are taken from <see cref="EmailOptions"/>.
/// A new client is created per message, which is the recommended usage pattern.
/// </summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipient);

        using var message = new MailMessage
        {
            From = new MailAddress(_options.From, _options.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };
        message.To.Add(recipient);

        using var client = new SmtpClient(_options.Smtp.Host!, _options.Smtp.Port)
        {
            EnableSsl = _options.Smtp.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        // Only attach credentials when a user is configured (some relays are open).
        if (!string.IsNullOrWhiteSpace(_options.Smtp.User))
        {
            client.Credentials = new NetworkCredential(_options.Smtp.User, _options.Smtp.Password);
        }

        try
        {
            await client.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("Sent e-mail to {Recipient} via SMTP (subject: {Subject}).", recipient, subject);
        }
        catch (Exception ex) when (ex is SmtpException or InvalidOperationException)
        {
            // Surface a clear, actionable error instead of a generic 500.
            _logger.LogError(ex, "SMTP delivery to {Recipient} failed.", recipient);
            throw new InvalidOperationException(
                $"Der E-Mail-Versand an {recipient} ist fehlgeschlagen. Bitte prüfen Sie die SMTP-Einstellungen.", ex);
        }
    }
}
