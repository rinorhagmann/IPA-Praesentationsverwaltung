using System.ComponentModel.DataAnnotations;

namespace IPA_Praesentationsverwaltung.Models.ViewModels;

/// <summary>Form model for the login page.</summary>
public class LoginViewModel
{
    [Required(ErrorMessage = "Bitte geben Sie Ihre E-Mail-Adresse ein.")]
    [EmailAddress(ErrorMessage = "Bitte geben Sie eine gültige E-Mail-Adresse ein.")]
    [Display(Name = "E-Mail Adresse")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bitte geben Sie Ihr Passwort ein.")]
    [DataType(DataType.Password)]
    [Display(Name = "Passwort")]
    public string Password { get; set; } = string.Empty;

    /// <summary>Optional URL the user is redirected to after a successful login.</summary>
    public string? ReturnUrl { get; set; }
}
