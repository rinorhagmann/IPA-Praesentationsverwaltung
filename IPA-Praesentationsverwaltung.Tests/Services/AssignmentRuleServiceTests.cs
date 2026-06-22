using IPA_Praesentationsverwaltung.Models.Domain;
using IPA_Praesentationsverwaltung.Services;
using Xunit;

namespace IPA_Praesentationsverwaltung.Tests.Services;

public class AssignmentRuleServiceTests
{
    private readonly AssignmentRuleService _sut = new();

    private static Presentation PresentationAt(DateTime startsAt, int observers = 0, int max = 6)
    {
        var presentation = new Presentation { StartsAt = startsAt, MaxObservers = max };
        for (int i = 0; i < observers; i++)
        {
            presentation.Registrations.Add(new Registration());
        }

        return presentation;
    }

    [Fact]
    public void HasMaximumTwoRegistrations_allows_until_two_selections()
    {
        var student = new Student();
        Assert.True(_sut.HasMaximumTwoRegistrations(student));

        student.Registrations.Add(new Registration());
        Assert.True(_sut.HasMaximumTwoRegistrations(student));

        student.Registrations.Add(new Registration());
        Assert.False(_sut.HasMaximumTwoRegistrations(student));
    }

    [Fact]
    public void HasFreeSeats_is_false_when_capacity_reached()
    {
        Assert.True(_sut.HasFreeSeats(PresentationAt(DateTime.Now, observers: 5)));
        Assert.False(_sut.HasFreeSeats(PresentationAt(DateTime.Now, observers: 6)));
    }

    [Fact]
    public void HasNoTimeConflict_detects_overlapping_start_times()
    {
        var time = new DateTime(2026, 1, 1, 9, 0, 0);
        var student = new Student();
        student.Registrations.Add(new Registration { Presentation = PresentationAt(time) });

        Assert.False(_sut.HasNoTimeConflict(student, PresentationAt(time)));
        Assert.True(_sut.HasNoTimeConflict(student, PresentationAt(time.AddMinutes(30))));
    }

    [Fact]
    public void IsSelectionComplete_is_true_with_two_registrations()
    {
        var student = new Student();
        student.Registrations.Add(new Registration());
        Assert.False(_sut.IsSelectionComplete(student));

        student.Registrations.Add(new Registration());
        Assert.True(_sut.IsSelectionComplete(student));
    }
}
