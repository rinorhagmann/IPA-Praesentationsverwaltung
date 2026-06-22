using System.Globalization;
using System.Text;

namespace IPA_Praesentationsverwaltung.Services.Infrastructure;

/// <summary>
/// A small, dependency-free PDF generator capable of laying out left-aligned
/// text in regular and bold Helvetica across multiple A4 pages. It produces a
/// valid PDF 1.4 document and is intentionally limited to the needs of the
/// printable observer/room lists, so no third-party PDF library is required.
/// </summary>
public sealed class SimplePdfWriter
{
    // A4 dimensions and layout constants, in PDF points (1/72 inch).
    private const double PageWidth = 595;
    private const double PageHeight = 842;
    private const double Margin = 50;
    private const double LineHeight = 16;

    // PDF text is written using single-byte WinAnsi encoding (Latin-1), which
    // covers the German umlauts used in the reports.
    private static readonly Encoding PdfEncoding = Encoding.Latin1;

    private readonly List<TextLine> _lines = new();

    private readonly record struct TextLine(string Text, double FontSize, bool Bold);

    public void AddHeading(string text) => _lines.Add(new TextLine(text, 18, Bold: true));

    public void AddSubHeading(string text) => _lines.Add(new TextLine(text, 13, Bold: true));

    public void AddLine(string text) => _lines.Add(new TextLine(text, 11, Bold: false));

    public void AddBlankLine() => _lines.Add(new TextLine(string.Empty, 11, Bold: false));

    /// <summary>Renders the accumulated lines and returns the PDF document bytes.</summary>
    public byte[] Build()
    {
        IReadOnlyList<string> pageContents = BuildPageContentStreams();

        // Object layout:
        //   1 = Catalog, 2 = Pages, 3 = Helvetica, 4 = Helvetica-Bold,
        //   then per page: a Page object and its Contents stream.
        var objects = new List<string>();
        var pageObjectNumbers = new List<int>();
        var contentObjectNumbers = new List<int>();

        int firstPageObjectNumber = 5;
        for (int i = 0; i < pageContents.Count; i++)
        {
            pageObjectNumbers.Add(firstPageObjectNumber + (i * 2));
            contentObjectNumbers.Add(firstPageObjectNumber + (i * 2) + 1);
        }

        string kids = string.Join(' ', pageObjectNumbers.Select(n => $"{n} 0 R"));

        objects.Add($"<< /Type /Catalog /Pages 2 0 R >>");
        objects.Add($"<< /Type /Pages /Kids [{kids}] /Count {pageContents.Count} >>");
        objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>");
        objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>");

        for (int i = 0; i < pageContents.Count; i++)
        {
            objects.Add(
                $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {Fmt(PageWidth)} {Fmt(PageHeight)}] " +
                $"/Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {contentObjectNumbers[i]} 0 R >>");

            string content = pageContents[i];
            objects.Add($"<< /Length {PdfEncoding.GetByteCount(content)} >>\nstream\n{content}\nendstream");
        }

        return Assemble(objects);
    }

    /// <summary>Lays the lines out into one or more page content streams.</summary>
    private IReadOnlyList<string> BuildPageContentStreams()
    {
        var pages = new List<string>();
        var current = new StringBuilder();
        double y = PageHeight - Margin;

        foreach (TextLine line in _lines)
        {
            if (y < Margin)
            {
                pages.Add(current.ToString());
                current = new StringBuilder();
                y = PageHeight - Margin;
            }

            if (!string.IsNullOrEmpty(line.Text))
            {
                string font = line.Bold ? "/F2" : "/F1";
                current.Append("BT ")
                       .Append(font).Append(' ').Append(Fmt(line.FontSize)).Append(" Tf ")
                       .Append("1 0 0 1 ").Append(Fmt(Margin)).Append(' ').Append(Fmt(y)).Append(" Tm ")
                       .Append('(').Append(EscapePdfText(line.Text)).Append(") Tj ET\n");
            }

            y -= LineHeight;
        }

        pages.Add(current.ToString());
        return pages;
    }

    /// <summary>Concatenates the objects, writes the cross-reference table and trailer.</summary>
    private static byte[] Assemble(List<string> objects)
    {
        using var stream = new MemoryStream();

        void Write(string text)
        {
            byte[] bytes = PdfEncoding.GetBytes(text);
            stream.Write(bytes, 0, bytes.Length);
        }

        Write("%PDF-1.4\n");

        var offsets = new long[objects.Count + 1];
        for (int i = 0; i < objects.Count; i++)
        {
            offsets[i + 1] = stream.Length;
            Write($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
        }

        long xrefOffset = stream.Length;
        Write($"xref\n0 {objects.Count + 1}\n");
        Write("0000000000 65535 f \n");
        for (int i = 1; i <= objects.Count; i++)
        {
            Write($"{offsets[i]:D10} 00000 n \n");
        }

        Write($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF");

        return stream.ToArray();
    }

    /// <summary>Escapes characters that are special inside a PDF literal string.</summary>
    private static string EscapePdfText(string text) => text
        .Replace("\\", "\\\\")
        .Replace("(", "\\(")
        .Replace(")", "\\)");

    private static string Fmt(double value) =>
        value.ToString("0.###", CultureInfo.InvariantCulture);
}
