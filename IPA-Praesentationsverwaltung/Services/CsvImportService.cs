using System.ComponentModel.DataAnnotations;
using System.Globalization;
using IPA_Praesentationsverwaltung.Models.Domain;
using IPA_Praesentationsverwaltung.Models.Dtos;
using IPA_Praesentationsverwaltung.Services.Abstractions;
using IPA_Praesentationsverwaltung.Services.Infrastructure;

namespace IPA_Praesentationsverwaltung.Services;

/// <summary>
/// Parses uploaded CSV files into domain entities and delegates persistence to
/// the student and presentation services. Each row is validated independently so
/// a single malformed line does not abort the whole import.
/// </summary>
public sealed class CsvImportService : ICsvImportService
{
    // Accepted date/time formats for the presentation import.
    private static readonly string[] DateTimeFormats =
    {
        "dd.MM.yyyy HH:mm", "dd.MM.yyyy H:mm", "dd.MM.yyyy HH:mm:ss",
        "yyyy-MM-dd HH:mm", "yyyy-MM-ddTHH:mm", "dd.MM.yy HH:mm"
    };

    private static readonly string[] DateFormats = { "dd.MM.yyyy", "yyyy-MM-dd", "dd.MM.yy" };
    private static readonly string[] TimeFormats = { "HH:mm", "H:mm", "HH:mm:ss" };

    private readonly IStudentService _studentService;
    private readonly IPresentationService _presentationService;

    public CsvImportService(IStudentService studentService, IPresentationService presentationService)
    {
        _studentService = studentService;
        _presentationService = presentationService;
    }

    public async Task<ImportResult> ImportStudentsAsync(Stream csvFile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(csvFile);

        IReadOnlyList<IReadOnlyList<string>> rows = CsvParser.Parse(csvFile);
        var errors = new List<string>();
        int imported = 0;

        // Track e-mails already present in the database and within the file to
        // reject duplicates before hitting the unique constraint.
        var knownEmails = (await _studentService.GetAllStudentsAsync(cancellationToken))
            .Select(s => s.Email.ToLowerInvariant())
            .ToHashSet();

        for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            IReadOnlyList<string> fields = rows[rowIndex];

            // Skip an optional header row (first row whose e-mail column is not an address).
            if (rowIndex == 0 && IsStudentHeader(fields))
            {
                continue;
            }

            StudentCsvRow? parsed = TryParseStudentRow(fields, rowIndex + 1, errors);
            if (parsed is null)
            {
                continue;
            }

            string email = parsed.Email.ToLowerInvariant();
            if (!knownEmails.Add(email))
            {
                errors.Add($"Zeile {rowIndex + 1}: E-Mail '{parsed.Email}' ist doppelt und wurde übersprungen.");
                continue;
            }

            var student = new Student
            {
                FirstName = parsed.FirstName,
                LastName = parsed.LastName,
                Email = parsed.Email
            };

            await _studentService.CreateStudentAsync(student, plainPassword: null, cancellationToken);
            imported++;
        }

        return new ImportResult { ImportedCount = imported, Errors = errors };
    }

    public async Task<ImportResult> ImportPresentationsAsync(Stream csvFile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(csvFile);

        IReadOnlyList<IReadOnlyList<string>> rows = CsvParser.Parse(csvFile);
        var errors = new List<string>();
        int imported = 0;

        for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            IReadOnlyList<string> fields = rows[rowIndex];

            if (rowIndex == 0 && IsPresentationHeader(fields))
            {
                continue;
            }

            PresentationCsvRow? parsed = TryParsePresentationRow(fields, rowIndex + 1, errors);
            if (parsed is null)
            {
                continue;
            }

            var presentation = new Presentation
            {
                Topic = parsed.Topic,
                StartsAt = parsed.StartsAt,
                MaxObservers = Presentation.DefaultMaxObservers
            };

            await _presentationService.CreatePresentationAsync(presentation, parsed.RoomName, cancellationToken);
            imported++;
        }

        return new ImportResult { ImportedCount = imported, Errors = errors };
    }

    private static StudentCsvRow? TryParseStudentRow(IReadOnlyList<string> fields, int line, List<string> errors)
    {
        if (fields.Count < 3)
        {
            errors.Add($"Zeile {line}: Erwartet werden Vorname, Nachname und E-Mail.");
            return null;
        }

        string firstName = fields[0];
        string lastName = fields[1];
        string email = fields[2];

        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
        {
            errors.Add($"Zeile {line}: Vor- und Nachname dürfen nicht leer sein.");
            return null;
        }

        if (!IsValidEmail(email))
        {
            errors.Add($"Zeile {line}: '{email}' ist keine gültige E-Mail-Adresse.");
            return null;
        }

        return new StudentCsvRow(firstName, lastName, email);
    }

    private static PresentationCsvRow? TryParsePresentationRow(IReadOnlyList<string> fields, int line, List<string> errors)
    {
        if (fields.Count < 3)
        {
            errors.Add($"Zeile {line}: Erwartet werden Thema, Datum/Uhrzeit und Raum.");
            return null;
        }

        string topic = fields[0];
        if (string.IsNullOrWhiteSpace(topic))
        {
            errors.Add($"Zeile {line}: Das Thema darf nicht leer sein.");
            return null;
        }

        // The date/time may be a single column or split into date + time columns.
        DateTime? startsAt;
        string roomName;
        if (fields.Count >= 4 && TryParseSplitDateTime(fields[1], fields[2], out DateTime split))
        {
            startsAt = split;
            roomName = fields[3];
        }
        else if (TryParseDateTime(fields[1], out DateTime combined))
        {
            startsAt = combined;
            roomName = fields[2];
        }
        else
        {
            errors.Add($"Zeile {line}: '{fields[1]}' konnte nicht als Datum/Uhrzeit gelesen werden.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(roomName))
        {
            errors.Add($"Zeile {line}: Der Raum darf nicht leer sein.");
            return null;
        }

        return new PresentationCsvRow(topic, startsAt.Value, roomName);
    }

    private static bool TryParseDateTime(string value, out DateTime result) =>
        DateTime.TryParseExact(value.Trim(), DateTimeFormats,
            CultureInfo.InvariantCulture, DateTimeStyles.None, out result)
        || DateTime.TryParse(value.Trim(), CultureInfo.GetCultureInfo("de-CH"),
            DateTimeStyles.None, out result);

    private static bool TryParseSplitDateTime(string datePart, string timePart, out DateTime result)
    {
        result = default;
        if (DateTime.TryParseExact(datePart.Trim(), DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date)
            && DateTime.TryParseExact(timePart.Trim(), TimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime time))
        {
            result = date.Date.Add(time.TimeOfDay);
            return true;
        }

        return false;
    }

    private static bool IsValidEmail(string email) =>
        !string.IsNullOrWhiteSpace(email) && new EmailAddressAttribute().IsValid(email.Trim());

    // A header row is assumed when the e-mail column does not contain an address.
    private static bool IsStudentHeader(IReadOnlyList<string> fields) =>
        fields.Count < 3 || !IsValidEmail(fields[2]);

    // A header row is assumed when neither candidate column parses as a date/time.
    private static bool IsPresentationHeader(IReadOnlyList<string> fields) =>
        fields.Count < 3 || (!TryParseDateTime(fields[1], out _) &&
                             !(fields.Count >= 4 && TryParseSplitDateTime(fields[1], fields[2], out _)));
}
