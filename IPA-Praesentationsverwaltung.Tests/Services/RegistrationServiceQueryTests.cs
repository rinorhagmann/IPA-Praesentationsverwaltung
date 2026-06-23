using IPA_Praesentationsverwaltung.Data;
using IPA_Praesentationsverwaltung.Models.Domain;
using IPA_Praesentationsverwaltung.Services;
using IPA_Praesentationsverwaltung.Services.Abstractions;
using IPA_Praesentationsverwaltung.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IPA_Praesentationsverwaltung.Tests.Services;

/// <summary>Covers the query, update, delete and guard paths of <see cref="RegistrationService"/>.</summary>
public class RegistrationServiceQueryTests
{
    private static RegistrationService CreateSut(ApplicationDbContext context) =>
        new(context, new AssignmentRuleService());

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
    public async Task GetRegistrationsByStudent_returns_only_that_students_rows_with_includes()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        (int annaId, int beatId, int presA, int presB) = await SeedAsync(context);
        RegistrationService sut = CreateSut(context);
        await sut.CreateRegistrationAsync(annaId, presA);
        await sut.CreateRegistrationAsync(beatId, presB);

        IReadOnlyList<Registration> annaRegs = await sut.GetRegistrationsByStudentAsync(annaId);

        Registration only = Assert.Single(annaRegs);
        Assert.Equal("A", only.Presentation!.Topic);
        Assert.Equal("411", only.Presentation!.Room!.Name);
    }

    [Fact]
    public async Task GetAllRegistrations_includes_student_and_presentation()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        (int annaId, _, int presA, _) = await SeedAsync(context);
        RegistrationService sut = CreateSut(context);
        await sut.CreateRegistrationAsync(annaId, presA);

        IReadOnlyList<Registration> all = await sut.GetAllRegistrationsAsync();

        Registration only = Assert.Single(all);
        Assert.NotNull(only.Student);
        Assert.NotNull(only.Presentation);
    }

    [Fact]
    public async Task GetRegistrationById_returns_match_or_null()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        (int annaId, _, int presA, _) = await SeedAsync(context);
        RegistrationService sut = CreateSut(context);
        await sut.CreateRegistrationAsync(annaId, presA);
        Registration reg = await context.Registrations.FirstAsync();

        Assert.NotNull(await sut.GetRegistrationByIdAsync(reg.Id));
        Assert.Null(await sut.GetRegistrationByIdAsync(9999));
    }

    [Fact]
    public async Task UpdateRegistration_changes_student_and_presentation()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        (int annaId, int beatId, int presA, int presB) = await SeedAsync(context);
        RegistrationService sut = CreateSut(context);
        await sut.CreateRegistrationAsync(annaId, presA);
        Registration reg = await context.Registrations.FirstAsync();

        await sut.UpdateRegistrationAsync(reg.Id, beatId, presB);

        Registration updated = await context.Registrations.FirstAsync();
        Assert.Equal(beatId, updated.StudentId);
        Assert.Equal(presB, updated.PresentationId);
    }

    [Fact]
    public async Task UpdateRegistration_throws_when_missing()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        (int annaId, _, _, int presB) = await SeedAsync(context);
        RegistrationService sut = CreateSut(context);

        await Assert.ThrowsAsync<RegistrationNotAllowedException>(() =>
            sut.UpdateRegistrationAsync(9999, annaId, presB));
    }

    [Fact]
    public async Task DeleteRegistration_removes_existing_and_ignores_missing()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        (int annaId, _, int presA, _) = await SeedAsync(context);
        RegistrationService sut = CreateSut(context);
        await sut.CreateRegistrationAsync(annaId, presA);
        Registration reg = await context.Registrations.FirstAsync();

        await sut.DeleteRegistrationAsync(9999);
        Assert.Equal(1, await context.Registrations.CountAsync());

        await sut.DeleteRegistrationAsync(reg.Id);
        Assert.Equal(0, await context.Registrations.CountAsync());
    }

    [Fact]
    public async Task CreateRegistration_throws_for_unknown_student_or_presentation()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        (int annaId, _, int presA, _) = await SeedAsync(context);
        RegistrationService sut = CreateSut(context);

        await Assert.ThrowsAsync<RegistrationNotAllowedException>(() => sut.CreateRegistrationAsync(9999, presA));
        await Assert.ThrowsAsync<RegistrationNotAllowedException>(() => sut.CreateRegistrationAsync(annaId, 9999));
    }

    [Fact]
    public void CanRegister_is_true_for_a_fresh_student_and_guards_nulls()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        RegistrationService sut = CreateSut(context);
        var student = new Student { Email = "a@b.ch", FirstName = "A", LastName = "B", PasswordHash = "x" };
        var presentation = new Presentation { Topic = "A", StartsAt = new DateTime(2026, 9, 1, 9, 0, 0), MaxObservers = 6 };

        Assert.True(sut.CanRegister(student, presentation));
        Assert.Throws<ArgumentNullException>(() => sut.CanRegister(null!, presentation));
        Assert.Throws<ArgumentNullException>(() => sut.CanRegister(student, null!));
    }
}
