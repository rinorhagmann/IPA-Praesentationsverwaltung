using IPA_Praesentationsverwaltung.Data;
using IPA_Praesentationsverwaltung.Models.Domain;
using IPA_Praesentationsverwaltung.Services;
using IPA_Praesentationsverwaltung.Services.Abstractions;
using IPA_Praesentationsverwaltung.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IPA_Praesentationsverwaltung.Tests.Services;

public class RegistrationServiceTests
{
    private static RegistrationService CreateSut(ApplicationDbContext context) =>
        new(context, new AssignmentRuleService());

    private static async Task<(int studentId, List<int> presentationIds)> SeedAsync(
        ApplicationDbContext context, int presentationCount = 3)
    {
        var room = new Room { Name = "411" };
        context.Rooms.Add(room);

        var student = new Student
        {
            Email = "anna@wgbs.ch", FirstName = "Anna", LastName = "Muster", PasswordHash = "x"
        };
        context.Students.Add(student);

        var presentationIds = new List<int>();
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
            presentationIds.Add(presentation.Id);
        }

        return (student.Id, presentationIds);
    }

    [Fact]
    public async Task CreateRegistration_persists_a_valid_registration()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        (int studentId, List<int> presentations) = await SeedAsync(context);
        RegistrationService sut = CreateSut(context);

        await sut.CreateRegistrationAsync(studentId, presentations[0]);

        Assert.Equal(1, await context.Registrations.CountAsync());
    }

    [Fact]
    public async Task CreateRegistration_rejects_more_than_two_selections()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        (int studentId, List<int> presentations) = await SeedAsync(context);
        RegistrationService sut = CreateSut(context);

        await sut.CreateRegistrationAsync(studentId, presentations[0]);
        await sut.CreateRegistrationAsync(studentId, presentations[1]);

        RegistrationNotAllowedException ex = await Assert.ThrowsAsync<RegistrationNotAllowedException>(
            () => sut.CreateRegistrationAsync(studentId, presentations[2]));
        Assert.Contains("maximale Anzahl", ex.Message);
    }

    [Fact]
    public async Task CreateRegistration_rejects_time_conflicts()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        var room = new Room { Name = "411" };
        var student = new Student { Email = "a@b.ch", FirstName = "A", LastName = "B", PasswordHash = "x" };
        var time = new DateTime(2026, 9, 1, 9, 0, 0);
        var first = new Presentation { Topic = "A", StartsAt = time, Room = room, MaxObservers = 6 };
        var sameTime = new Presentation { Topic = "B", StartsAt = time, Room = room, MaxObservers = 6 };
        context.AddRange(room, student, first, sameTime);
        await context.SaveChangesAsync();
        RegistrationService sut = CreateSut(context);

        await sut.CreateRegistrationAsync(student.Id, first.Id);

        RegistrationNotAllowedException ex = await Assert.ThrowsAsync<RegistrationNotAllowedException>(
            () => sut.CreateRegistrationAsync(student.Id, sameTime.Id));
        Assert.Contains("gleichen Zeit", ex.Message);
    }

    [Fact]
    public async Task CreateRegistration_rejects_when_presentation_is_full()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        var room = new Room { Name = "411" };
        var target = new Presentation
        {
            Topic = "Voll", StartsAt = new DateTime(2026, 9, 1, 9, 0, 0), Room = room, MaxObservers = 6
        };
        context.AddRange(room, target);
        await context.SaveChangesAsync();

        // Fill all six seats with other students.
        for (int i = 0; i < 6; i++)
        {
            var filler = new Student { Email = $"s{i}@b.ch", FirstName = "S", LastName = i.ToString(), PasswordHash = "x" };
            context.Students.Add(filler);
            await context.SaveChangesAsync();
            context.Registrations.Add(new Registration { StudentId = filler.Id, PresentationId = target.Id });
            await context.SaveChangesAsync();
        }

        var latecomer = new Student { Email = "late@b.ch", FirstName = "L", LastName = "C", PasswordHash = "x" };
        context.Students.Add(latecomer);
        await context.SaveChangesAsync();
        RegistrationService sut = CreateSut(context);

        RegistrationNotAllowedException ex = await Assert.ThrowsAsync<RegistrationNotAllowedException>(
            () => sut.CreateRegistrationAsync(latecomer.Id, target.Id));
        Assert.Contains("ausgebucht", ex.Message);
    }

    [Fact]
    public async Task CreateRegistration_rejects_duplicate_selection()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        (int studentId, List<int> presentations) = await SeedAsync(context);
        RegistrationService sut = CreateSut(context);

        await sut.CreateRegistrationAsync(studentId, presentations[0]);

        await Assert.ThrowsAsync<RegistrationNotAllowedException>(
            () => sut.CreateRegistrationAsync(studentId, presentations[0]));
    }
}
