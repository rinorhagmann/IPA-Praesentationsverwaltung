namespace IPA_Praesentationsverwaltung.Models.Domain;

/// <summary>
/// Join entity that records that a student registered as an observer for a
/// specific presentation. The combination of <see cref="StudentId"/> and
/// <see cref="PresentationId"/> is unique.
/// </summary>
public class Registration
{
    public int Id { get; set; }

    /// <summary>Timestamp the registration was created.</summary>
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    public int StudentId { get; set; }

    public Student? Student { get; set; }

    public int PresentationId { get; set; }

    public Presentation? Presentation { get; set; }
}
