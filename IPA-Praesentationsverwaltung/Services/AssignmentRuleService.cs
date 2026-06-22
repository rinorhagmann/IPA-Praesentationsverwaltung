using IPA_Praesentationsverwaltung.Models.Domain;
using IPA_Praesentationsverwaltung.Services.Abstractions;

namespace IPA_Praesentationsverwaltung.Services;

/// <summary>
/// Pure implementation of the assignment rules:
/// <list type="bullet">
///   <item>a student observes at most two presentations,</item>
///   <item>a presentation holds at most <see cref="Presentation.MaxObservers"/> observers,</item>
///   <item>a student cannot attend two presentations starting at the same time.</item>
/// </list>
/// </summary>
public sealed class AssignmentRuleService : IAssignmentRuleService
{
    public bool HasMaximumTwoRegistrations(Student student)
    {
        ArgumentNullException.ThrowIfNull(student);
        return student.Registrations.Count < Student.RequiredSelectionCount;
    }

    public bool HasNoTimeConflict(Student student, Presentation presentation)
    {
        ArgumentNullException.ThrowIfNull(student);
        ArgumentNullException.ThrowIfNull(presentation);

        // None of the already chosen presentations may start at the same time.
        return student.Registrations
            .Select(registration => registration.Presentation)
            .Where(existing => existing is not null)
            .All(existing => existing!.StartsAt != presentation.StartsAt);
    }

    public bool HasFreeSeats(Presentation presentation)
    {
        ArgumentNullException.ThrowIfNull(presentation);
        return presentation.HasFreeSeats();
    }

    public bool IsSelectionComplete(Student student)
    {
        ArgumentNullException.ThrowIfNull(student);
        return student.HasCompletedSelection();
    }
}
