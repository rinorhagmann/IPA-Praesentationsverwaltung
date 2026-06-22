using System.Text;

namespace IPA_Praesentationsverwaltung.Services.Infrastructure;

/// <summary>
/// Minimal, dependency-free CSV reader that understands quoted fields, escaped
/// quotes ("") and both comma and semicolon delimiters. Returns the raw fields
/// per line; interpretation of the columns is left to the calling service.
/// </summary>
public static class CsvParser
{
    /// <summary>
    /// Parses the stream into a list of rows, each row being a list of fields.
    /// Empty lines are skipped. The delimiter is auto-detected from the header.
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<string>> Parse(Stream csvStream)
    {
        ArgumentNullException.ThrowIfNull(csvStream);

        var rows = new List<IReadOnlyList<string>>();
        using var reader = new StreamReader(csvStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        string content = reader.ReadToEnd();
        if (string.IsNullOrWhiteSpace(content))
        {
            return rows;
        }

        char delimiter = DetectDelimiter(content);

        foreach (string rawLine in SplitLines(content))
        {
            if (string.IsNullOrWhiteSpace(rawLine))
            {
                continue;
            }

            rows.Add(ParseLine(rawLine, delimiter));
        }

        return rows;
    }

    /// <summary>Detects whether the file uses a semicolon or comma as delimiter.</summary>
    private static char DetectDelimiter(string content)
    {
        int firstLineEnd = content.IndexOfAny(['\r', '\n']);
        string header = firstLineEnd < 0 ? content : content[..firstLineEnd];

        // Excel on German locales typically exports semicolon separated files.
        return header.Count(c => c == ';') > header.Count(c => c == ',') ? ';' : ',';
    }

    private static IEnumerable<string> SplitLines(string content) =>
        content.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

    /// <summary>Splits a single CSV line into fields, honouring quoted sections.</summary>
    private static List<string> ParseLine(string line, char delimiter)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    // A doubled quote inside a quoted field is an escaped quote.
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else if (c == '"')
            {
                inQuotes = true;
            }
            else if (c == delimiter)
            {
                fields.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        fields.Add(current.ToString().Trim());
        return fields;
    }
}
