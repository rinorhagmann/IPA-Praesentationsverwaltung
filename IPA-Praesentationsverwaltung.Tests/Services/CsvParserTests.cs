using System.Text;
using IPA_Praesentationsverwaltung.Services.Infrastructure;
using Xunit;

namespace IPA_Praesentationsverwaltung.Tests.Services;

public class CsvParserTests
{
    private static Stream ToStream(string content) => new MemoryStream(Encoding.UTF8.GetBytes(content));

    [Fact]
    public void Parse_handles_comma_separated_rows()
    {
        var rows = CsvParser.Parse(ToStream("Anna,Muster,anna@wgbs.ch\nBeat,Beispiel,beat@wgbs.ch"));

        Assert.Equal(2, rows.Count);
        Assert.Equal(new[] { "Anna", "Muster", "anna@wgbs.ch" }, rows[0]);
    }

    [Fact]
    public void Parse_detects_semicolon_delimiter()
    {
        var rows = CsvParser.Parse(ToStream("Thema;01.01.2026 09:00;411"));

        Assert.Single(rows);
        Assert.Equal(new[] { "Thema", "01.01.2026 09:00", "411" }, rows[0]);
    }

    [Fact]
    public void Parse_supports_quoted_fields_with_delimiters_and_escaped_quotes()
    {
        var rows = CsvParser.Parse(ToStream("\"Müller, Hans\",\"Sagt \"\"Hallo\"\"\",x@y.ch"));

        Assert.Single(rows);
        Assert.Equal("Müller, Hans", rows[0][0]);
        Assert.Equal("Sagt \"Hallo\"", rows[0][1]);
        Assert.Equal("x@y.ch", rows[0][2]);
    }

    [Fact]
    public void Parse_skips_blank_lines()
    {
        var rows = CsvParser.Parse(ToStream("a,b\n\n   \nc,d"));
        Assert.Equal(2, rows.Count);
    }
}
