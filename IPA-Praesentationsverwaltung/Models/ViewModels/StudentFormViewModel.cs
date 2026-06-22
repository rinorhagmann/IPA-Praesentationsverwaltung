using System.ComponentModel.DataAnnotations;

namespace IPA_Praesentationsverwaltung.Models.ViewModels;

/// <summary>Create/edit form model for students (admin CRUD).</summary>
public class StudentFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Bitte geben Sie eine E-Mail-Adresse ein.")]
    [EmailAddress(ErrorMessage = "Bitte geben Sie eine gültige E-Mail-Adresse ein.")]
    [Display(Name = "E-Mail Adresse")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bitte geben Sie den Vornamen ein.")]
    [Display(Name = "Vorname")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bitte geben Sie den Nachnamen ein.")]
    [Display(Name = "Nachname")]
    public string LastName { get; set; } = string.Empty;

    /// <summary>Optional password; on create a random one is generated when left empty.</summary>
    [DataType(DataType.Password)]
    [Display(Name = "Passwort")]
    public string? Password { get; set; }

    public bool IsEdit => Id > 0;
}
