namespace IPA_Praesentationsverwaltung.Models.ViewModels;

/// <summary>Aggregated statistics shown on the administration dashboard.</summary>
public class DashboardViewModel
{
    /// <summary>Number of students who have completed their selection.</summary>
    public int StudentsWithCompleteSelection { get; init; }

    /// <summary>Total number of imported students.</summary>
    public int TotalStudents { get; init; }

    /// <summary>Total number of imported presentations.</summary>
    public int TotalPresentations { get; init; }

    /// <summary>Number of registrations still missing for a full assignment.</summary>
    public int PendingRegistrations { get; init; }

    /// <summary>Number of detected scheduling conflicts (over capacity / double booking).</summary>
    public int PotentialConflicts { get; init; }
}
