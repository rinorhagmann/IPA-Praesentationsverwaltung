namespace IPA_Praesentationsverwaltung.Models.Dtos;

/// <summary>
/// One parsed row of the G3 student CSV import file
/// (columns: FirstName, LastName, Email).
/// </summary>
public sealed record StudentCsvRow(string FirstName, string LastName, string Email);
