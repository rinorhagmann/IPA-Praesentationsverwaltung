using System.Globalization;
using System.Text;
using IPA_Praesentationsverwaltung.Data;
using IPA_Praesentationsverwaltung.Models.Dtos;
using IPA_Praesentationsverwaltung.Services.Abstractions;
using IPA_Praesentationsverwaltung.Services.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace IPA_Praesentationsverwaltung.Services;

/// <summary>
/// Produces the room and observer overviews and exports them as CSV or PDF.
/// </summary>
public sealed class ExportService : IExportService
{
    private const char CsvDelimiter = ';';
    private static readonly CultureInfo Swiss = CultureInfo.GetCultureInfo("de-CH");

    private readonly ApplicationDbContext _dbContext;

    public ExportService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RoomObserverList> CreatePrintListAsync(ListOrder order, CancellationToken cancellationToken = default)
    {
        // Project everything needed for the report in a single query.
        var rows = await _dbContext.Presentations
            .AsNoTracking()
            .Select(p => new
            {
                RoomName = p.Room!.Name,
                p.Topic,
                p.StartsAt,
                Observers = p.Registrations
                    .Select(r => r.Student!.LastName + " " + r.Student.FirstName)
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        IEnumerable<RoomObserverListItem> items = rows.Select(r => new RoomObserverListItem
        {
            RoomName = r.RoomName,
            PresentationTopic = r.Topic,
            StartsAt = r.StartsAt,
            ObserverNames = r.Observers.OrderBy(name => name, StringComparer.CurrentCulture).ToList()
        });

        items = order == ListOrder.ByRoom
            ? items.OrderBy(i => i.RoomName, StringComparer.CurrentCulture).ThenBy(i => i.StartsAt)
            : items.OrderBy(i => i.StartsAt).ThenBy(i => i.RoomName, StringComparer.CurrentCulture);

        return new RoomObserverList { Items = items.ToList() };
    }

    public byte[] ExportAsCsv(RoomObserverList list)
    {
        ArgumentNullException.ThrowIfNull(list);

        var builder = new StringBuilder();
        builder.AppendLine(string.Join(CsvDelimiter, "Raum", "Datum", "Uhrzeit", "Thema", "Zuseher"));

        foreach (RoomObserverListItem item in list.Items)
        {
            string date = item.StartsAt.ToString("dd.MM.yyyy", Swiss);
            string time = item.StartsAt.ToString("HH:mm", Swiss);

            // One row per observer keeps the export normalised; presentations
            // without observers still appear with an empty observer column.
            if (item.ObserverNames.Count == 0)
            {
                builder.AppendLine(BuildCsvRow(item.RoomName, date, time, item.PresentationTopic, string.Empty));
                continue;
            }

            foreach (string observer in item.ObserverNames)
            {
                builder.AppendLine(BuildCsvRow(item.RoomName, date, time, item.PresentationTopic, observer));
            }
        }

        // Prepend a UTF-8 BOM so Excel opens umlauts correctly.
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(builder.ToString())).ToArray();
    }

    public byte[] ExportAsPdf(RoomObserverList list, string title)
    {
        ArgumentNullException.ThrowIfNull(list);

        var pdf = new SimplePdfWriter();
        pdf.AddHeading(title);
        pdf.AddLine($"Erstellt am {list.GeneratedAt.ToString("dd.MM.yyyy HH:mm", Swiss)} Uhr");
        pdf.AddBlankLine();

        foreach (RoomObserverListItem item in list.Items)
        {
            pdf.AddSubHeading(
                $"Raum {item.RoomName} – {item.StartsAt.ToString("dd.MM.yyyy HH:mm", Swiss)} Uhr");
            pdf.AddLine($"Thema: {item.PresentationTopic}");

            if (item.ObserverNames.Count == 0)
            {
                pdf.AddLine("Zuseher: keine");
            }
            else
            {
                pdf.AddLine("Zuseher:");
                foreach (string observer in item.ObserverNames)
                {
                    pdf.AddLine($"   - {observer}");
                }
            }

            pdf.AddBlankLine();
        }

        return pdf.Build();
    }

    private static string BuildCsvRow(params string[] fields) =>
        string.Join(CsvDelimiter, fields.Select(EscapeCsv));

    /// <summary>Quotes a field when it contains the delimiter, quotes or line breaks.</summary>
    private static string EscapeCsv(string value)
    {
        value ??= string.Empty;
        bool mustQuote = value.Contains(CsvDelimiter) || value.Contains('"') ||
                         value.Contains('\n') || value.Contains('\r');

        if (!mustQuote)
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
