namespace IPA_Praesentationsverwaltung.Services.Infrastructure;

/// <summary>
/// Strongly typed e-mail configuration, bound from the "Email" section of the
/// application configuration. When <see cref="Smtp"/>.<see cref="SmtpOptions.Host"/>
/// is set, real SMTP delivery is used; otherwise the development file sender runs.
/// </summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>Sender address used as the "From" header.</summary>
    public string From { get; set; } = "noreply@wgbs.ch";

    /// <summary>Display name shown for the sender.</summary>
    public string FromName { get; set; } = "Wirtschaftsgymnasium Basel";

    /// <summary>Optional directory for the development file sender.</summary>
    public string? OutboxPath { get; set; }

    public SmtpOptions Smtp { get; set; } = new();

    /// <summary>True when a usable SMTP host is configured.</summary>
    public bool IsSmtpConfigured => !string.IsNullOrWhiteSpace(Smtp.Host);
}

/// <summary>SMTP server connection settings.</summary>
public sealed class SmtpOptions
{
    public string? Host { get; set; }

    public int Port { get; set; } = 587;

    /// <summary>Login user; leave empty for an unauthenticated relay.</summary>
    public string? User { get; set; }

    public string? Password { get; set; }

    /// <summary>Whether STARTTLS / SSL should be used (recommended).</summary>
    public bool EnableSsl { get; set; } = true;
}
