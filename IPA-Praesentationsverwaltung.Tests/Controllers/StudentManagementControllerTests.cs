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

public class StudentManagementControllerTests
{
    private static StudentManagementController CreateSut(ApplicationDbContext context) =>
        new StudentManagementController(new StudentService(context, new Pbkdf2PasswordHasher()))
            .WithContext(MvcTestHarness.Admin());

    private static StudentFormViewModel ValidForm(int id = 0) => new()
    {
        Id = id, Email = "anna@wgbs.ch", FirstName = "Anna", LastName = "Muster"
    };

    [Fact]
    public async Task Index_returns_view_with_students()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        StudentManagementController sut = CreateSut(context);
        await sut.Create(ValidForm());

        var view = Assert.IsType<ViewResult>(await sut.Index());
        var list = Assert.IsAssignableFrom<IReadOnlyList<Student>>(view.Model);
        Assert.Single(list);
    }

    [Fact]
    public void Create_get_returns_empty_form()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        StudentManagementController sut = CreateSut(context);

        var view = Assert.IsType<ViewResult>(sut.Create());
        Assert.Equal("StudentForm", view.ViewName);
        Assert.IsType<StudentFormViewModel>(view.Model);
    }

    [Fact]
    public async Task Create_post_invalid_modelstate_returns_form()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        StudentManagementController sut = CreateSut(context);
        sut.ModelState.AddModelError("Email", "required");

        var view = Assert.IsType<ViewResult>(await sut.Create(ValidForm()));
        Assert.Equal("StudentForm", view.ViewName);
        Assert.Equal(0, await context.Students.CountAsync());
    }

    [Fact]
    public async Task Create_post_valid_creates_and_redirects_with_password_in_tempdata()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        StudentManagementController sut = CreateSut(context);

        var redirect = Assert.IsType<RedirectToActionResult>(await sut.Create(ValidForm()));
        Assert.Equal(nameof(StudentManagementController.Index), redirect.ActionName);
        Assert.Equal(1, await context.Students.CountAsync());
        Assert.NotNull(sut.TempData["Success"]);
    }

    [Fact]
    public async Task Edit_get_returns_not_found_for_unknown_id()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        StudentManagementController sut = CreateSut(context);

        Assert.IsType<NotFoundResult>(await sut.Edit(9999));
    }

    [Fact]
    public async Task Edit_get_returns_populated_form()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        StudentManagementController sut = CreateSut(context);
        await sut.Create(ValidForm());
        Student created = await context.Students.FirstAsync();

        var view = Assert.IsType<ViewResult>(await sut.Edit(created.Id));
        var model = Assert.IsType<StudentFormViewModel>(view.Model);
        Assert.Equal("anna@wgbs.ch", model.Email);
    }

    [Fact]
    public async Task Edit_post_valid_updates_and_redirects()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        StudentManagementController sut = CreateSut(context);
        await sut.Create(ValidForm());
        Student created = await context.Students.FirstAsync();

        var form = ValidForm(created.Id);
        form.LastName = "Geändert";
        var redirect = Assert.IsType<RedirectToActionResult>(await sut.Edit(form));
        Assert.Equal(nameof(StudentManagementController.Index), redirect.ActionName);
        Assert.Equal("Geändert", (await context.Students.FirstAsync()).LastName);
    }

    [Fact]
    public async Task Delete_removes_and_redirects()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        StudentManagementController sut = CreateSut(context);
        await sut.Create(ValidForm());
        Student created = await context.Students.FirstAsync();

        var redirect = Assert.IsType<RedirectToActionResult>(await sut.Delete(created.Id));
        Assert.Equal(nameof(StudentManagementController.Index), redirect.ActionName);
        Assert.Equal(0, await context.Students.CountAsync());
    }
}
