using IPA_Praesentationsverwaltung.Controllers;
using IPA_Praesentationsverwaltung.Data;
using IPA_Praesentationsverwaltung.Models.Domain;
using IPA_Praesentationsverwaltung.Models.ViewModels;
using IPA_Praesentationsverwaltung.Services;
using IPA_Praesentationsverwaltung.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IPA_Praesentationsverwaltung.Tests.Controllers;

public class PresentationControllerTests
{
    private static PresentationController CreateSut(ApplicationDbContext context) =>
        new PresentationController(new PresentationService(context)).WithContext(MvcTestHarness.Admin());

    private static PresentationFormViewModel ValidForm(int id = 0) => new()
    {
        Id = id,
        Topic = "Digitalisierung",
        StartsAt = new DateTime(2026, 9, 1, 9, 0, 0),
        RoomName = "411",
        MaxObservers = 6
    };

    [Fact]
    public async Task Index_returns_view_with_presentations()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        PresentationController sut = CreateSut(context);
        await sut.Create(ValidForm());

        var view = Assert.IsType<ViewResult>(await sut.Index());
        var list = Assert.IsAssignableFrom<IReadOnlyList<Presentation>>(view.Model);
        Assert.Single(list);
    }

    [Fact]
    public void Create_get_returns_empty_form()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        PresentationController sut = CreateSut(context);

        var view = Assert.IsType<ViewResult>(sut.Create());
        Assert.Equal("PresentationForm", view.ViewName);
        Assert.IsType<PresentationFormViewModel>(view.Model);
    }

    [Fact]
    public async Task Create_post_invalid_modelstate_returns_form()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        PresentationController sut = CreateSut(context);
        sut.ModelState.AddModelError("Topic", "required");

        var view = Assert.IsType<ViewResult>(await sut.Create(ValidForm()));
        Assert.Equal("PresentationForm", view.ViewName);
        Assert.Equal(0, await context.Presentations.CountAsync());
    }

    [Fact]
    public async Task Create_post_valid_persists_and_redirects()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        PresentationController sut = CreateSut(context);

        var redirect = Assert.IsType<RedirectToActionResult>(await sut.Create(ValidForm()));
        Assert.Equal(nameof(PresentationController.Index), redirect.ActionName);
        Assert.Equal(1, await context.Presentations.CountAsync());
    }

    [Fact]
    public async Task Edit_get_returns_not_found_for_unknown_id()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        PresentationController sut = CreateSut(context);

        Assert.IsType<NotFoundResult>(await sut.Edit(9999));
    }

    [Fact]
    public async Task Edit_get_returns_populated_form()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        PresentationController sut = CreateSut(context);
        await sut.Create(ValidForm());
        Presentation created = await context.Presentations.FirstAsync();

        var view = Assert.IsType<ViewResult>(await sut.Edit(created.Id));
        var model = Assert.IsType<PresentationFormViewModel>(view.Model);
        Assert.Equal("Digitalisierung", model.Topic);
        Assert.Equal("411", model.RoomName);
    }

    [Fact]
    public async Task Edit_post_valid_updates_and_redirects()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        PresentationController sut = CreateSut(context);
        await sut.Create(ValidForm());
        Presentation created = await context.Presentations.FirstAsync();

        var form = ValidForm(created.Id);
        form.Topic = "Geändert";
        var redirect = Assert.IsType<RedirectToActionResult>(await sut.Edit(form));
        Assert.Equal(nameof(PresentationController.Index), redirect.ActionName);
        Assert.Equal("Geändert", (await context.Presentations.FirstAsync()).Topic);
    }

    [Fact]
    public async Task Delete_removes_and_redirects()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        PresentationController sut = CreateSut(context);
        await sut.Create(ValidForm());
        Presentation created = await context.Presentations.FirstAsync();

        var redirect = Assert.IsType<RedirectToActionResult>(await sut.Delete(created.Id));
        Assert.Equal(nameof(PresentationController.Index), redirect.ActionName);
        Assert.Equal(0, await context.Presentations.CountAsync());
    }
}
