using IPA_Praesentationsverwaltung.Models.Domain;

namespace IPA_Praesentationsverwaltung.Services.Abstractions;

/// <summary>CRUD operations and credential handling for G3 students.</summary>
public interface IStudentService
{
    Task<IReadOnlyList<Student>> GetAllStudentsAsync(CancellationToken cancellationToken = default);

    Task<Student?> GetStudentByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a new student. When <paramref name="plainPassword"/> is null a
    /// random initial password is generated. Returns the plaintext password so
    /// the caller can deliver it; it is never stored in clear text.
    /// </summary>
    Task<string> CreateStudentAsync(Student student, string? plainPassword = null, CancellationToken cancellationToken = default);

    Task UpdateStudentAsync(Student student, string? newPlainPassword = null, CancellationToken cancellationToken = default);

    Task DeleteStudentAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Generates a new strong, human-readable initial password (plaintext).</summary>
    string GenerateInitialPassword();

    /// <summary>
    /// Students that have not yet received their access credentials by e-mail.
    /// </summary>
    Task<IReadOnlyList<Student>> GetStudentsWithoutSentCredentialsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a freshly generated password to the student, stores its hash and
    /// returns the plaintext for immediate delivery.
    /// </summary>
    Task<string> ResetPasswordAsync(int studentId, CancellationToken cancellationToken = default);

    /// <summary>Marks the student's credentials as delivered.</summary>
    Task MarkCredentialsSentAsync(int studentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores that the student has irrevocably confirmed their selection. Idempotent:
    /// the timestamp is not overwritten if it is already set.
    /// </summary>
    Task MarkSelectionConfirmedAsync(int studentId, CancellationToken cancellationToken = default);
}
