using IPA_Praesentationsverwaltung.Models.Domain;

namespace IPA_Praesentationsverwaltung.Services.Abstractions;

/// <summary>
/// Thrown when a registration violates an assignment rule (capacity, time
/// conflict or the maximum number of selections). The message is suitable for
/// display to the end user.
/// </summary>
public sealed class RegistrationNotAllowedException : Exception
{
    public RegistrationNotAllowedException(string message) : base(message)
    {
    }
}

/// <summary>Creates, queries and removes observer registrations.</summary>
public interface IRegistrationService
{
    Task<IReadOnlyList<Registration>> GetRegistrationsByStudentAsync(int studentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Registration>> GetAllRegistrationsAsync(CancellationToken cancellationToken = default);

    Task<Registration?> GetRegistrationByIdAsync(int registrationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers the student for the presentation after validating every
    /// assignment rule. Throws <see cref="RegistrationNotAllowedException"/> when
    /// the registration would break a rule.
    /// </summary>
    Task CreateRegistrationAsync(int studentId, int presentationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Administrative override that reassigns an existing registration without
    /// the student-facing rule checks, supporting manual corrections.
    /// </summary>
    Task UpdateRegistrationAsync(int registrationId, int studentId, int presentationId, CancellationToken cancellationToken = default);

    Task DeleteRegistrationAsync(int registrationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates whether the student may register for the presentation. Both
    /// aggregates must have their registrations (and the presentations behind
    /// them) loaded.
    /// </summary>
    bool CanRegister(Student student, Presentation presentation);
}
