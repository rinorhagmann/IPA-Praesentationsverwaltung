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

public class AdminControllerTests
{
    private static AdminController CreateSut(ApplicationDbContext context)
    {
        var hasher = new Pbkdf2PasswordHasher();
        return new AdminController(
            new DashboardService(context),
            new SystemResetService(context),
            new AdminAccountService(context, hasher));
    }

    private static async Task SeedAdminsAsync(ApplicationDbContext context, int count)
    {
        var service = new AdminAccountService(context, new Pbkdf2PasswordHasher());
        for (int i = 0; i < count; i++)
        {
            await service.CreateAdminAsync(
                new Admin { Email = $"admin{i}@wgbs.ch", FirstName = "Sys", LastName = $"Admin{i}" }, "x");
        }
    }

    [Fact]
    public async Task Dashboard_returns_view_with_statistics()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        AdminController sut = CreateSut(context).WithContext(MvcTestHarness.Admin());

        var view = Assert.IsType<ViewResult>(await sut.Dashboard());
        Assert.IsType<DashboardViewModel>(view.Model);
    }

    [Fact]
    public async Task ResetSystem_post_clears_data_and_redirects_to_dashboard()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        context.Students.Add(new Student { Email = "a@wgbs.ch", FirstName = "A", LastName = "A", PasswordHash = "x" });
        await context.SaveChangesAsync();
        AdminController sut = CreateSut(context).WithContext(MvcTestHarness.Admin());

        var redirect = Assert.IsType<RedirectToActionResult>(await sut.ResetSystem(confirmed: true));
        Assert.Equal(nameof(AdminController.Dashboard), redirect.ActionName);
        Assert.Equal(0, await context.Students.CountAsync());
        Assert.NotNull(sut.TempData["Success"]);
    }

    [Fact]
    public async Task ManageAdmins_returns_view_with_admin_list()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        await SeedAdminsAsync(context, 2);
        AdminController sut = CreateSut(context).WithContext(MvcTestHarness.Admin());

        var view = Assert.IsType<ViewResult>(await sut.ManageAdmins());
        var admins = Assert.IsAssignableFrom<IReadOnlyList<Admin>>(view.Model);
        Assert.Equal(2, admins.Count);
    }

    [Fact]
    public void CreateAdmin_get_returns_empty_form()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        AdminController sut = CreateSut(context).WithContext(MvcTestHarness.Admin());

        var view = Assert.IsType<ViewResult>(sut.CreateAdmin());
        Assert.Equal("AdminForm", view.ViewName);
        Assert.IsType<AdminFormViewModel>(view.Model);
    }

    [Fact]
    public async Task CreateAdmin_post_without_password_returns_form_with_error()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        AdminController sut = CreateSut(context).WithContext(MvcTestHarness.Admin());
        var model = new AdminFormViewModel { Email = "new@wgbs.ch", FirstName = "Neu", LastName = "Admin" };

        var view = Assert.IsType<ViewResult>(await sut.CreateAdmin(model));
        Assert.Equal("AdminForm", view.ViewName);
        Assert.False(sut.ModelState.IsValid);
        Assert.Equal(0, await context.Admins.CountAsync());
    }

    [Fact]
    public async Task CreateAdmin_post_valid_creates_and_redirects()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        AdminController sut = CreateSut(context).WithContext(MvcTestHarness.Admin());
        var model = new AdminFormViewModel
        {
            Email = "new@wgbs.ch", FirstName = "Neu", LastName = "Admin", Password = "Secret123!"
        };

        var redirect = Assert.IsType<RedirectToActionResult>(await sut.CreateAdmin(model));
        Assert.Equal(nameof(AdminController.ManageAdmins), redirect.ActionName);
        Assert.Equal(1, await context.Admins.CountAsync());
    }

    [Fact]
    public async Task EditAdmin_get_returns_not_found_for_unknown_id()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        AdminController sut = CreateSut(context).WithContext(MvcTestHarness.Admin());

        Assert.IsType<NotFoundResult>(await sut.EditAdmin(9999));
    }

    [Fact]
    public async Task EditAdmin_get_returns_populated_form()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        await SeedAdminsAsync(context, 1);
        Admin admin = await context.Admins.FirstAsync();
        AdminController sut = CreateSut(context).WithContext(MvcTestHarness.Admin());

        var view = Assert.IsType<ViewResult>(await sut.EditAdmin(admin.Id));
        var model = Assert.IsType<AdminFormViewModel>(view.Model);
        Assert.Equal(admin.Id, model.Id);
        Assert.Equal(admin.Email, model.Email);
    }

    [Fact]
    public async Task EditAdmin_post_invalid_modelstate_returns_form()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        AdminController sut = CreateSut(context).WithContext(MvcTestHarness.Admin());
        sut.ModelState.AddModelError("Email", "required");

        var view = Assert.IsType<ViewResult>(await sut.EditAdmin(new AdminFormViewModel()));
        Assert.Equal("AdminForm", view.ViewName);
    }

    [Fact]
    public async Task EditAdmin_post_valid_updates_and_redirects()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        await SeedAdminsAsync(context, 1);
        Admin admin = await context.Admins.FirstAsync();
        AdminController sut = CreateSut(context).WithContext(MvcTestHarness.Admin());

        var model = new AdminFormViewModel
        {
            Id = admin.Id, Email = "updated@wgbs.ch", FirstName = "Up", LastName = "Dated"
        };
        var redirect = Assert.IsType<RedirectToActionResult>(await sut.EditAdmin(model));
        Assert.Equal(nameof(AdminController.ManageAdmins), redirect.ActionName);
        Assert.Equal("updated@wgbs.ch", (await context.Admins.FirstAsync()).Email);
    }

    [Fact]
    public async Task DeleteAdmin_refuses_to_delete_own_account()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        await SeedAdminsAsync(context, 2);
        Admin self = await context.Admins.FirstAsync();
        AdminController sut = CreateSut(context).WithContext(MvcTestHarness.Admin(id: self.Id));

        var redirect = Assert.IsType<RedirectToActionResult>(await sut.DeleteAdmin(self.Id));
        Assert.Equal(nameof(AdminController.ManageAdmins), redirect.ActionName);
        Assert.NotNull(sut.TempData["Error"]);
        Assert.Equal(2, await context.Admins.CountAsync());
    }

    [Fact]
    public async Task DeleteAdmin_removes_other_account()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        await SeedAdminsAsync(context, 2);
        Admin other = await context.Admins.OrderBy(a => a.Id).LastAsync();
        AdminController sut = CreateSut(context).WithContext(MvcTestHarness.Admin(id: 99999));

        var redirect = Assert.IsType<RedirectToActionResult>(await sut.DeleteAdmin(other.Id));
        Assert.Equal(nameof(AdminController.ManageAdmins), redirect.ActionName);
        Assert.NotNull(sut.TempData["Success"]);
        Assert.Equal(1, await context.Admins.CountAsync());
    }
}
