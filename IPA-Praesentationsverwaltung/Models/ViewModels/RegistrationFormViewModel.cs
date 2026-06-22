using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IPA_Praesentationsverwaltung.Models.ViewModels;

/// <summary>Create/edit form model for registrations (admin CRUD).</summary>
public class RegistrationFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Bitte wählen Sie eine Schülerin / einen Schüler.")]
    [Display(Name = "Schüler/in")]
    public int StudentId { get; set; }

    [Required(ErrorMessage = "Bitte wählen Sie eine Präsentation.")]
    [Display(Name = "Präsentation")]
    public int PresentationId { get; set; }

    /// <summary>Select list options populated by the controller.</summary>
    public IEnumerable<SelectListItem> StudentOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> PresentationOptions { get; set; } = new List<SelectListItem>();

    public bool IsEdit => Id > 0;
}
