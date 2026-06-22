namespace IPA_Praesentationsverwaltung.Models.Dtos;

/// <summary>
/// A single line of the <see cref="RoomObserverList"/>: one presentation with
/// its room, start time and the names of the observers assigned to it.
/// </summary>
public sealed class RoomObserverListItem
{
    public string RoomName { get; init; } = string.Empty;

    public string PresentationTopic { get; init; } = string.Empty;

    public DateTime StartsAt { get; init; }

    public IReadOnlyList<string> ObserverNames { get; init; } = new List<string>();
}
