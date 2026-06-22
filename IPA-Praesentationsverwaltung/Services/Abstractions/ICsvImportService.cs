using IPA_Praesentationsverwaltung.Models.Dtos;

namespace IPA_Praesentationsverwaltung.Services.Abstractions;

/// <summary>Imports students and presentations from uploaded CSV files.</summary>
public interface ICsvImportService
{
    /// <summary>
    /// Imports G3 students (columns: FirstName, LastName, Email). A random
    /// initial password is generated for every imported student.
    /// </summary>
    Task<ImportResult> ImportStudentsAsync(Stream csvFile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports G4 presentations (columns: Topic, Date/Time, Room). Rooms are
    /// created on demand.
    /// </summary>
    Task<ImportResult> ImportPresentationsAsync(Stream csvFile, CancellationToken cancellationToken = default);
}
