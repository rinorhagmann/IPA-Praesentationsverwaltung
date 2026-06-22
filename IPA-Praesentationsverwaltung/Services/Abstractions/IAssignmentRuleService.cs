using IPA_Praesentationsverwaltung.Models.Domain;

namespace IPA_Praesentationsverwaltung.Services.Abstractions;

/// <summary>
/// Evaluates the assignment rules from the requirements. The methods are pure
/// functions over already-loaded domain aggregates, which keeps them trivial to
/// unit test and free of side effects.
/// </summary>
public interface IAssignmentRuleService
{
    /// <summary>True while the student has not yet reached the maximum of two registrations.</summary>
    bool HasMaximumTwoRegistrations(Student student);

    /// <summary>True when none of the student's registrations clash in time with the presentation.</summary>
    bool HasNoTimeConflict(Student student, Presentation presentation);

    /// <summary>True when the presentation still has at least one free observer seat.</summary>
    bool HasFreeSeats(Presentation presentation);

    /// <summary>True once the student has selected the required number of presentations.</summary>
    bool IsSelectionComplete(Student student);
}
