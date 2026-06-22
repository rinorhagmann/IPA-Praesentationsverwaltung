using System.ComponentModel.DataAnnotations;

namespace IPA_Praesentationsverwaltung.Models.Domain;

/// <summary>
/// Base class for every account in the system. Concrete account types
/// (<see cref="Student"/> and <see cref="Admin"/>) inherit from it and are
/// persisted through a table-per-hierarchy mapping discriminated by <see cref="Role"/>.
/// </summary>
public abstract class User
{
    public int Id { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// PBKDF2 hash of the account password. The plaintext password is never stored.
    /// </summary>
    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    /// <summary>Whether the account is allowed to authenticate.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Returns the human readable full name "FirstName LastName".</summary>
    public string GetFullName() => $"{FirstName} {LastName}".Trim();
}
