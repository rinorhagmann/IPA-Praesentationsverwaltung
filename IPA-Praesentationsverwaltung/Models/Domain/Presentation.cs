using System.ComponentModel.DataAnnotations;

namespace IPA_Praesentationsverwaltung.Models.Domain;

/// <summary>
/// A G4 Matura presentation that G3 students can observe. Presentations are
/// imported from a CSV file and carry the scheduling information as well as the
/// observer capacity.
/// </summary>
public class Presentation
{
    /// <summary>The maximum number of observers allowed per presentation.</summary>
    public const int DefaultMaxObservers = 6;

    public int Id { get; set; }

    /// <summary>Topic / subject of the Matura work being presented.</summary>
    [Required]
    [MaxLength(300)]
    public string Topic { get; set; } = string.Empty;

    /// <summary>Date and time the presentation starts.</summary>
    public DateTime StartsAt { get; set; }

    /// <summary>Foreign key to the room the presentation is held in.</summary>
    public int RoomId { get; set; }

    public Room? Room { get; set; }

    /// <summary>Upper bound of observers; defaults to six per the requirements.</summary>
    public int MaxObservers { get; set; } = DefaultMaxObservers;

    /// <summary>Observer registrations attached to this presentation.</summary>
    public ICollection<Registration> Registrations { get; set; } = new List<Registration>();

    /// <summary>
    /// True while the presentation has not yet reached its observer capacity.
    /// </summary>
    public bool HasFreeSeats() => Registrations.Count < MaxObservers;

    /// <summary>
    /// Returns true when this presentation starts at exactly the same moment as
    /// <paramref name="other"/>. Used to prevent a student from booking two
    /// simultaneous presentations.
    /// </summary>
    public bool IsAtSameTime(Presentation other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return StartsAt == other.StartsAt;
    }
}
