using IPA_Praesentationsverwaltung.Services.Infrastructure;

namespace IPA_Praesentationsverwaltung.Tests.TestSupport;

/// <summary>In-memory <see cref="IEmailSender"/> that records every message for assertions.</summary>
public sealed class FakeEmailSender : IEmailSender
{
    public List<SentEmail> Sent { get; } = new();

    public Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default)
    {
        Sent.Add(new SentEmail(recipient, subject, body));
        return Task.CompletedTask;
    }

    public sealed record SentEmail(string Recipient, string Subject, string Body);
}
