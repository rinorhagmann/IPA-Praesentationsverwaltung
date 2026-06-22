using System.Text;
using IPA_Praesentationsverwaltung.Data;
using IPA_Praesentationsverwaltung.Models.Dtos;
using IPA_Praesentationsverwaltung.Services;
using IPA_Praesentationsverwaltung.Services.Infrastructure;
using IPA_Praesentationsverwaltung.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IPA_Praesentationsverwaltung.Tests.Services;

public class CsvImportServiceTests
{
    private static CsvImportService CreateSut(ApplicationDbContext context)
    {
        var hasher = new Pbkdf2PasswordHasher();
        var studentService = new StudentService(context, hasher);
        var presentationService = new PresentationService(context);
        return new CsvImportService(studentService, presentationService);
    }

    private static Stream ToStream(string content) => new MemoryStream(Encoding.UTF8.GetBytes(content));

    [Fact]
    public async Task ImportStudents_imports_valid_rows_and_skips_header()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        CsvImportService sut = CreateSut(context);

        string csv = "Vorname;Nachname;E-Mail\nAnna;Meier;anna@wgbs.ch\nBeat;Müller;beat@wgbs.ch";
        ImportResult result = await sut.ImportStudentsAsync(ToStream(csv));

        Assert.Equal(2, result.ImportedCount);
        Assert.False(result.HasErrors);
        Assert.Equal(2, await context.Students.CountAsync());
    }

    [Fact]
    public async Task ImportStudents_reports_invalid_email_and_duplicates()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        CsvImportService sut = CreateSut(context);

        string csv = "Anna;Meier;anna@wgbs.ch\nBeat;Müller;not-an-email\nClara;Meier;anna@wgbs.ch";
        ImportResult result = await sut.ImportStudentsAsync(ToStream(csv));

        Assert.Equal(1, result.ImportedCount);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public async Task ImportStudents_stores_hashed_passwords_only()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        CsvImportService sut = CreateSut(context);

        await sut.ImportStudentsAsync(ToStream("Anna;Meier;anna@wgbs.ch"));

        var student = await context.Students.SingleAsync();
        Assert.StartsWith("100000.", student.PasswordHash); // PBKDF2 format, not plaintext.
        Assert.False(student.InitialPasswordSent);
    }

    [Fact]
    public async Task ImportPresentations_creates_presentations_and_rooms()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        CsvImportService sut = CreateSut(context);

        string csv = "Thema;Datum/Uhrzeit;Raum\n" +
                     "Digitalisierung;01.09.2026 09:00;411\n" +
                     "Nachhaltigkeit;01.09.2026 09:00;412\n" +
                     "KI im Marketing;01.09.2026 09:30;411";
        ImportResult result = await sut.ImportPresentationsAsync(ToStream(csv));

        Assert.Equal(3, result.ImportedCount);
        Assert.Equal(3, await context.Presentations.CountAsync());
        Assert.Equal(2, await context.Rooms.CountAsync()); // 411 reused, 412 new.
    }

    [Fact]
    public async Task ImportPresentations_reports_unparseable_date()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        CsvImportService sut = CreateSut(context);

        string csv = "Thema;Datum/Uhrzeit;Raum\nGutes Thema;kein-datum;411";
        ImportResult result = await sut.ImportPresentationsAsync(ToStream(csv));

        Assert.Equal(0, result.ImportedCount);
        Assert.True(result.HasErrors);
    }
}
