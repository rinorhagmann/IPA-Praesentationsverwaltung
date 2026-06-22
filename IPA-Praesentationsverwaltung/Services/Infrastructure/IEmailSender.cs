namespace IPA_Praesentationsverwaltung.Services.Infrastructure;

/// <summary>
/// Transport abstraction for outgoing e-mail. Keeping the transport behind an
/// interface lets the notification logic stay independent of how mail is
/// actually delivered (SMTP in production, a log/file sink during development).
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default);
}
