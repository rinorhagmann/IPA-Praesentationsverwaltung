namespace IPA_Praesentationsverwaltung.Services.Abstractions;

/// <summary>Resets the system at the start of a new school year.</summary>
public interface ISystemResetService
{
    /// <summary>
    /// Irreversibly removes all registrations, presentations, students and rooms.
    /// Administrator accounts are intentionally preserved.
    /// </summary>
    Task ResetSchoolYearAsync(CancellationToken cancellationToken = default);
}
