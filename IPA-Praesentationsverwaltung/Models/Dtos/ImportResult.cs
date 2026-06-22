namespace IPA_Praesentationsverwaltung.Models.Dtos;

/// <summary>
/// Outcome of a CSV import: how many rows were imported and any per-row errors
/// that were skipped. Returned instead of throwing so the UI can report a
/// meaningful, line-by-line summary.
/// </summary>
public sealed class ImportResult
{
    public int ImportedCount { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = new List<string>();

    public bool HasErrors => Errors.Count > 0;
}
