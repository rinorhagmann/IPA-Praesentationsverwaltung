using IPA_Praesentationsverwaltung.Data;
using IPA_Praesentationsverwaltung.Models.Domain;
using IPA_Praesentationsverwaltung.Models.ViewModels;
using IPA_Praesentationsverwaltung.Services;
using IPA_Praesentationsverwaltung.Tests.TestSupport;
using Xunit;

namespace IPA_Praesentationsverwaltung.Tests.Services;

public class DashboardServiceTests
{
    [Fact]
    public async Task GetDashboard_counts_students_presentations_and_gaps()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        var room = new Room { Name = "411" };
        var withReg = new Student { Email = "a@wgbs.ch", FirstName = "A", LastName = "A", PasswordHash = "x" };
        var withoutReg = new Student { Email = "b@wgbs.ch", FirstName = "B", LastName = "B", PasswordHash = "x" };
        var observed = new Presentation { Topic = "P1", StartsAt = new DateTime(2026, 9, 1, 9, 0, 0), Room = room, MaxObservers = 6 };
        var unobserved = new Presentation { Topic = "P2", StartsAt = new DateTime(2026, 9, 1, 10, 0, 0), Room = room, MaxObservers = 6 };
        context.AddRange(room, withReg, withoutReg, observed, unobserved);
        await context.SaveChangesAsync();
        context.Registrations.Add(new Registration { StudentId = withReg.Id, PresentationId = observed.Id });
        await context.SaveChangesAsync();

        var sut = new DashboardService(context);
        DashboardViewModel model = await sut.GetDashboardAsync();

        Assert.Equal(2, model.TotalStudents);
        Assert.Equal(2, model.TotalPresentations);
        Assert.Equal(1, model.StudentsWithoutRegistration);
        Assert.Equal(1, model.PresentationsWithoutObservers);
    }

    [Fact]
    public async Task GetDashboard_on_empty_database_returns_zeroes()
    {
        using ApplicationDbContext context = InMemoryDbContextFactory.Create();
        var sut = new DashboardService(context);

        DashboardViewModel model = await sut.GetDashboardAsync();

        Assert.Equal(0, model.TotalStudents);
        Assert.Equal(0, model.TotalPresentations);
        Assert.Equal(0, model.StudentsWithoutRegistration);
        Assert.Equal(0, model.PresentationsWithoutObservers);
    }
}
