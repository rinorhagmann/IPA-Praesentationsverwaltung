using IPA_Praesentationsverwaltung.Controllers;
using IPA_Praesentationsverwaltung.Data;
using IPA_Praesentationsverwaltung.Models.Domain;
using IPA_Praesentationsverwaltung.Models.Dtos;
using IPA_Praesentationsverwaltung.Services;
using IPA_Praesentationsverwaltung.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace IPA_Praesentationsverwaltung.Tests.Controllers;

public class ExportControllerTests
{
    private static async Task<ExportController> CreateSutAsync(ApplicationDbContext context)
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

        return new ExportController(new ExportService(context)).WithContext(MvcTestHarness.Admin());
    }

    [Fact]
    public async Task Index_returns_view()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        ExportController sut = await CreateSutAsync(context);

        Assert.IsType<ViewResult>(sut.Index());
    }

    [Fact]
    public async Task PrintRooms_returns_print_list_view()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        ExportController sut = await CreateSutAsync(context);

        var view = Assert.IsType<ViewResult>(await sut.PrintRooms());
        Assert.Equal("PrintList", view.ViewName);
        Assert.IsType<RoomObserverList>(view.Model);
        Assert.Equal("Raumlisten", sut.ViewData["Title"]);
    }

    [Fact]
    public async Task PrintObservers_returns_print_list_view()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        ExportController sut = await CreateSutAsync(context);

        var view = Assert.IsType<ViewResult>(await sut.PrintObservers());
        Assert.Equal("PrintList", view.ViewName);
        Assert.Equal("Zuseherlisten", sut.ViewData["Title"]);
    }

    [Fact]
    public async Task RoomsCsv_returns_csv_file()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        ExportController sut = await CreateSutAsync(context);

        var file = Assert.IsType<FileContentResult>(await sut.RoomsCsv());
        Assert.Equal("text/csv", file.ContentType);
        Assert.Equal("raumlisten.csv", file.FileDownloadName);
        Assert.NotEmpty(file.FileContents);
    }

    [Fact]
    public async Task RoomsPdf_returns_pdf_file()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        ExportController sut = await CreateSutAsync(context);

        var file = Assert.IsType<FileContentResult>(await sut.RoomsPdf());
        Assert.Equal("application/pdf", file.ContentType);
        Assert.Equal("raumlisten.pdf", file.FileDownloadName);
    }

    [Fact]
    public async Task ObserversCsv_returns_csv_file()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        ExportController sut = await CreateSutAsync(context);

        var file = Assert.IsType<FileContentResult>(await sut.ObserversCsv());
        Assert.Equal("text/csv", file.ContentType);
        Assert.Equal("zuseherlisten.csv", file.FileDownloadName);
    }

    [Fact]
    public async Task ObserversPdf_returns_pdf_file()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        ExportController sut = await CreateSutAsync(context);

        var file = Assert.IsType<FileContentResult>(await sut.ObserversPdf());
        Assert.Equal("application/pdf", file.ContentType);
        Assert.Equal("zuseherlisten.pdf", file.FileDownloadName);
    }
}
