using IPA_Praesentationsverwaltung.Models.Domain;

namespace IPA_Praesentationsverwaltung.Services.Abstractions;

/// <summary>Composes and sends the application's e-mail notifications.</summary>
public interface INotificationService
{
    /// <summary>Sends the generated access credentials to a student.</summary>
    Task SendCredentialsAsync(Student student, string plainPassword, CancellationToken cancellationToken = default);

    /// <summary>Sends a confirmation listing the presentations the student selected.</summary>
    Task SendConfirmationAsync(Student student, IReadOnlyList<Presentation> selectedPresentations, CancellationToken cancellationToken = default);

    /// <summary>Notifies an administrator that the assignment phase is complete.</summary>
    Task SendAdminNotificationAsync(Admin admin, string message, CancellationToken cancellationToken = default);
}
