namespace IPA_Praesentationsverwaltung.Models.ViewModels;

/// <summary>
/// Backing model for the student presentation pages. Holds the presentations the
/// student may still choose from as well as the ones already selected.
/// </summary>
public class StudentSelectionViewModel
{
    public int StudentId { get; init; }

    public string StudentName { get; init; } = string.Empty;

    /// <summary>Presentations that are still selectable for this student.</summary>
    public IReadOnlyList<PresentationListItemViewModel> AvailablePresentations { get; init; }
        = new List<PresentationListItemViewModel>();

    /// <summary>Presentations the student has already registered for.</summary>
    public IReadOnlyList<PresentationListItemViewModel> CurrentSelections { get; init; }
        = new List<PresentationListItemViewModel>();

    /// <summary>Number of presentations the student still has to choose.</summary>
    public int RemainingSelections { get; init; }

    public bool HasCompletedSelection => RemainingSelections <= 0;

    /// <summary>True after the student has irrevocably confirmed their selection.</summary>
    public bool IsSelectionConfirmed { get; init; }
}
