using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IPA_Praesentationsverwaltung.Services.Infrastructure;

/// <summary>
/// Development e-mail sender that does not require an SMTP server: every message
/// is logged and, on a best-effort basis, written to a ".eml"-style file in an
/// outbox directory. Used automatically when no SMTP host is configured.
/// </summary>
public sealed class FileEmailSender : IEmailSender
{
    private readonly ILogger<FileEmailSender> _logger;
    private readonly string _outboxPath;

    public FileEmailSender(IOptions<EmailOptions> options, ILogger<FileEmailSender> logger)
    {
        _logger = logger;

        // Allow overriding the location; default to a writable temp subdirectory
        // so the non-root container user can always create it.
        _outboxPath = options.Value.OutboxPath
            ?? Path.Combine(Path.GetTempPath(), "wgbs-mail-outbox");
    }

    public async Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default)
    {
        string message = new StringBuilder()
            .AppendLine($"To: {recipient}")
            .AppendLine($"Subject: {subject}")
            .AppendLine($"Date: {DateTimeOffset.Now:R}")
            .AppendLine()
            .AppendLine(body)
            .ToString();

        _logger.LogInformation(
            "Outgoing e-mail to {Recipient} (subject: {Subject}). No SMTP host configured – stored in the outbox only.",
            recipient, subject);

        // Persisting the message is a convenience for inspection during
        // development and must never break the calling flow.
        try
        {
            Directory.CreateDirectory(_outboxPath);
            string fileName = $"{DateTime.Now:yyyyMMdd-HHmmssfff}_{Sanitize(recipient)}.eml";
            await File.WriteAllTextAsync(Path.Combine(_outboxPath, fileName), message, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Could not write the e-mail to the outbox at {Path}.", _outboxPath);
        }
    }

    private static string Sanitize(string value) =>
        string.Concat(value.Where(c => char.IsLetterOrDigit(c) || c is '@' or '.' or '-' or '_'));
}
