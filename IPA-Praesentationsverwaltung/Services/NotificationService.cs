using System.Globalization;
using System.Text;
using IPA_Praesentationsverwaltung.Models.Domain;
using IPA_Praesentationsverwaltung.Services.Abstractions;
using IPA_Praesentationsverwaltung.Services.Infrastructure;

namespace IPA_Praesentationsverwaltung.Services;

/// <summary>
/// Builds the German notification e-mails and hands them to the configured
/// <see cref="IEmailSender"/> transport.
/// </summary>
public sealed class NotificationService : INotificationService
{
    private static readonly CultureInfo Swiss = CultureInfo.GetCultureInfo("de-CH");

    private readonly IEmailSender _emailSender;

    public NotificationService(IEmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    public Task SendCredentialsAsync(Student student, string plainPassword, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(student);

        string body = new StringBuilder()
            .AppendLine($"Guten Tag {student.GetFullName()}")
            .AppendLine()
            .AppendLine("Für die Anmeldung zu den Maturaarbeitspräsentationen wurde ein Zugang erstellt.")
            .AppendLine()
            .AppendLine($"Benutzername (E-Mail): {student.Email}")
            .AppendLine($"Passwort: {plainPassword}")
            .AppendLine()
            .AppendLine("Bitte melden Sie sich an und wählen Sie zwei Präsentationen aus.")
            .AppendLine()
            .AppendLine("Freundliche Grüsse")
            .AppendLine("Wirtschaftsgymnasium Basel")
            .ToString();

        return _emailSender.SendAsync(student.Email, "Ihre Zugangsdaten", body, cancellationToken);
    }

    public Task SendConfirmationAsync(
        Student student, IReadOnlyList<Presentation> selectedPresentations, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(student);
        ArgumentNullException.ThrowIfNull(selectedPresentations);

        var builder = new StringBuilder()
            .AppendLine($"Guten Tag {student.GetFullName()}")
            .AppendLine()
            .AppendLine("Ihre Anmeldung zu den folgenden Präsentationen wurde bestätigt:")
            .AppendLine();

        foreach (Presentation presentation in selectedPresentations)
        {
            builder.AppendLine(
                $"- {presentation.StartsAt.ToString("dd.MM.yyyy HH:mm", Swiss)} Uhr, " +
                $"Raum {presentation.Room?.Name}: {presentation.Topic}");
        }

        builder.AppendLine()
               .AppendLine("Freundliche Grüsse")
               .AppendLine("Wirtschaftsgymnasium Basel");

        return _emailSender.SendAsync(student.Email, "Bestätigung Ihrer Präsentationsauswahl", builder.ToString(), cancellationToken);
    }

    public Task SendAdminNotificationAsync(Admin admin, string message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(admin);

        string body = new StringBuilder()
            .AppendLine($"Guten Tag {admin.GetFullName()}")
            .AppendLine()
            .AppendLine(message)
            .ToString();

        return _emailSender.SendAsync(admin.Email, "Information zur Präsentationszuteilung", body, cancellationToken);
    }
}
