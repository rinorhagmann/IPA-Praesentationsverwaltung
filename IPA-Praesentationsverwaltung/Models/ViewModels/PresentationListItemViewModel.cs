using IPA_Praesentationsverwaltung.Models.Domain;

namespace IPA_Praesentationsverwaltung.Models.ViewModels;

/// <summary>
/// Display model for a single presentation row in the student facing lists
/// (available presentations and "my selection"). It carries the precomputed
/// seat information so the views stay free of business logic.
/// </summary>
public class PresentationListItemViewModel
{
    public int Id { get; init; }
    public string Topic { get; init; } = string.Empty;
    public DateTime StartsAt { get; init; }
    public string RoomName { get; init; } = string.Empty;
    public int MaxObservers { get; init; }
    public int TakenSeats { get; init; }

    /// <summary>Whether the current student has already selected this presentation.</summary>
    public bool IsSelectedByStudent { get; init; }

    /// <summary>
    /// When <see cref="IsSelectedByStudent"/> is true, holds the id of the
    /// registration row so the student can remove the selection.
    /// </summary>
    public int? RegistrationId { get; init; }

    public int FreeSeats => Math.Max(0, MaxObservers - TakenSeats);
    public bool HasFreeSeats => FreeSeats > 0;

    public static PresentationListItemViewModel FromDomain(
        Presentation presentation, bool isSelectedByStudent, int? registrationId = null)
    {
        ArgumentNullException.ThrowIfNull(presentation);
        return new PresentationListItemViewModel
        {
            Id = presentation.Id,
            Topic = presentation.Topic,
            StartsAt = presentation.StartsAt,
            RoomName = presentation.Room?.Name ?? string.Empty,
            MaxObservers = presentation.MaxObservers,
            TakenSeats = presentation.Registrations.Count,
            IsSelectedByStudent = isSelectedByStudent,
            RegistrationId = registrationId
        };
    }
}
