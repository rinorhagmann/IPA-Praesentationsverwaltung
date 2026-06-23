using IPA_Praesentationsverwaltung.Controllers;
using IPA_Praesentationsverwaltung.Data;
using IPA_Praesentationsverwaltung.Models.Domain;
using IPA_Praesentationsverwaltung.Models.ViewModels;
using IPA_Praesentationsverwaltung.Services;
using IPA_Praesentationsverwaltung.Services.Infrastructure;
using IPA_Praesentationsverwaltung.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IPA_Praesentationsverwaltung.Tests.Controllers;

public class StudentControllerTests
{
    private static StudentController CreateSut(ApplicationDbContext context, int studentId, out FakeEmailSender mail)
    {
        mail = new FakeEmailSender();
        var hasher = new Pbkdf2PasswordHasher();
        return new StudentController(
                new StudentService(context, hasher),
                new PresentationService(context),
                new RegistrationService(context, new AssignmentRuleService()),
                new NotificationService(mail))
            .WithContext(MvcTestHarness.Student(id: studentId));
    }

    private static async Task<(int studentId, List<int> presentationIds)> SeedAsync(
        ApplicationDbContext context, int presentationCount = 3)
    {
        var room = new Room { Name = "411" };
        var student = new Student { Email = "anna@wgbs.ch", FirstName = "Anna", LastName = "Muster", PasswordHash = "x" };
        context.AddRange(room, student);
        await context.SaveChangesAsync();

        var ids = new List<int>();
        for (int i = 0; i < presentationCount; i++)
        {
            var presentation = new Presentation
            {
                Topic = $"Thema {i}",
                StartsAt = new DateTime(2026, 9, 1, 9 + i, 0, 0),
                Room = room,
                MaxObservers = 6
            };
            context.Presentations.Add(presentation);
            await context.SaveChangesAsync();
            ids.Add(presentation.Id);
        }

        return (student.Id, ids);
    }

    private static async Task RegisterAsync(ApplicationDbContext context, int studentId, params int[] presentationIds)
    {
        var service = new RegistrationService(context, new AssignmentRuleService());
        foreach (int id in presentationIds)
        {
            await service.CreateRegistrationAsync(studentId, id);
        }
    }

    [Fact]
    public void Index_redirects_to_presentations()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        StudentController sut = CreateSut(context, studentId: 1, out _);

        var redirect = Assert.IsType<RedirectToActionResult>(sut.Index());
        Assert.Equal(nameof(StudentController.Presentations), redirect.ActionName);
    }

    [Fact]
    public async Task Presentations_returns_selection_model()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        (int studentId, _) = await SeedAsync(context);
        StudentController sut = CreateSut(context, studentId, out _);

        var view = Assert.IsType<ViewResult>(await sut.Presentations());
        var model = Assert.IsType<StudentSelectionViewModel>(view.Model);
        Assert.Equal(3, model.AvailablePresentations.Count);
        Assert.Equal(Student.RequiredSelectionCount, model.RemainingSelections);
    }

    [Fact]
    public async Task Select_adds_registration_and_reports_success()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        (int studentId, List<int> presentations) = await SeedAsync(context);
        StudentController sut = CreateSut(context, studentId, out _);

        var redirect = Assert.IsType<RedirectToActionResult>(await sut.Select(presentations[0]));
        Assert.Equal(nameof(StudentController.Presentations), redirect.ActionName);
        Assert.Equal(1, await context.Registrations.CountAsync());
        Assert.NotNull(sut.TempData["Success"]);
    }

    [Fact]
    public async Task Select_is_blocked_after_confirmation()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        (int studentId, List<int> presentations) = await SeedAsync(context);
        Student student = await context.Students.FirstAsync();
        student.SelectionConfirmedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        StudentController sut = CreateSut(context, studentId, out _);

        Assert.IsType<RedirectToActionResult>(await sut.Select(presentations[0]));
        Assert.Equal(0, await context.Registrations.CountAsync());
        Assert.NotNull(sut.TempData["Error"]);
    }

    [Fact]
    public async Task Select_reports_rule_violation_as_error()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        (int studentId, List<int> presentations) = await SeedAsync(context);
        await RegisterAsync(context, studentId, presentations[0]);
        StudentController sut = CreateSut(context, studentId, out _);

        // Selecting the same presentation twice is rejected by the rule service.
        Assert.IsType<RedirectToActionResult>(await sut.Select(presentations[0]));
        Assert.Equal(1, await context.Registrations.CountAsync());
        Assert.NotNull(sut.TempData["Error"]);
    }

    [Fact]
    public async Task MySelection_lists_current_registrations()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        (int studentId, List<int> presentations) = await SeedAsync(context);
        await RegisterAsync(context, studentId, presentations[0]);
        StudentController sut = CreateSut(context, studentId, out _);

        var view = Assert.IsType<ViewResult>(await sut.MySelection());
        var model = Assert.IsType<StudentSelectionViewModel>(view.Model);
        Assert.Single(model.CurrentSelections);
    }

    [Fact]
    public async Task RemoveSelection_forbids_foreign_registration()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        (int studentId, List<int> presentations) = await SeedAsync(context);
        var other = new Student { Email = "b@wgbs.ch", FirstName = "B", LastName = "B", PasswordHash = "x" };
        context.Students.Add(other);
        await context.SaveChangesAsync();
        await RegisterAsync(context, other.Id, presentations[0]);
        Registration foreign = await context.Registrations.FirstAsync();
        StudentController sut = CreateSut(context, studentId, out _);

        Assert.IsType<ForbidResult>(await sut.RemoveSelection(foreign.Id));
        Assert.Equal(1, await context.Registrations.CountAsync());
    }

    [Fact]
    public async Task RemoveSelection_removes_own_registration()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        (int studentId, List<int> presentations) = await SeedAsync(context);
        await RegisterAsync(context, studentId, presentations[0]);
        Registration own = await context.Registrations.FirstAsync();
        StudentController sut = CreateSut(context, studentId, out _);

        var redirect = Assert.IsType<RedirectToActionResult>(await sut.RemoveSelection(own.Id, returnTo: "presentations"));
        Assert.Equal(nameof(StudentController.Presentations), redirect.ActionName);
        Assert.Equal(0, await context.Registrations.CountAsync());
    }

    [Fact]
    public async Task RemoveSelection_is_blocked_after_confirmation()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        (int studentId, List<int> presentations) = await SeedAsync(context);
        await RegisterAsync(context, studentId, presentations[0]);
        Registration own = await context.Registrations.FirstAsync();
        Student student = await context.Students.FirstAsync();
        student.SelectionConfirmedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        StudentController sut = CreateSut(context, studentId, out _);

        Assert.IsType<RedirectToActionResult>(await sut.RemoveSelection(own.Id));
        Assert.Equal(1, await context.Registrations.CountAsync());
        Assert.NotNull(sut.TempData["Error"]);
    }

    [Fact]
    public async Task Confirm_redirects_to_presentations_when_incomplete()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        (int studentId, _) = await SeedAsync(context);
        StudentController sut = CreateSut(context, studentId, out _);

        var redirect = Assert.IsType<RedirectToActionResult>(await sut.Confirm());
        Assert.Equal(nameof(StudentController.Presentations), redirect.ActionName);
        Assert.NotNull(sut.TempData["Error"]);
    }

    [Fact]
    public async Task Confirm_shows_view_when_selection_is_complete()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        (int studentId, List<int> presentations) = await SeedAsync(context);
        await RegisterAsync(context, studentId, presentations[0], presentations[2]);
        StudentController sut = CreateSut(context, studentId, out _);

        var view = Assert.IsType<ViewResult>(await sut.Confirm());
        var model = Assert.IsType<StudentSelectionViewModel>(view.Model);
        Assert.True(model.HasCompletedSelection);
    }

    [Fact]
    public async Task Confirm_redirects_to_my_selection_when_already_confirmed()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        (int studentId, List<int> presentations) = await SeedAsync(context);
        await RegisterAsync(context, studentId, presentations[0], presentations[2]);
        Student student = await context.Students.FirstAsync();
        student.SelectionConfirmedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        StudentController sut = CreateSut(context, studentId, out _);

        var redirect = Assert.IsType<RedirectToActionResult>(await sut.Confirm());
        Assert.Equal(nameof(StudentController.MySelection), redirect.ActionName);
    }

    [Fact]
    public async Task ConfirmSelection_marks_confirmed_sends_mail_and_redirects_to_success()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        (int studentId, List<int> presentations) = await SeedAsync(context);
        await RegisterAsync(context, studentId, presentations[0], presentations[2]);
        StudentController sut = CreateSut(context, studentId, out FakeEmailSender mail);

        var redirect = Assert.IsType<RedirectToActionResult>(await sut.ConfirmSelection());

        Assert.Equal(nameof(StudentController.Success), redirect.ActionName);
        Assert.True((await context.Students.FirstAsync()).IsSelectionConfirmed);
        Assert.Single(mail.Sent);
    }

    [Fact]
    public async Task ConfirmSelection_rejects_incomplete_selection()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        (int studentId, List<int> presentations) = await SeedAsync(context);
        await RegisterAsync(context, studentId, presentations[0]);
        StudentController sut = CreateSut(context, studentId, out _);

        var redirect = Assert.IsType<RedirectToActionResult>(await sut.ConfirmSelection());
        Assert.Equal(nameof(StudentController.Presentations), redirect.ActionName);
        Assert.False((await context.Students.FirstAsync()).IsSelectionConfirmed);
    }

    [Fact]
    public async Task ConfirmSelection_is_idempotent_when_already_confirmed()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        (int studentId, List<int> presentations) = await SeedAsync(context);
        await RegisterAsync(context, studentId, presentations[0], presentations[2]);
        Student student = await context.Students.FirstAsync();
        student.SelectionConfirmedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        StudentController sut = CreateSut(context, studentId, out FakeEmailSender mail);

        var redirect = Assert.IsType<RedirectToActionResult>(await sut.ConfirmSelection());
        Assert.Equal(nameof(StudentController.Success), redirect.ActionName);
        Assert.Empty(mail.Sent);
    }

    [Fact]
    public void Success_returns_view()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        StudentController sut = CreateSut(context, studentId: 1, out _);

        Assert.IsType<ViewResult>(sut.Success());
    }
}
