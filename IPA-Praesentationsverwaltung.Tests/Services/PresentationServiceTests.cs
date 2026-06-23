using IPA_Praesentationsverwaltung.Data;
using IPA_Praesentationsverwaltung.Models.Domain;
using IPA_Praesentationsverwaltung.Services;
using IPA_Praesentationsverwaltung.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IPA_Praesentationsverwaltung.Tests.Services;

public class PresentationServiceTests
{
    private static PresentationService CreateSut(ApplicationDbContext context) => new(context);

    private static Presentation NewPresentation(string topic, DateTime startsAt, int max = 6) =>
        new() { Topic = topic, StartsAt = startsAt, MaxObservers = max };

    [Fact]
    public async Task CreatePresentation_reuses_existing_room_by_trimmed_name()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        PresentationService sut = CreateSut(context);

        await sut.CreatePresentationAsync(NewPresentation("A", new DateTime(2026, 9, 1, 9, 0, 0)), "411");
        await sut.CreatePresentationAsync(NewPresentation("B", new DateTime(2026, 9, 1, 10, 0, 0)), " 411 ");

        Assert.Equal(1, await context.Rooms.CountAsync());
        Assert.Equal(2, await context.Presentations.CountAsync());
    }

    [Fact]
    public async Task CreatePresentation_rejects_blank_room()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        PresentationService sut = CreateSut(context);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.CreatePresentationAsync(NewPresentation("A", new DateTime(2026, 9, 1, 9, 0, 0)), "   "));
    }

    [Fact]
    public async Task GetAllPresentations_includes_room_and_orders_by_start()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        PresentationService sut = CreateSut(context);
        await sut.CreatePresentationAsync(NewPresentation("Late", new DateTime(2026, 9, 1, 11, 0, 0)), "411");
        await sut.CreatePresentationAsync(NewPresentation("Early", new DateTime(2026, 9, 1, 9, 0, 0)), "412");

        IReadOnlyList<Presentation> all = await sut.GetAllPresentationsAsync();

        Assert.Equal("Early", all[0].Topic);
        Assert.NotNull(all[0].Room);
    }

    [Fact]
    public async Task GetPresentationById_returns_match_or_null()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        PresentationService sut = CreateSut(context);
        await sut.CreatePresentationAsync(NewPresentation("A", new DateTime(2026, 9, 1, 9, 0, 0)), "411");
        Presentation created = await context.Presentations.FirstAsync();

        Assert.NotNull(await sut.GetPresentationByIdAsync(created.Id));
        Assert.Null(await sut.GetPresentationByIdAsync(9999));
    }

    [Fact]
    public async Task UpdatePresentation_changes_fields_and_room()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        PresentationService sut = CreateSut(context);
        await sut.CreatePresentationAsync(NewPresentation("Old", new DateTime(2026, 9, 1, 9, 0, 0)), "411");
        Presentation created = await context.Presentations.FirstAsync();

        await sut.UpdatePresentationAsync(
            new Presentation
            {
                Id = created.Id,
                Topic = "New",
                StartsAt = new DateTime(2026, 9, 2, 9, 0, 0),
                MaxObservers = 4
            },
            "412");

        Presentation updated = await sut.GetPresentationByIdAsync(created.Id)
            ?? throw new InvalidOperationException();
        Assert.Equal("New", updated.Topic);
        Assert.Equal(4, updated.MaxObservers);
        Assert.Equal("412", updated.Room!.Name);
    }

    [Fact]
    public async Task UpdatePresentation_throws_when_missing()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        PresentationService sut = CreateSut(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.UpdatePresentationAsync(
                new Presentation { Id = 5, Topic = "X", StartsAt = new DateTime(2026, 9, 1, 9, 0, 0) }, "411"));
    }

    [Fact]
    public async Task DeletePresentation_removes_existing_and_ignores_missing()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        PresentationService sut = CreateSut(context);
        await sut.CreatePresentationAsync(NewPresentation("A", new DateTime(2026, 9, 1, 9, 0, 0)), "411");
        Presentation created = await context.Presentations.FirstAsync();

        await sut.DeletePresentationAsync(9999);
        Assert.Equal(1, await context.Presentations.CountAsync());

        await sut.DeletePresentationAsync(created.Id);
        Assert.Equal(0, await context.Presentations.CountAsync());
    }

    [Fact]
    public async Task GetAvailablePresentations_excludes_selected_full_and_conflicting()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        PresentationService sut = CreateSut(context);

        var room = new Room { Name = "411" };
        var student = new Student { Email = "anna@wgbs.ch", FirstName = "Anna", LastName = "Muster", PasswordHash = "x" };
        var other = new Student { Email = "b@wgbs.ch", FirstName = "B", LastName = "B", PasswordHash = "x" };
        var nine = new DateTime(2026, 9, 1, 9, 0, 0);
        var ten = new DateTime(2026, 9, 1, 10, 0, 0);
        var eleven = new DateTime(2026, 9, 1, 11, 0, 0);

        var selected = new Presentation { Topic = "Selected", StartsAt = nine, Room = room, MaxObservers = 6 };
        var conflict = new Presentation { Topic = "Conflict", StartsAt = nine, Room = room, MaxObservers = 6 };
        var full = new Presentation { Topic = "Full", StartsAt = ten, Room = room, MaxObservers = 1 };
        var open = new Presentation { Topic = "Open", StartsAt = eleven, Room = room, MaxObservers = 6 };
        context.AddRange(room, student, other, selected, conflict, full, open);
        await context.SaveChangesAsync();

        context.Registrations.Add(new Registration { StudentId = student.Id, PresentationId = selected.Id });
        context.Registrations.Add(new Registration { StudentId = other.Id, PresentationId = full.Id });
        await context.SaveChangesAsync();

        IReadOnlyList<Presentation> available = await sut.GetAvailablePresentationsAsync(student);

        Presentation only = Assert.Single(available);
        Assert.Equal("Open", only.Topic);
    }

    [Fact]
    public async Task GetAvailablePresentations_validates_student()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        PresentationService sut = CreateSut(context);

        await Assert.ThrowsAsync<ArgumentNullException>(() => sut.GetAvailablePresentationsAsync(null!));
    }
}
