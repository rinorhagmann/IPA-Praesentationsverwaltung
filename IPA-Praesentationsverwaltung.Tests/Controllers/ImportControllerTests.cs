using System.Text;
using IPA_Praesentationsverwaltung.Controllers;
using IPA_Praesentationsverwaltung.Data;
using IPA_Praesentationsverwaltung.Models.Domain;
using IPA_Praesentationsverwaltung.Models.ViewModels;
using IPA_Praesentationsverwaltung.Services;
using IPA_Praesentationsverwaltung.Services.Infrastructure;
using IPA_Praesentationsverwaltung.Tests.TestSupport;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IPA_Praesentationsverwaltung.Tests.Controllers;

public class ImportControllerTests
{
    private static ImportController CreateSut(ApplicationDbContext context, out FakeEmailSender mail)
    {
        mail = new FakeEmailSender();
        var hasher = new Pbkdf2PasswordHasher();
        var studentService = new StudentService(context, hasher);
        var presentationService = new PresentationService(context);
        var csvImport = new CsvImportService(studentService, presentationService);
        return new ImportController(csvImport, studentService, new NotificationService(mail))
            .WithContext(MvcTestHarness.Admin());
    }

    private static IFormFile CsvFile(string content, string fileName = "data.csv")
    {
        byte[] bytes = Encoding.UTF8.GetBytes(content);
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "CsvFile", fileName);
    }

    [Fact]
    public void Index_returns_view()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        ImportController sut = CreateSut(context, out _);

        Assert.IsType<ViewResult>(sut.Index());
    }

    [Fact]
    public async Task ImportStudents_with_valid_file_imports_and_redirects()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        ImportController sut = CreateSut(context, out _);
        var model = new CsvImportViewModel { CsvFile = CsvFile("Anna;Muster;anna@wgbs.ch\nBeat;Berger;beat@wgbs.ch") };

        var redirect = Assert.IsType<RedirectToActionResult>(await sut.ImportStudents(model));
        Assert.Equal(nameof(ImportController.Index), redirect.ActionName);
        Assert.Equal(2, await context.Students.CountAsync());
        Assert.NotNull(sut.TempData["Success"]);
    }

    [Fact]
    public async Task ImportStudents_without_file_reports_error()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        ImportController sut = CreateSut(context, out _);

        var redirect = Assert.IsType<RedirectToActionResult>(await sut.ImportStudents(new CsvImportViewModel()));
        Assert.Equal(nameof(ImportController.Index), redirect.ActionName);
        Assert.NotNull(sut.TempData["Error"]);
        Assert.Equal(0, await context.Students.CountAsync());
    }

    [Fact]
    public async Task ImportStudents_rejects_non_csv_extension()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        ImportController sut = CreateSut(context, out _);
        var model = new CsvImportViewModel { CsvFile = CsvFile("Anna;Muster;anna@wgbs.ch", "students.txt") };

        Assert.IsType<RedirectToActionResult>(await sut.ImportStudents(model));
        Assert.NotNull(sut.TempData["Error"]);
        Assert.Equal(0, await context.Students.CountAsync());
    }

    [Fact]
    public async Task ImportPresentations_with_valid_file_imports()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        ImportController sut = CreateSut(context, out _);
        var csv = "Thema;Datum/Uhrzeit;Raum\nDigitalisierung;01.09.2026 09:00;411";
        var model = new CsvImportViewModel { CsvFile = CsvFile(csv) };

        var redirect = Assert.IsType<RedirectToActionResult>(await sut.ImportPresentations(model));
        Assert.Equal(nameof(ImportController.Index), redirect.ActionName);
        Assert.Equal(1, await context.Presentations.CountAsync());
    }

    [Fact]
    public async Task SendCredentials_mails_pending_students_and_marks_them_sent()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        ImportController sut = CreateSut(context, out FakeEmailSender mail);
        var studentService = new StudentService(context, new Pbkdf2PasswordHasher());
        await studentService.CreateStudentAsync(new Student { Email = "a@wgbs.ch", FirstName = "A", LastName = "A" });
        await studentService.CreateStudentAsync(new Student { Email = "b@wgbs.ch", FirstName = "B", LastName = "B" });

        var redirect = Assert.IsType<RedirectToActionResult>(await sut.SendCredentials());

        Assert.Equal(nameof(ImportController.Index), redirect.ActionName);
        Assert.Equal(2, mail.Sent.Count);
        Assert.All(await context.Students.ToListAsync(), s => Assert.True(s.InitialPasswordSent));
    }

    [Fact]
    public async Task SendCredentials_reports_when_nothing_to_send()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        ImportController sut = CreateSut(context, out FakeEmailSender mail);

        Assert.IsType<RedirectToActionResult>(await sut.SendCredentials());
        Assert.Empty(mail.Sent);
        Assert.NotNull(sut.TempData["Success"]);
    }
}
