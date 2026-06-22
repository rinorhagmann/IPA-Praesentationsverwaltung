using IPA_Praesentationsverwaltung.Models.Domain;
using Xunit;

namespace IPA_Praesentationsverwaltung.Tests.Domain;

public class DomainModelTests
{
    [Fact]
    public void GetFullName_combines_first_and_last_name()
    {
        var student = new Student { FirstName = "Anna", LastName = "Muster" };
        Assert.Equal("Anna Muster", student.GetFullName());
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(5, true)]
    [InlineData(6, false)]
    public void HasFreeSeats_depends_on_registration_count(int registrations, bool expected)
    {
        var presentation = new Presentation { MaxObservers = 6 };
        for (int i = 0; i < registrations; i++)
        {
            presentation.Registrations.Add(new Registration());
        }

        Assert.Equal(expected, presentation.HasFreeSeats());
    }

    [Fact]
    public void IsAtSameTime_is_true_only_for_equal_start_times()
    {
        var time = new DateTime(2026, 1, 1, 9, 0, 0);
        var a = new Presentation { StartsAt = time };
        var sameTime = new Presentation { StartsAt = time };
        var otherTime = new Presentation { StartsAt = time.AddMinutes(30) };

        Assert.True(a.IsAtSameTime(sameTime));
        Assert.False(a.IsAtSameTime(otherTime));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, true)]
    public void HasCompletedSelection_requires_two_registrations(int count, bool expected)
    {
        var student = new Student();
        for (int i = 0; i < count; i++)
        {
            student.Registrations.Add(new Registration());
        }

        Assert.Equal(expected, student.HasCompletedSelection());
    }
}
