namespace IPA_Praesentationsverwaltung.Models.Domain;

/// <summary>
/// A G3 student who is required to observe two G4 presentations.
/// Students are imported from a CSV file at the start of a school year.
/// </summary>
public class Student : User
{
    /// <summary>The number of presentations every student has to attend.</summary>
    public const int RequiredSelectionCount = 2;

    /// <summary>
    /// Whether the generated initial credentials have already been e-mailed
    /// to the student. Prevents sending access data twice.
    /// </summary>
    public bool InitialPasswordSent { get; set; }

    /// <summary>
    /// Set once the student has irrevocably confirmed their presentation
    /// selection. After this point the student may no longer add or remove
    /// registrations themselves.
    /// </summary>
    public DateTime? SelectionConfirmedAt { get; set; }

    /// <summary>Registrations the student created for presentations.</summary>
    public ICollection<Registration> Registrations { get; set; } = new List<Registration>();

    /// <summary>
    /// True once the student has selected the required number of presentations.
    /// </summary>
    public bool HasCompletedSelection() => Registrations.Count >= RequiredSelectionCount;

    /// <summary>True after the student locked in their selection.</summary>
    public bool IsSelectionConfirmed => SelectionConfirmedAt.HasValue;
}
