namespace IPA_Praesentationsverwaltung.Models.Dtos;

/// <summary>
/// One parsed row of the G4 presentation CSV import file
/// (columns: Topic, StartsAt, RoomName).
/// </summary>
public sealed record PresentationCsvRow(string Topic, DateTime StartsAt, string RoomName);
