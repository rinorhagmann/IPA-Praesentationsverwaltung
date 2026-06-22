using IPA_Praesentationsverwaltung.Models.Dtos;

namespace IPA_Praesentationsverwaltung.Services.Abstractions;

/// <summary>How the room/observer overview should be ordered.</summary>
public enum ListOrder
{
    /// <summary>Grouped by room, then chronologically (room lists).</summary>
    ByRoom,

    /// <summary>Chronologically across all rooms (observer lists).</summary>
    ByTime
}

/// <summary>Builds and exports the printable room and observer overviews.</summary>
public interface IExportService
{
    /// <summary>Creates the overview of rooms, presentations and assigned observers.</summary>
    Task<RoomObserverList> CreatePrintListAsync(ListOrder order, CancellationToken cancellationToken = default);

    /// <summary>Serialises the overview to a UTF-8 CSV document.</summary>
    byte[] ExportAsCsv(RoomObserverList list);

    /// <summary>Renders the overview to a PDF document.</summary>
    byte[] ExportAsPdf(RoomObserverList list, string title);
}
