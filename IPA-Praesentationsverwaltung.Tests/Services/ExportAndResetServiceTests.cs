using System.Text;
using IPA_Praesentationsverwaltung.Data;
using IPA_Praesentationsverwaltung.Models.Domain;
using IPA_Praesentationsverwaltung.Models.Dtos;
using IPA_Praesentationsverwaltung.Services;
using IPA_Praesentationsverwaltung.Services.Abstractions;
using IPA_Praesentationsverwaltung.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IPA_Praesentationsverwaltung.Tests.Services;

public class ExportAndResetServiceTests
{
    private static async Task SeedScenarioAsync(ApplicationDbContext context)
    {
        var room = new Room { Name = "411" };
        var student = new Student { Email = "anna@wgbs.ch", FirstName = "Anna", LastName = "Muster", PasswordHash = "x" };
        var presentation = new Presentation
        {
            Topic = "Digitalisierung", StartsAt = new DateTime(2026, 9, 1, 9, 0, 0), Room = room, MaxObservers = 6
        };
        context.AddRange(room, student, presentation);
        await context.SaveChangesAsync();

        context.Registrations.Add(new Registration { StudentId = student.Id, PresentationId = presentation.Id });
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task ExportAsCsv_contains_header_and_observer_name()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        await SeedScenarioAsync(context);
        var sut = new ExportService(context);

        RoomObserverList list = await sut.CreatePrintListAsync(ListOrder.ByRoom);
        string csv = Encoding.UTF8.GetString(sut.ExportAsCsv(list));

        Assert.Contains("Raum;Datum;Uhrzeit;Thema;Zuseher", csv);
        Assert.Contains("Muster Anna", csv);
        Assert.Contains("Digitalisierung", csv);
    }

    [Fact]
    public async Task ExportAsPdf_produces_a_valid_pdf_document()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        await SeedScenarioAsync(context);
        var sut = new ExportService(context);

        RoomObserverList list = await sut.CreatePrintListAsync(ListOrder.ByRoom);
        byte[] pdf = sut.ExportAsPdf(list, "Raumlisten");

        string header = Encoding.Latin1.GetString(pdf, 0, 5);
        Assert.Equal("%PDF-", header);
        Assert.EndsWith("%%EOF", Encoding.Latin1.GetString(pdf));
    }

    [Fact]
    public async Task ResetSchoolYear_clears_year_data_but_keeps_admins()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        await SeedScenarioAsync(context);
        context.Admins.Add(new Admin
        {
            Email = "admin@wgbs.ch", FirstName = "Sys", LastName = "Admin", PasswordHash = "x"
        });
        await context.SaveChangesAsync();

        var sut = new SystemResetService(context);
        await sut.ResetSchoolYearAsync();

        Assert.Equal(0, await context.Registrations.CountAsync());
        Assert.Equal(0, await context.Presentations.CountAsync());
        Assert.Equal(0, await context.Students.CountAsync());
        Assert.Equal(0, await context.Rooms.CountAsync());
        Assert.Equal(1, await context.Admins.CountAsync()); // Admin survives the reset.
    }
}
