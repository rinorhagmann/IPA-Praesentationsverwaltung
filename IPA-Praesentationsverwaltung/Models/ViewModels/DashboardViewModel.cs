namespace IPA_Praesentationsverwaltung.Models.ViewModels;

/// <summary>Aggregated statistics shown on the administration dashboard.</summary>
public class DashboardViewModel
{
    /// <summary>Total number of imported students.</summary>
    public int TotalStudents { get; init; }

    /// <summary>Total number of imported presentations.</summary>
    public int TotalPresentations { get; init; }

    /// <summary>Number of students that have not entered any registration yet.</summary>
    public int StudentsWithoutRegistration { get; init; }

    /// <summary>Number of presentations that no student has registered for.</summary>
    public int PresentationsWithoutObservers { get; init; }
}
