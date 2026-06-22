using System.ComponentModel.DataAnnotations;

namespace IPA_Praesentationsverwaltung.Models.ViewModels;

/// <summary>Create/edit form model for presentations (admin CRUD).</summary>
public class PresentationFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Bitte geben Sie ein Thema ein.")]
    [Display(Name = "Thema")]
    public string Topic { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bitte geben Sie Datum und Uhrzeit ein.")]
    [DataType(DataType.DateTime)]
    [Display(Name = "Datum und Uhrzeit")]
    public DateTime StartsAt { get; set; } = DateTime.Today.AddHours(9);

    [Required(ErrorMessage = "Bitte geben Sie einen Raum ein.")]
    [Display(Name = "Raum")]
    public string RoomName { get; set; } = string.Empty;

    [Range(1, 100, ErrorMessage = "Die maximale Anzahl Zuseher muss zwischen 1 und 100 liegen.")]
    [Display(Name = "Max. Zuseher")]
    public int MaxObservers { get; set; } = Domain.Presentation.DefaultMaxObservers;

    public bool IsEdit => Id > 0;
}
