namespace IPA_Praesentationsverwaltung.Models.Dtos;

/// <summary>
/// Printable / exportable overview of all rooms with the presentations taking
/// place in them and the observers assigned to each presentation.
/// </summary>
public sealed class RoomObserverList
{
    public IReadOnlyList<RoomObserverListItem> Items { get; init; } = new List<RoomObserverListItem>();

    /// <summary>Moment the list was generated, shown on the printed report.</summary>
    public DateTime GeneratedAt { get; init; } = DateTime.Now;
}
