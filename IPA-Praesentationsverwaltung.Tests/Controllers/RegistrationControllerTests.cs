using IPA_Praesentationsverwaltung.Controllers;
using IPA_Praesentationsverwaltung.Data;
using IPA_Praesentationsverwaltung.Models.Domain;
using IPA_Praesentationsverwaltung.Models.ViewModels;
using IPA_Praesentationsverwaltung.Services;
using IPA_Praesentationsverwaltung.Services.Infrastructure;
using IPA_Praesentationsverwaltung.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IPA_Praesentationsverwaltung.Tests.Controllers;

public class RegistrationControllerTests
{
    private static RegistrationController CreateSut(ApplicationDbContext context, out FakeEmailSender mail)
    {
        mail = new FakeEmailSender();
        var hasher = new Pbkdf2PasswordHasher();
        return new RegistrationController(
            new RegistrationService(context, new AssignmentRuleService()),
            new StudentService(context, hasher),
            new PresentationService(context),
            new NotificationService(mail),
            NullLogger<RegistrationController>.Instance).WithContext(MvcTestHarness.Admin());
    }

    private static async Task<(int annaId, int beatId, int presA, int presB)> SeedAsync(ApplicationDbContext context)
    {
        var room = new Room { Name = "411" };
        var anna = new Student { Email = "anna@wgbs.ch", FirstName = "Anna", LastName = "Muster", PasswordHash = "x" };
        var beat = new Student { Email = "beat@wgbs.ch", FirstName = "Beat", LastName = "Berger", PasswordHash = "x" };
        var a = new Presentation { Topic = "A", StartsAt = new DateTime(2026, 9, 1, 9, 0, 0), Room = room, MaxObservers = 6 };
        var b = new Presentation { Topic = "B", StartsAt = new DateTime(2026, 9, 1, 10, 0, 0), Room = room, MaxObservers = 6 };
        context.AddRange(room, anna, beat, a, b);
        await context.SaveChangesAsync();
        return (anna.Id, beat.Id, a.Id, b.Id);
    }

    [Fact]
    public async Task Index_returns_view_with_registrations()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        (int annaId, _, int presA, _) = await SeedAsync(context);
        RegistrationController sut = CreateSut(context, out _);
        await new RegistrationService(context, new AssignmentRuleService()).CreateRegistrationAsync(annaId, presA);

        var view = Assert.IsType<ViewResult>(await sut.Index());
        var list = Assert.IsAssignableFrom<IReadOnlyList<Registration>>(view.Model);
        Assert.Single(list);
    }

    [Fact]
    public async Task Create_get_populates_dropdowns()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        await SeedAsync(context);
        RegistrationController sut = CreateSut(context, out _);

        var view = Assert.IsType<ViewResult>(await sut.Create());
        Assert.Equal("RegistrationForm", view.ViewName);
        var model = Assert.IsType<RegistrationFormViewModel>(view.Model);
        Assert.NotEmpty(model.StudentOptions);
        Assert.NotEmpty(model.PresentationOptions);
    }

    [Fact]
    public async Task Create_post_invalid_modelstate_repopulates_and_returns_form()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        await SeedAsync(context);
        RegistrationController sut = CreateSut(context, out _);
        sut.ModelState.AddModelError("StudentId", "required");

        var view = Assert.IsType<ViewResult>(await sut.Create(new RegistrationFormViewModel()));
        var model = Assert.IsType<RegistrationFormViewModel>(view.Model);
        Assert.NotEmpty(model.StudentOptions);
    }

    [Fact]
    public async Task Create_post_valid_creates_notifies_and_redirects()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        (int annaId, _, int presA, _) = await SeedAsync(context);
        RegistrationController sut = CreateSut(context, out FakeEmailSender mail);

        var redirect = Assert.IsType<RedirectToActionResult>(
            await sut.Create(new RegistrationFormViewModel { StudentId = annaId, PresentationId = presA }));

        Assert.Equal(nameof(RegistrationController.Index), redirect.ActionName);
        Assert.Equal(1, await context.Registrations.CountAsync());
        Assert.Single(mail.Sent);
    }

    [Fact]
    public async Task Create_post_rule_violation_returns_form_with_error()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        (int annaId, _, int presA, _) = await SeedAsync(context);
        RegistrationController sut = CreateSut(context, out _);
        await new RegistrationService(context, new AssignmentRuleService()).CreateRegistrationAsync(annaId, presA);

        // Same student + presentation again triggers a duplicate-selection rule failure.
        var view = Assert.IsType<ViewResult>(
            await sut.Create(new RegistrationFormViewModel { StudentId = annaId, PresentationId = presA }));

        Assert.Equal("RegistrationForm", view.ViewName);
        Assert.False(sut.ModelState.IsValid);
    }

    [Fact]
    public async Task Edit_get_returns_not_found_for_unknown_id()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        await SeedAsync(context);
        RegistrationController sut = CreateSut(context, out _);

        Assert.IsType<NotFoundResult>(await sut.Edit(9999));
    }

    [Fact]
    public async Task Edit_get_returns_populated_form()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        (int annaId, _, int presA, _) = await SeedAsync(context);
        RegistrationController sut = CreateSut(context, out _);
        await new RegistrationService(context, new AssignmentRuleService()).CreateRegistrationAsync(annaId, presA);
        Registration reg = await context.Registrations.FirstAsync();

        var view = Assert.IsType<ViewResult>(await sut.Edit(reg.Id));
        var model = Assert.IsType<RegistrationFormViewModel>(view.Model);
        Assert.Equal(annaId, model.StudentId);
        Assert.Equal(presA, model.PresentationId);
    }

    [Fact]
    public async Task Edit_post_moving_to_another_student_notifies_both()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        (int annaId, int beatId, int presA, int presB) = await SeedAsync(context);
        RegistrationController sut = CreateSut(context, out FakeEmailSender mail);
        await new RegistrationService(context, new AssignmentRuleService()).CreateRegistrationAsync(annaId, presA);
        Registration reg = await context.Registrations.FirstAsync();

        var redirect = Assert.IsType<RedirectToActionResult>(await sut.Edit(
            new RegistrationFormViewModel { Id = reg.Id, StudentId = beatId, PresentationId = presB }));

        Assert.Equal(nameof(RegistrationController.Index), redirect.ActionName);
        Assert.Equal(beatId, (await context.Registrations.FirstAsync()).StudentId);
        Assert.Equal(2, mail.Sent.Count); // new owner + previous owner
    }

    [Fact]
    public async Task Edit_post_invalid_modelstate_returns_form()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        await SeedAsync(context);
        RegistrationController sut = CreateSut(context, out _);
        sut.ModelState.AddModelError("StudentId", "required");

        var view = Assert.IsType<ViewResult>(await sut.Edit(new RegistrationFormViewModel()));
        Assert.Equal("RegistrationForm", view.ViewName);
    }

    [Fact]
    public async Task Delete_removes_notifies_and_redirects()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        (int annaId, _, int presA, _) = await SeedAsync(context);
        RegistrationController sut = CreateSut(context, out FakeEmailSender mail);
        await new RegistrationService(context, new AssignmentRuleService()).CreateRegistrationAsync(annaId, presA);
        Registration reg = await context.Registrations.FirstAsync();

        var redirect = Assert.IsType<RedirectToActionResult>(await sut.Delete(reg.Id));

        Assert.Equal(nameof(RegistrationController.Index), redirect.ActionName);
        Assert.Equal(0, await context.Registrations.CountAsync());
        Assert.Single(mail.Sent);
    }
}
